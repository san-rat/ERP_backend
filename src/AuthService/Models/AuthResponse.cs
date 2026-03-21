namespace AuthService.Models;

/// <summary>
/// Successful authentication response returned from both /register and /login.
/// </summary>
public sealed class AuthResponse
{
    /// <summary>Signed HS256 JWT Bearer token.</summary>
    public required string Token     { get; init; }

    /// <summary>Username embedded in the token.</summary>
    public required string Username  { get; init; }

    /// <summary>Role embedded in the token (Admin | Manager | Employee).</summary>
    public required string Role      { get; init; }

    /// <summary>UTC timestamp when the token expires.</summary>
    public required DateTime ExpiresAt { get; init; }
}
