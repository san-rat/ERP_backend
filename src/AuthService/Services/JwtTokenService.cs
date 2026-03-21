using AuthService.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Services;

/// <summary>
/// Generates signed HS256 JWT tokens.
/// Configuration keys (all under "JwtSettings"):
///   SecretKey     — signing secret (≥32 chars); overridden by env var JWT_SECRET
///   Issuer        — token issuer  (default: InsightERP)
///   Audience      — token audience (default: InsightERP-Users)
///   ExpiryMinutes — token lifetime in minutes (default: 60)
/// </summary>
public sealed class JwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int    _expiryMinutes;

    public JwtTokenService(IConfiguration config)
    {
        // Env var JWT_SECRET takes precedence over appsettings
        _secret        = Environment.GetEnvironmentVariable("JWT_SECRET")
                         ?? config["JwtSettings:SecretKey"]
                         ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        _issuer        = config["JwtSettings:Issuer"]        ?? "InsightERP";
        _audience      = config["JwtSettings:Audience"]      ?? "InsightERP-Users";
        _expiryMinutes = int.TryParse(config["JwtSettings:ExpiryMinutes"], out var mins) ? mins : 60;
    }

    /// <summary>
    /// Generates a JWT token embedding userId, username and role.
    /// </summary>
    /// <param name="userId">Unique user identifier (GUID or int, stored as "sub" claim).</param>
    /// <param name="username">Username stored as ClaimTypes.Name.</param>
    /// <param name="role">Role stored as ClaimTypes.Role (Admin | Manager | Employee).</param>
    /// <returns>Populated <see cref="AuthResponse"/> with the signed token.</returns>
    public AuthResponse GenerateToken(string userId, string username, string role)
    {
        var jti      = Guid.NewGuid().ToString();
        var expires  = DateTime.UtcNow.AddMinutes(_expiryMinutes);
        var key      = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds    = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  userId),
            new Claim(JwtRegisteredClaimNames.Jti,  jti),
            new Claim(ClaimTypes.Name,              username),
            new Claim(ClaimTypes.Role,              role)
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new AuthResponse
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(token),
            Username  = username,
            Role      = role,
            ExpiresAt = expires
        };
    }
}
