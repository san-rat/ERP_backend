using AuthService.Controllers;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace AuthService.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// </summary>
public class AuthControllerTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

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

    private static string HashPassword(string password)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// A stateful fake repository to mimic database behavior in unit tests.
    /// Replaces the old ConcurrentDictionary previously used within AuthController.
    /// </summary>
    private class FakeUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, DbUser> _users = new(StringComparer.OrdinalIgnoreCase);

        public FakeUserRepository()
        {
            // Seed the same initial accounts expected by the unit tests.
            _users["admin"]    = new DbUser(Guid.NewGuid(), "admin",    "admin@test.com", HashPassword("Admin@123"),    "Admin",    true, "Admin");
            _users["manager"]  = new DbUser(Guid.NewGuid(), "manager",  "mgr@test.com",   HashPassword("Manager@123"),  "Manager",  true, "Manager");
            _users["employee"] = new DbUser(Guid.NewGuid(), "employee", "emp@test.com",   HashPassword("Employee@123"), "Employee", true, "Employee");
        }

        public Task<DbUser?> FindByUsernameAsync(string username)
        {
            _users.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> UsernameExistsAsync(string username)
        {
            return Task.FromResult(_users.ContainsKey(username));
        }

        public Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string? fullName, string roleName)
        {
            var id = Guid.NewGuid();
            var user = new DbUser(id, username, email, passwordHash, fullName, true, roleName);
            _users.TryAdd(username, user);
            return Task.FromResult(id);
        }
    }

    private static AuthController BuildController(IUserRepository? repo = null) =>
        new(BuildJwtService(), repo ?? new FakeUserRepository());

    private static string UniqueUser(string prefix = "user") =>
        $"{prefix}_{Guid.NewGuid():N}";

    // ═══════════════════════════════════════════════════════════════════════════
    // POST /api/auth/register
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Register_WithValidCredentials_Returns201WithToken()
    {
        var ctrl = BuildController();
        var username = UniqueUser("reg");

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(username, response.Username);
        Assert.Equal("Employee", response.Role);   // default role
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithNoRoleSupplied_DefaultsToEmployee()
    {
        var ctrl = BuildController();
        var username = UniqueUser();

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public async Task Register_WithManagerRole_Returns201WithManagerRole()
    {
        var ctrl = BuildController();
        var username = UniqueUser();

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234", "Full Name", "Manager"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Manager", response.Role);
    }

    [Fact]
    public async Task Register_WithEmployeeRole_Returns201WithEmployeeRole()
    {
        var ctrl = BuildController();
        var username = UniqueUser();

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234", "Full Name", "Employee"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public async Task Register_WithAdminRole_Returns400_AdminCannotBeSelfAssigned()
    {
        var ctrl = BuildController();
        var username = UniqueUser();

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234", "Full Name", "Admin"));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);

        var message = GetProperty(bad.Value, "message")?.ToString();
        Assert.Contains("Admin", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be self-assigned", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithUnknownRole_Returns400_InvalidRole()
    {
        var ctrl = BuildController();
        var username = UniqueUser();

        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "P@ss1234", "Full Name", "SuperHero"));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var message = GetProperty(bad.Value, "message")?.ToString();
        Assert.Contains("SuperHero", message);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns409Conflict()
    {
        var ctrl = BuildController();
        var username = UniqueUser("dup");

        await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "First@123"));                  // first
        var result = await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", "Second@456"));    // duplicate

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var message = GetProperty(conflict.Value, "message")?.ToString();
        Assert.Contains(username, message);
    }

    [Theory]
    [InlineData("", "test@test.com", "P@ss1234")]
    [InlineData("  ", "test@test.com", "P@ss1234")]
    [InlineData("testuser", "", "P@ss1234")]
    [InlineData("testuser", "  ", "P@ss1234")]
    [InlineData("testuser", "test@test.com", "")]
    [InlineData("testuser", "test@test.com", "  ")]
    public async Task Register_WithMissingRequiredFields_Returns400(string username, string email, string password)
    {
        var ctrl = BuildController();

        var result = await ctrl.Register(new RegisterRequest(username, email, password));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POST /api/auth/login
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_WithSeededAdminCredentials_Returns200WithToken()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("admin", "Admin@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("admin", response.Username);
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public async Task Login_WithSeededManagerCredentials_Returns200WithManagerRole()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("manager", "Manager@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal("Manager", response.Role);
    }

    [Fact]
    public async Task Login_WithSeededEmployeeCredentials_Returns200WithEmployeeRole()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("employee", "Employee@123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal("Employee", response.Role);
    }

    [Fact]
    public async Task Login_AfterRegistration_Returns200WithToken()
    {
        var repo = new FakeUserRepository();
        var ctrl = BuildController(repo);
        var username = UniqueUser("logintest");
        const string password = "MyPass@999";

        await ctrl.Register(new RegisterRequest(username, $"{username}@test.com", password, "Full Name", "Manager"));

        var result = await ctrl.Login(new LoginRequest(username, password));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(username, response.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("admin", "WrongPassword!"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var message = GetProperty(unauthorized.Value, "message")?.ToString();
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public async Task Login_WithNonExistentUsername_Returns401()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("nobody_exists_xxx", "AnyPass@1"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var message = GetProperty(unauthorized.Value, "message")?.ToString();
        Assert.Equal("Invalid credentials.", message);
    }

    [Fact]
    public async Task Login_WithCaseSensitivePassword_Returns401_WhenCaseWrong()
    {
        var ctrl = BuildController();

        var result = await ctrl.Login(new LoginRequest("admin", "admin@123")); // lowercase 'a'

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Token structure (spot-checks via AuthResponse)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ReturnedToken_HasThreeParts_WellFormedJwt()
    {
        var ctrl = BuildController();

        var ok = Assert.IsType<OkObjectResult>(
            await ctrl.Login(new LoginRequest("admin", "Admin@123")));
        var response = Assert.IsType<AuthResponse>(ok.Value);

        Assert.Equal(2, response.Token.Count(c => c == '.'));
    }

    [Fact]
    public async Task Login_ExpiresAt_IsInTheFuture()
    {
        var ctrl = BuildController();

        var ok = Assert.IsType<OkObjectResult>(
            await ctrl.Login(new LoginRequest("admin", "Admin@123")));
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
