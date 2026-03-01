using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// API Gateway Health and Information Endpoints
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class GatewayController : ControllerBase
    {
        private readonly ILogger<GatewayController> _logger;

        public GatewayController(ILogger<GatewayController> logger)
        {
            _logger = logger;
        }

        /// <summary>Returns 200 OK when gateway is healthy. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("health")]
        [HttpHead("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("Health check requested");
            return Ok(new
            {
                service = Assembly.GetExecutingAssembly().GetName().Name,
                status = "healthy",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                timestamp = DateTime.UtcNow,
                uptime = GetUptime()
            });
        }

        /// <summary>Returns gateway info, features and registered routes. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            _logger.LogInformation("Gateway info requested");
            return Ok(new
            {
                service = "InsightERP API Gateway",
                description = "Ocelot API Gateway with static service registry",
                version = "1.0.0",
                framework = ".NET 9.0",
                apiGateway = "Ocelot 24.1.0",
                features = new[]
                {
                    "Static Service Registry",
                    "Rate Limiting",
                    "JWT Authentication",
                    "CORS Support",
                    "Circuit Breaker (QoS)",
                    "Request Logging",
                    "Health Checks"
                },
                endpoints = new
                {
                    authentication  = "/api/auth/*",
                    customers       = "/api/customers/*",
                    products        = "/api/products/*",
                    orders          = "/api/orders/*",
                    ml_churn        = "/api/ml/churn/*",
                    ml_segmentation = "/api/ml/segmentation/*",
                    ml_forecast     = "/api/ml/forecast/*",
                    reports         = "/api/reports/*",
                    chatbot         = "/api/chatbot/*",
                    notifications   = "/api/notifications/*",
                    dashboard       = "/api/dashboard/*"
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Returns all statically registered downstream services. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("services")]
        public IActionResult GetServices()
        {
            _logger.LogInformation("Services info requested");
            return Ok(new
            {
                totalServices = 7,
                services = new[]
                {
                    new { name = "authentication-service", host = "authentication-service", port = 5001 },
                    new { name = "core-erp-service",       host = "core-erp-service",       port = 5002 },
                    new { name = "ml-service",             host = "ml-service",             port = 5005 },
                    new { name = "report-service",         host = "report-service",         port = 5006 },
                    new { name = "chatbot-service",        host = "chatbot-service",        port = 5007 },
                    new { name = "notification-service",   host = "notification-service",   port = 5008 },
                    new { name = "dashboard-service",      host = "dashboard-service",      port = 5009 }
                },
                note = "Hosts resolve via Docker network by container name",
                swaggerUI = "http://localhost:5000/swagger",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Returns gateway route table. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            _logger.LogInformation("Routes info requested");
            return Ok(new
            {
                totalRoutes = 11,
                routes = new object[]
                {
                    new { upstream = "/api/auth/*",            downstream = "authentication-service:5001", auth = false },
                    new { upstream = "/api/customers/*",       downstream = "core-erp-service:5002",       auth = true  },
                    new { upstream = "/api/products/*",        downstream = "core-erp-service:5002",       auth = true  },
                    new { upstream = "/api/orders/*",          downstream = "core-erp-service:5002",       auth = true  },
                    new { upstream = "/api/ml/churn/*",        downstream = "ml-service:5005",             auth = true  },
                    new { upstream = "/api/ml/segmentation/*", downstream = "ml-service:5005",             auth = true  },
                    new { upstream = "/api/ml/forecast/*",     downstream = "ml-service:5005",             auth = true  },
                    new { upstream = "/api/reports/*",         downstream = "report-service:5006",         auth = true  },
                    new { upstream = "/api/chatbot/*",         downstream = "chatbot-service:5007",        auth = true  },
                    new { upstream = "/api/notifications/*",   downstream = "notification-service:5008",   auth = true  },
                    new { upstream = "/api/dashboard/*",       downstream = "dashboard-service:5009",      auth = true  }
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Kubernetes liveness probe. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("live")]
        [HttpHead("live")]
        public IActionResult IsAlive() =>
            Ok(new { status = "alive", timestamp = DateTime.UtcNow });

        /// <summary>Kubernetes readiness probe. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("ready")]
        [HttpHead("ready")]
        public IActionResult IsReady() =>
            Ok(new { status = "ready", timestamp = DateTime.UtcNow });

        private static string GetUptime()
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
        }
    }
}