namespace AuthService.Models;

/// <summary>
/// Payload for POST /api/auth/register
/// </summary>
/// <param name="Username">Unique username (required)</param>
/// <param name="Email">Unique email address (required)</param>
/// <param name="Password">Plain-text password (required, hashed before storage)</param>
/// <param name="FullName">Optional display name</param>
/// <param name="Role">Optional role. Accepted values: Manager, Employee. Defaults to Employee.</param>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? FullName = null,
    string? Role = null);
