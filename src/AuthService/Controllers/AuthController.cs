using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // ── Role definitions ───────────────────────────────────────────────────────

    /// <summary>All valid roles in the system.</summary>
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Manager", "Employee"
    };

    /// <summary>
    /// Roles that users may self-assign on registration.
    /// Admin is excluded — it must be assigned by an existing Admin via a privileged endpoint.
    /// </summary>
    private static readonly HashSet<string> SelfAssignableRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Manager", "Employee"
    };

    // ── Dependencies ──────────────────────────────────────────────────────────
    private readonly JwtTokenService _jwt;
    private readonly IUserRepository  _users;

    public AuthController(JwtTokenService jwt, IUserRepository users)
    {
        _jwt   = jwt;
        _users = users;
    }

    // ── POST /api/auth/register ───────────────────────────────────────────────

    /// <summary>
    /// Register a new user and receive a JWT immediately (auto-login).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email)    ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username, Email, and Password are required." });
        }

        // ── Role validation ────────────────────────────────────────────────
        string role;

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            role = "Employee";
        }
        else if (!AllowedRoles.Contains(request.Role))
        {
            return BadRequest(new
            {
                message      = $"'{request.Role}' is not a valid role.",
                allowedRoles = SelfAssignableRoles.Order(),
                hint         = "Omit the Role field to default to Employee."
            });
        }
        else if (!SelfAssignableRoles.Contains(request.Role))
        {
            return BadRequest(new
            {
                message      = $"Role '{request.Role}' cannot be self-assigned during registration.",
                allowedRoles = SelfAssignableRoles.Order(),
                hint         = "Admin accounts must be created by an existing Admin."
            });
        }
        else
        {
            role = SelfAssignableRoles.First(
                r => r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        }

        // ── Check username uniqueness ──────────────────────────────────────
        if (await _users.UsernameExistsAsync(request.Username))
        {
            return Conflict(new { message = $"Username '{request.Username}' is already taken." });
        }

        // ── Create user in DB ──────────────────────────────────────────────
        var userId = await _users.CreateUserAsync(
            request.Username,
            request.Email,
            HashPassword(request.Password),
            request.FullName,
            role);

        var response = _jwt.GenerateToken(userId.ToString(), request.Username, role);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    /// <summary>
    /// Authenticate with username + password and receive a JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByUsernameAsync(request.Username);

        if (user is null || user.PasswordHash != HashPassword(request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is disabled." });
        }

        var response = _jwt.GenerateToken(user.Id.ToString(), user.Username, user.Role);
        return Ok(response);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Simple SHA-256 password hash.
    /// Note: upgrade to BCrypt/Argon2 in a future sprint.
    /// </summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}