using AuthService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AuthService.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="HealthController"/>.
///
/// The GET /health endpoint is anonymous and has no dependencies beyond IConfiguration.
/// GET /db-check hits a real MySQL — we keep that out of unit tests and only verify
/// the controller's error path via a bad connection string (fast-fail).
/// </summary>
public class HealthControllerTests
{
    // ── Health endpoint ────────────────────────────────────────────────────────

    [Fact]
    public void Health_ReturnsOkResult()
    {
        var config = new Mock<IConfiguration>();
        var ctrl   = new HealthController(config.Object);

        var result = ctrl.Health();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Health_ReturnsExpectedMessage()
    {
        var config = new Mock<IConfiguration>();
        var ctrl   = new HealthController(config.Object);

        var ok = Assert.IsType<OkObjectResult>(ctrl.Health());

        var body = Assert.IsType<string>(ok.Value);
        Assert.Contains("AuthService is running", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Health_ResponseIsConsistent_On_MultipleCallsRequests()
    {
        var config = new Mock<IConfiguration>();
        var ctrl   = new HealthController(config.Object);

        // Health is idempotent — call it twice, same result
        var first  = Assert.IsType<OkObjectResult>(ctrl.Health());
        var second = Assert.IsType<OkObjectResult>(ctrl.Health());

        Assert.Equal(first.Value, second.Value);
    }

    // ── DbCheck endpoint ───────────────────────────────────────────────────────
    // We test only the failure path here because a real DB isn't available in CI.

    [Fact]
    public async Task DbCheck_WithBadConnectionString_ReturnsProblemResult()
    {
        // Arrange: provide a deliberately broken connection string
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Server=invalid_host;Port=3306;Database=notexist;Uid=none;Pwd=none;"
            })
            .Build();

        var ctrl = new HealthController(config);

        // Act
        var result = await ctrl.DbCheck();

        // Assert: must return a 500-level problem detail
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task DbCheck_WithNullConnectionString_ReturnsProblemResult()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ctrl = new HealthController(config);

        var result = await ctrl.DbCheck();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
