namespace AuthService;

public static class JwtSecretResolver
{
    /// <summary>
    /// Resolves the JWT signing secret using a fail-fast precedence contract:
    ///   1. JWT_SECRET env var (all environments)
    ///   2. JwtSettings:SecretKey config key (non-Production only)
    ///
    /// Empty, whitespace, and placeholder values (${...}) are always rejected.
    /// In Production, only the env var is accepted; startup aborts if it is missing.
    /// </summary>
    /// <returns>A tuple of (resolvedSecret, sourceDescription).</returns>
    public static (string Secret, string Source) Resolve(
        string? envSecret,
        string? configSecret,
        bool isProduction)
    {
        if (!string.IsNullOrWhiteSpace(envSecret) && !envSecret.StartsWith("${"))
            return (envSecret, "JWT_SECRET env var");

        if (isProduction)
            throw new InvalidOperationException(
                "JWT_SECRET environment variable is required in Production. " +
                "JwtSettings:SecretKey is not accepted as a production secret source. " +
                "Ensure JWT_SECRET is injected via Azure Container Apps secrets.");

        if (!string.IsNullOrWhiteSpace(configSecret) && !configSecret.StartsWith("${"))
            return (configSecret, "JwtSettings:SecretKey (config fallback)");

        throw new InvalidOperationException(
            "No valid JWT secret configured. " +
            "Set the JWT_SECRET environment variable or JwtSettings:SecretKey in appsettings.json.");
    }
}
