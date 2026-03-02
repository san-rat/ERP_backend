using AuthService.Controllers;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
///
/// Design note: AuthController uses a static ConcurrentDictionary seeded with
/// three accounts (admin/manager/employee). To avoid cross-test pollution every
/// test that registers a new user uses a unique username via Guid.NewGuid().
/// </summary>
public class AuthControllerTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="JwtTokenService"/> wired to an in-memory config with
    /// a 256-bit secret, so no environment variable is needed during tests.
    /// </summary>
    private static JwtTokenService BuildJwtService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]     = "SuperSecretKeyForTestingPurposesOnly1234",
                ["JwtSettings:Issuer"]        = "InsightERP-Test",
                ["JwtSettings:Audience"]      = "InsightERP-Users-Test",
                ["JwtSettings:ExpiryMinutes"] = "60"
            })
            .Build());

    private static AuthController BuildController() =>
        new(BuildJwtService());

    /// <summary>Returns a unique username so parallel tests don't collide.</summary>
    private static string UniqueUser(string prefix = "user") =>
        $"{prefix}_{Guid.NewGuid():N}";

    // ═══════════════════════════════════════════════════════════════════════════
    // POST /api/auth/register
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Register_WithValidCredentials_Returns201WithToken()
    {
        var ctrl = BuildController();
        var username = UniqueUser("reg");

        var result = ctrl.Register(new RegisterRequest(username, "P@ss1234"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(username, response.Username);
        Assert.Equal("Employee", response.Role);   // default role
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Register_WithNoRoleSupplied_DefaultsToEmployee()
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(UniqueUser(), "P@ss1234"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public void Register_WithManagerRole_Returns201WithManagerRole()
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(UniqueUser(), "P@ss1234", "Manager"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Manager", response.Role);
    }

    [Fact]
    public void Register_WithEmployeeRole_Returns201WithEmployeeRole()
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(UniqueUser(), "P@ss1234", "Employee"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public void Register_WithAdminRole_Returns400_AdminCannotBeSelfAssigned()
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(UniqueUser(), "P@ss1234", "Admin"));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);

        var message = GetProperty(bad.Value, "message")?.ToString();
        Assert.Contains("Admin", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be self-assigned", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_WithUnknownRole_Returns400_InvalidRole()
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(UniqueUser(), "P@ss1234", "SuperHero"));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetProperty(bad.Value, "message")?.ToString();
        Assert.Contains("SuperHero", message);
    }

    [Fact]
    public void Register_WithDuplicateUsername_Returns409Conflict()
    {
        var ctrl = BuildController();
        var username = UniqueUser("dup");

        ctrl.Register(new RegisterRequest(username, "First@123"));                  // first
        var result = ctrl.Register(new RegisterRequest(username, "Second@456"));    // duplicate

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var message = GetProperty(conflict.Value, "message")?.ToString();
        Assert.Contains(username, message);
    }

    [Theory]
    [InlineData("", "P@ss1234")]
    [InlineData("  ", "P@ss1234")]
    [InlineData("testuser", "")]
    [InlineData("testuser", "  ")]
    public void Register_WithMissingUsernameOrPassword_Returns400(string username, string password)
    {
        var ctrl = BuildController();

        var result = ctrl.Register(new RegisterRequest(username, password));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POST /api/auth/login
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Login_WithSeededAdminCredentials_Returns200WithToken()
    {
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("admin", "Admin@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("admin", response.Username);
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public void Login_WithSeededManagerCredentials_Returns200WithManagerRole()
    {
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("manager", "Manager@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal("Manager", response.Role);
    }

    [Fact]
    public void Login_WithSeededEmployeeCredentials_Returns200WithEmployeeRole()
    {
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("employee", "Employee@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public void Login_AfterRegistration_Returns200WithToken()
    {
        var ctrl = BuildController();
        var username = UniqueUser("logintest");
        const string password = "MyPass@999";

        ctrl.Register(new RegisterRequest(username, password, "Manager"));

        var result = ctrl.Login(new LoginRequest(username, password));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(username, response.Username);
    }

    [Fact]
    public void Login_WithWrongPassword_Returns401()
    {
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("admin", "WrongPassword!"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var message = GetProperty(unauthorized.Value, "message")?.ToString();
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public void Login_WithNonExistentUsername_Returns401()
    {
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("nobody_exists_xxx", "AnyPass@1"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var message = GetProperty(unauthorized.Value, "message")?.ToString();
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public void Login_WithCaseSensitivePassword_Returns401_WhenCaseWrong()
    {
        // Passwords are SHA-256 hashed — casing matters
        var ctrl = BuildController();

        var result = ctrl.Login(new LoginRequest("admin", "admin@123")); // lowercase 'a'

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Token structure (spot-checks via AuthResponse)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Login_ReturnedToken_HasThreeParts_WellFormedJwt()
    {
        var ctrl = BuildController();

        var ok = Assert.IsType<OkObjectResult>(
            ctrl.Login(new LoginRequest("admin", "Admin@123")));
        var response = Assert.IsType<AuthResponse>(ok.Value);

        // A valid JWT always contains exactly two dots
        Assert.Equal(2, response.Token.Count(c => c == '.'));
    }

    [Fact]
    public void Login_ExpiresAt_IsInTheFuture()
    {
        var ctrl = BuildController();

        var ok = Assert.IsType<OkObjectResult>(
            ctrl.Login(new LoginRequest("admin", "Admin@123")));
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    // ── Reflection helper ─────────────────────────────────────────────────────

    private static object? GetProperty(object? obj, string propertyName)
    {
        if (obj is null) return null;
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }
}
