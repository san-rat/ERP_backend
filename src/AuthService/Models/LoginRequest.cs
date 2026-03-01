namespace AuthService.Models;

/// <summary>
/// Payload for POST /api/auth/login
/// </summary>
/// <param name="Username">Username of the registered user</param>
/// <param name="Password">Plain-text password to validate</param>
public record LoginRequest(string Username, string Password);
