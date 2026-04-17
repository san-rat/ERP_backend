using System.Text.Json;
using Xunit;

namespace ApiGateway.Tests;

public class OcelotRouteContractTests
{
    public static IEnumerable<object[]> OcelotFiles =>
    [
        new object[] { "src/ApiGateway/ocelot.json" },
        new object[] { "src/ApiGateway/ocelot.Docker.json" },
        new object[] { "src/ApiGateway/ocelot.Dev.json" },
        new object[] { "src/ApiGateway/ocelot.Production.json" },
    ];

    [Theory]
    [MemberData(nameof(OcelotFiles))]
    public void MlGatewayRoutes_AreAlignedAcrossEnvironments(string relativePath)
    {
        using var document = Load(relativePath);

        AssertRoute(document, "/api/ml/forecast/{everything}", "/api/forecasting/{everything}");
        AssertRoute(document, "/api/ml/churn/{everything}", "/api/ml/churn/{everything}");
        AssertRoute(document, "/api/ml/model-management/{everything}", "/api/ml/{everything}");
        AssertRoute(document, "/api/ml/{everything}", "/api/ml/{everything}");
        AssertRoute(document, "/auth/db-check", "/db-check");
        AssertRoute(document, "/prediction/db-check", "/db-check");
    }

    [Theory]
    [MemberData(nameof(OcelotFiles))]
    public void SwaggerProxyRoutes_ExistForAllGatewayHostedServiceDocs(string relativePath)
    {
        using var document = Load(relativePath);

        foreach (var service in new[] { "auth", "customer", "order", "product", "forecast", "prediction", "analytics", "admin" })
        {
            AssertRoute(document, $"/{service}/swagger/{{everything}}", "/swagger/{everything}");
        }
    }

    [Theory]
    [MemberData(nameof(OcelotFiles))]
    public void SpecificMlRoutes_AppearBeforeGenericPredictionRoute(string relativePath)
    {
        using var document = Load(relativePath);

        var genericPredictionRouteIndex = FindRouteIndex(document, "/api/ml/{everything}");
        Assert.True(FindRouteIndex(document, "/api/ml/forecast/{everything}") < genericPredictionRouteIndex);
        Assert.True(FindRouteIndex(document, "/api/ml/churn/{everything}") < genericPredictionRouteIndex);
        Assert.True(FindRouteIndex(document, "/api/ml/model-management/{everything}") < genericPredictionRouteIndex);
    }

    private static JsonDocument Load(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        var filePath = Path.Combine(repoRoot, relativePath);
        return JsonDocument.Parse(File.ReadAllText(filePath));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "src/ApiGateway/ocelot.json")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test output directory.");
    }

    private static void AssertRoute(JsonDocument document, string upstreamPath, string downstreamPath)
    {
        var route = FindRoute(document, upstreamPath);
        Assert.Equal(downstreamPath, route.GetProperty("DownstreamPathTemplate").GetString());
    }

    private static JsonElement FindRoute(JsonDocument document, string upstreamPath)
    {
        foreach (var route in document.RootElement.GetProperty("Routes").EnumerateArray())
        {
            if (string.Equals(route.GetProperty("UpstreamPathTemplate").GetString(), upstreamPath, StringComparison.Ordinal))
            {
                return route;
            }
        }

        throw new Xunit.Sdk.XunitException($"Route '{upstreamPath}' was not found.");
    }

    private static int FindRouteIndex(JsonDocument document, string upstreamPath)
    {
        var index = 0;
        foreach (var route in document.RootElement.GetProperty("Routes").EnumerateArray())
        {
            if (string.Equals(route.GetProperty("UpstreamPathTemplate").GetString(), upstreamPath, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        throw new Xunit.Sdk.XunitException($"Route '{upstreamPath}' was not found.");
    }
}
