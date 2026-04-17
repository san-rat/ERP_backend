using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("")]
public class ServiceSwaggerController : ControllerBase
{
    private static readonly string[] SupportedServices =
    [
        "auth",
        "customer",
        "order",
        "product",
        "forecast",
        "prediction",
        "analytics",
        "admin"
    ];

    private static readonly string[] OperationKeys =
    [
        "get",
        "put",
        "post",
        "delete",
        "options",
        "head",
        "patch",
        "trace"
    ];

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceSwaggerController> _logger;

    public ServiceSwaggerController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceSwaggerController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("{service}/swagger")]
    [HttpGet("{service}/swagger/index.html")]
    public IActionResult SwaggerUi(string service)
    {
        if (!TryGetServiceDefinition(service, out var definition))
        {
            return NotFound();
        }

        return Content(BuildSwaggerHtml(definition), "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("{service}/swagger/v1/swagger.json")]
    public async Task<IActionResult> SwaggerJson(string service, CancellationToken cancellationToken)
    {
        if (!TryGetServiceDefinition(service, out var definition))
        {
            return NotFound();
        }

        var downstreamSwaggerUri = GetDownstreamSwaggerUri(definition.Slug);
        if (downstreamSwaggerUri is null)
        {
            _logger.LogError("Swagger proxy route is missing for service {Service}", definition.Slug);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = $"Swagger proxy route is not configured for service '{definition.Slug}'."
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(downstreamSwaggerUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch downstream swagger for {Service}. Status: {StatusCode}",
                    definition.Slug,
                    (int)response.StatusCode);

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = $"Downstream swagger fetch failed for service '{definition.Slug}'.",
                    statusCode = (int)response.StatusCode
                });
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var document = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document is not JsonObject root)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = $"Downstream swagger for service '{definition.Slug}' is not valid JSON."
                });
            }

            RewriteOpenApiDocument(root, definition);

            return Content(
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                "application/json; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rewriting swagger for service {Service}", definition.Slug);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = $"Gateway could not load swagger for service '{definition.Slug}'."
            });
        }
    }

    private void RewriteOpenApiDocument(JsonObject root, SwaggerServiceDefinition definition)
    {
        root["servers"] = new JsonArray(
            new JsonObject
            {
                ["url"] = GetGatewayBaseUrl()
            });

        root.Remove("security");

        var protectedOperationsExist = false;
        if (root["paths"] is JsonObject existingPaths)
        {
            var rewrittenPaths = new JsonObject();

            foreach (var pathEntry in existingPaths)
            {
                if (pathEntry.Value is null)
                {
                    continue;
                }

                var rewrittenPath = RewritePath(definition.Slug, pathEntry.Key);
                if (rewrittenPath is null)
                {
                    continue;
                }

                if (pathEntry.Value.DeepClone() is not JsonObject rewrittenPathItem)
                {
                    continue;
                }

                var requiresAuth = RequiresAuth(rewrittenPath);
                ApplyOperationSecurity(rewrittenPathItem, requiresAuth, ref protectedOperationsExist);
                MergePathItem(rewrittenPaths, rewrittenPath, rewrittenPathItem);
            }

            root["paths"] = rewrittenPaths;
        }

        if (protectedOperationsExist)
        {
            EnsureBearerSecurityScheme(root);
        }
    }

    private string GetGatewayBaseUrl()
    {
        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
        return $"{Request.Scheme}://{Request.Host}{pathBase}";
    }

    private Uri? GetDownstreamSwaggerUri(string service)
    {
        var route = _configuration.GetSection("Routes")
            .GetChildren()
            .FirstOrDefault(section =>
                string.Equals(
                    section["UpstreamPathTemplate"],
                    $"/{service}/swagger/{{everything}}",
                    StringComparison.OrdinalIgnoreCase));

        var hostAndPort = route?.GetSection("DownstreamHostAndPorts").GetChildren().FirstOrDefault();
        var host = hostAndPort?["Host"];
        var scheme = route?["DownstreamScheme"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(scheme))
        {
            return null;
        }

        var uriBuilder = new UriBuilder(scheme, host)
        {
            Path = "/swagger/v1/swagger.json"
        };

        if (int.TryParse(hostAndPort?["Port"], out var port))
        {
            uriBuilder.Port = IsDefaultPort(scheme, port) ? -1 : port;
        }

        return uriBuilder.Uri;
    }

    private static bool IsDefaultPort(string scheme, int port) =>
        (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) && port == 80)
        || (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) && port == 443);

    private static bool TryGetServiceDefinition(string service, out SwaggerServiceDefinition definition)
    {
        definition = default!;
        if (!SupportedServices.Contains(service, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        definition = new SwaggerServiceDefinition(service.ToLowerInvariant(), service.ToLowerInvariant() switch
        {
            "auth" => "Authentication Service",
            "customer" => "Customer Service",
            "order" => "Order Service",
            "product" => "Product Service",
            "forecast" => "Forecast Service",
            "prediction" => "Prediction Service",
            "analytics" => "Analytics Service",
            "admin" => "Admin Service",
            _ => "Service"
        });

        return true;
    }

    private static string BuildSwaggerHtml(SwaggerServiceDefinition definition)
    {
        var title = $"InsightERP {definition.DisplayName} Swagger";
        var jsonUrl = $"/{definition.Slug}/swagger/v1/swagger.json";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{{title}}</title>
  <link rel="stylesheet" href="/swagger/swagger-ui.css" />
  <style>
    body { margin: 0; background: #f6f8fb; }
    .swagger-ui .topbar { display: none; }
    .gateway-note {
      font-family: Arial, sans-serif;
      background: #0f172a;
      color: #e2e8f0;
      padding: 12px 20px;
      font-size: 14px;
    }
    .gateway-note strong { color: #ffffff; }
  </style>
</head>
<body>
  <div class="gateway-note">
    <strong>{{title}}</strong> routed through ApiGateway. "Try it out" uses gateway upstream URLs.
  </div>
  <div id="swagger-ui"></div>
  <script src="/swagger/swagger-ui-bundle.js"></script>
  <script src="/swagger/swagger-ui-standalone-preset.js"></script>
  <script>
    window.onload = function () {
      window.ui = SwaggerUIBundle({
        url: '{{jsonUrl}}',
        dom_id: '#swagger-ui',
        deepLinking: true,
        presets: [
          SwaggerUIBundle.presets.apis,
          SwaggerUIStandalonePreset
        ],
        layout: 'BaseLayout',
        persistAuthorization: true,
        displayRequestDuration: true
      });
    };
  </script>
</body>
</html>
""";
    }

    private static void MergePathItem(JsonObject rewrittenPaths, string path, JsonObject newPathItem)
    {
        if (rewrittenPaths[path] is not JsonObject existingPathItem)
        {
            rewrittenPaths[path] = newPathItem;
            return;
        }

        foreach (var property in newPathItem)
        {
            existingPathItem[property.Key] = property.Value?.DeepClone();
        }
    }

    private static void ApplyOperationSecurity(JsonObject pathItem, bool requiresAuth, ref bool protectedOperationsExist)
    {
        foreach (var operationKey in OperationKeys)
        {
            if (pathItem[operationKey] is not JsonObject operation)
            {
                continue;
            }

            if (requiresAuth)
            {
                operation["security"] = CreateBearerSecurityRequirement();
                protectedOperationsExist = true;
            }
            else
            {
                operation.Remove("security");
            }
        }
    }

    private static JsonArray CreateBearerSecurityRequirement() =>
        [
            new JsonObject
            {
                ["Bearer"] = new JsonArray()
            }
        ];

    private static void EnsureBearerSecurityScheme(JsonObject root)
    {
        if (root["components"] is not JsonObject components)
        {
            components = new JsonObject();
            root["components"] = components;
        }

        if (components["securitySchemes"] is not JsonObject securitySchemes)
        {
            securitySchemes = new JsonObject();
            components["securitySchemes"] = securitySchemes;
        }

        securitySchemes["Bearer"] = new JsonObject
        {
            ["type"] = "http",
            ["scheme"] = "bearer",
            ["bearerFormat"] = "JWT",
            ["description"] = "Enter a gateway-issued JWT token."
        };
    }

    private static bool RequiresAuth(string rewrittenPath) =>
        rewrittenPath.StartsWith("/api/customers", StringComparison.OrdinalIgnoreCase)
        || rewrittenPath.StartsWith("/api/orders", StringComparison.OrdinalIgnoreCase)
        || rewrittenPath.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase)
        || rewrittenPath.StartsWith("/api/ml", StringComparison.OrdinalIgnoreCase)
        || rewrittenPath.StartsWith("/api/dashboard", StringComparison.OrdinalIgnoreCase)
        || rewrittenPath.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);

    private static string? RewritePath(string service, string rawPath) => service switch
    {
        "auth" => RewriteAuthPath(rawPath),
        "customer" => RewriteCustomerPath(rawPath),
        "order" => RewriteOrderPath(rawPath),
        "product" => RewriteProductPath(rawPath),
        "forecast" => RewriteForecastPath(rawPath),
        "prediction" => RewritePredictionPath(rawPath),
        "analytics" => RewriteAnalyticsPath(rawPath),
        "admin" => RewriteAdminPath(rawPath),
        _ => null
    };

    private static string? RewriteAuthPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/auth/health";
        }

        if (string.Equals(rawPath, "/db-check", StringComparison.OrdinalIgnoreCase))
        {
            return "/auth/db-check";
        }

        return ReplacePrefix(rawPath, "/api/auth", "/api/auth");
    }

    private static string? RewriteCustomerPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/customer/health";
        }

        return ReplacePrefix(rawPath, "/api/customers", "/api/customers");
    }

    private static string? RewriteOrderPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/order/health";
        }

        return ReplacePrefix(rawPath, "/api/orders", "/api/orders")
            ?? ReplacePrefix(rawPath, "/api/Orders", "/api/orders");
    }

    private static string? RewriteProductPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/product/health";
        }

        return ReplacePrefix(rawPath, "/api/products", "/api/products")
            ?? ReplacePrefix(rawPath, "/api/Products", "/api/products");
    }

    private static string? RewriteForecastPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/forecast/health";
        }

        return ReplacePrefix(rawPath, "/api/forecasting/Analytics", "/api/ml/forecast/analytics")
            ?? ReplacePrefix(rawPath, "/api/forecasting/Forecast", "/api/ml/forecast/forecast")
            ?? ReplacePrefix(rawPath, "/api/forecasting/Retraining", "/api/ml/forecast/retraining")
            ?? ReplacePrefix(rawPath, "/api/forecasting/analytics", "/api/ml/forecast/analytics")
            ?? ReplacePrefix(rawPath, "/api/forecasting/forecast", "/api/ml/forecast/forecast")
            ?? ReplacePrefix(rawPath, "/api/forecasting/retraining", "/api/ml/forecast/retraining")
            ?? ReplacePrefix(rawPath, "/api/forecasting", "/api/ml/forecast");
    }

    private static string? RewritePredictionPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/prediction/health";
        }

        if (string.Equals(rawPath, "/db-check", StringComparison.OrdinalIgnoreCase))
        {
            return "/prediction/db-check";
        }

        return ReplacePrefix(rawPath, "/api/ml", "/api/ml");
    }

    private static string? RewriteAnalyticsPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/analytics/health";
        }

        return ReplacePrefix(rawPath, "/api/dashboard", "/api/dashboard")
            ?? ReplacePrefix(rawPath, "/api/Dashboard", "/api/dashboard");
    }

    private static string? RewriteAdminPath(string rawPath)
    {
        if (string.Equals(rawPath, "/health", StringComparison.OrdinalIgnoreCase))
        {
            return "/admin/health";
        }

        return ReplacePrefix(rawPath, "/api/admin", "/api/admin")
            ?? ReplacePrefix(rawPath, "/api/Admin", "/api/admin");
    }

    private static string? ReplacePrefix(string path, string sourcePrefix, string destinationPrefix)
    {
        if (!path.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return destinationPrefix + path[sourcePrefix.Length..];
    }

    private sealed record SwaggerServiceDefinition(string Slug, string DisplayName);
}
