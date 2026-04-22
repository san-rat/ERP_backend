using AuthService;

namespace AuthService.Tests;

public class JwtSecretResolverTests
{
    private const string ValidSecret = "valid-signing-secret-at-least-32-characters-long!!";

    // ── env var is accepted ───────────────────────────────────────────────────

    [Fact]
    public void ValidEnvSecret_IsAccepted_OutsideProduction()
    {
        var (secret, source) = JwtSecretResolver.Resolve(ValidSecret, null, isProduction: false);
        Assert.Equal(ValidSecret, secret);
        Assert.Contains("JWT_SECRET", source);
    }

    [Fact]
    public void ValidEnvSecret_IsAccepted_InProduction()
    {
        var (secret, source) = JwtSecretResolver.Resolve(ValidSecret, null, isProduction: true);
        Assert.Equal(ValidSecret, secret);
        Assert.Contains("JWT_SECRET", source);
    }

    // ── env var rejection ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceEnvSecret_FallsThrough_ToConfigFallback(string empty)
    {
        var (secret, _) = JwtSecretResolver.Resolve(empty, ValidSecret, isProduction: false);
        Assert.Equal(ValidSecret, secret);
    }

    [Fact]
    public void PlaceholderEnvSecret_IsRejected_AndFallsThrough()
    {
        var (secret, _) = JwtSecretResolver.Resolve("${JWT_SECRET_KEY}", ValidSecret, isProduction: false);
        Assert.Equal(ValidSecret, secret);
    }

    [Fact]
    public void EmptyEnvSecret_InProduction_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve("", null, isProduction: true));
    }

    [Fact]
    public void PlaceholderEnvSecret_InProduction_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve("${JWT_SECRET_KEY}", null, isProduction: true));
    }

    // ── config fallback ───────────────────────────────────────────────────────

    [Fact]
    public void ConfigFallback_IsAccepted_OutsideProduction()
    {
        var (secret, source) = JwtSecretResolver.Resolve(null, ValidSecret, isProduction: false);
        Assert.Equal(ValidSecret, secret);
        Assert.Contains("JwtSettings:SecretKey", source);
    }

    [Fact]
    public void ConfigFallback_IsRejected_InProduction()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve(null, ValidSecret, isProduction: true));
        Assert.Contains("Production", ex.Message);
    }

    [Fact]
    public void PlaceholderConfigSecret_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve(null, "${JWT_SECRET_KEY}", isProduction: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyOrWhitespaceConfigSecret_IsRejected(string empty)
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve(null, empty, isProduction: false));
    }

    // ── both null/missing ─────────────────────────────────────────────────────

    [Fact]
    public void BothNull_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve(null, null, isProduction: false));
    }

    [Fact]
    public void BothNull_InProduction_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretResolver.Resolve(null, null, isProduction: true));
    }

    // ── env var takes precedence over config ──────────────────────────────────

    [Fact]
    public void EnvSecret_TakesPrecedenceOver_ConfigSecret()
    {
        const string envVal    = "env-secret-that-is-at-least-32-chars-long!!";
        const string configVal = "config-secret-that-is-at-least-32-chars-long!!";

        var (secret, source) = JwtSecretResolver.Resolve(envVal, configVal, isProduction: false);

        Assert.Equal(envVal, secret);
        Assert.Contains("JWT_SECRET", source);
    }
}
