using AuthService.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="JwtTokenService"/>.
/// </summary>
public class JwtTokenServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static JwtTokenService BuildService(
        string secret        = "SuperSecretKeyForTestingPurposesOnly1234",
        string issuer        = "InsightERP-Test",
        string audience      = "InsightERP-Users-Test",
        string expiryMinutes = "60")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]     = secret,
                ["JwtSettings:Issuer"]        = issuer,
                ["JwtSettings:Audience"]      = audience,
                ["JwtSettings:ExpiryMinutes"] = expiryMinutes
            })
            .Build();

        return new JwtTokenService(config);
    }

    // ── Construction ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithMissingSecretKey_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        var ex = Record.Exception(() => BuildService());
        Assert.Null(ex);
    }

    // ── GenerateToken — AuthResponse shape ────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var svc = BuildService();
        var response = svc.GenerateToken("user-001", "alice", "Employee");

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public void GenerateToken_TokenIsWellFormedJwt()
    {
        var svc = BuildService();
        var response = svc.GenerateToken("user-001", "alice", "Employee");

        // A compact JWT always has exactly two '.' separators
        Assert.Equal(2, response.Token.Count(c => c == '.'));
    }

    [Fact]
    public void GenerateToken_Returns_CorrectUsername()
    {
        var svc = BuildService();
        var response = svc.GenerateToken("user-001", "alice", "Employee");

        Assert.Equal("alice", response.Username);
    }

    [Fact]
    public void GenerateToken_Returns_CorrectRole()
    {
        var svc = BuildService();
        var response = svc.GenerateToken("user-001", "alice", "Manager");

        Assert.Equal("Manager", response.Role);
    }

    [Fact]
    public void GenerateToken_ExpiresAt_IsInTheFuture()
    {
        var svc      = BuildService();
        var before   = DateTime.UtcNow;
        var response = svc.GenerateToken("user-001", "alice", "Admin");

        Assert.True(response.ExpiresAt > before);
    }

    [Fact]
    public void GenerateToken_ExpiryReflectsConfiguredMinutes()
    {
        var svc      = BuildService(expiryMinutes: "30");
        var before   = DateTime.UtcNow;
        var response = svc.GenerateToken("u", "bob", "Employee");

        // Allow ±5 s tolerance for test execution time
        var expected = before.AddMinutes(30);
        Assert.True(Math.Abs((response.ExpiresAt - expected).TotalSeconds) < 5);
    }

    // ── JWT claims (decoded without signature verification) ───────────────────

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Employee")]
    public void GenerateToken_JwtContains_RoleClaim(string role)
    {
        var svc      = BuildService();
        var response = svc.GenerateToken("user-x", "tester", role);
        var handler  = new JwtSecurityTokenHandler();
        var jwt      = handler.ReadJwtToken(response.Token);

        // ClaimTypes.Role maps to the long URI; we check both
        var roleClaim = jwt.Claims.FirstOrDefault(c =>
            c.Type == "role" ||
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

        Assert.NotNull(roleClaim);
        Assert.Equal(role, roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_JwtContains_SubClaim_MatchingUserId()
    {
        var svc      = BuildService();
        var response = svc.GenerateToken("user-42", "charlie", "Manager");
        var handler  = new JwtSecurityTokenHandler();
        var jwt      = handler.ReadJwtToken(response.Token);

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(sub);
        Assert.Equal("user-42", sub.Value);
    }

    [Fact]
    public void GenerateToken_JwtContains_JtiClaim()
    {
        var svc      = BuildService();
        var response = svc.GenerateToken("u1", "diana", "Employee");
        var handler  = new JwtSecurityTokenHandler();
        var jwt      = handler.ReadJwtToken(response.Token);

        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jti);
        Assert.False(string.IsNullOrWhiteSpace(jti.Value));
    }

    [Fact]
    public void GenerateToken_JtiIsUnique_ForEachCall()
    {
        var svc = BuildService();
        var handler = new JwtSecurityTokenHandler();

        var jwt1 = handler.ReadJwtToken(svc.GenerateToken("u", "a", "Admin").Token);
        var jwt2 = handler.ReadJwtToken(svc.GenerateToken("u", "a", "Admin").Token);

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }

    [Fact]
    public void GenerateToken_JwtContains_CorrectIssuerAndAudience()
    {
        var svc     = BuildService(issuer: "MyIssuer", audience: "MyAudience");
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(svc.GenerateToken("u", "eve", "Employee").Token);

        Assert.Equal("MyIssuer",   jwt.Issuer);
        Assert.Contains("MyAudience", jwt.Audiences);
    }

    // ── Defaults when config keys are absent ──────────────────────────────────

    [Fact]
    public void GenerateToken_DefaultsToInsightERP_WhenIssuerNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "SuperSecretKeyForTestingPurposesOnly1234"
                // Issuer, Audience, ExpiryMinutes intentionally omitted
            })
            .Build();

        var svc     = new JwtTokenService(config);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(svc.GenerateToken("u", "frank", "Admin").Token);

        Assert.Equal("InsightERP", jwt.Issuer);
        Assert.Contains("InsightERP-Users", jwt.Audiences);
    }
}
