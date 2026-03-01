namespace AuthService.Models;

/// <summary>
/// Payload for POST /api/auth/register
/// </summary>
/// <param name="Username">Unique username (required)</param>
/// <param name="Password">Plain-text password (required, hashed before storage)</param>
/// <param name="Role">Optional role override. Accepted values: Admin, Manager, Employee.
///                    Defaults to <c>Employee</c> when omitted or unrecognised.</param>
public record RegisterRequest(string Username, string Password, string? Role = null);
