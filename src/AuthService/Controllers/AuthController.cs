using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // ── In-memory user store ────────────────────────────────────────────────────
    // userId → UserRecord (thread-safe, replaces DB for this iteration)
    private static readonly ConcurrentDictionary<string, UserRecord> _users = new(
        StringComparer.OrdinalIgnoreCase);

    // Pre-seeded test accounts — one per role
    static AuthController()
    {
        Seed("admin-001",    "admin",    "Admin@123",   "Admin");
        Seed("manager-001",  "manager",  "Manager@123", "Manager");
        Seed("employee-001", "employee", "Employee@123","Employee");
    }

    private static void Seed(string id, string username, string password, string role) =>
        _users[username] = new UserRecord(id, username, HashPassword(password), role);

    // ── Role definitions ──────────────────────────────────────────────────────────

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

    // ── Dependencies ─────────────────────────────────────────────────────────────
    private readonly JwtTokenService _jwt;

    public AuthController(JwtTokenService jwt) => _jwt = jwt;

    // ── POST /api/auth/register ──────────────────────────────────────────────────

    /// <summary>
    /// Register a new user and receive a JWT immediately (auto-login).
    /// <para>
    /// <b>Role rules:</b><br/>
    /// • Omit <c>Role</c> → defaults to <c>Employee</c>.<br/>
    /// • Supply <c>"Manager"</c> or <c>"Employee"</c> → accepted.<br/>
    /// • Supply <c>"Admin"</c> → 400 Bad Request (Admin cannot be self-assigned).<br/>
    /// • Supply any other string → 400 Bad Request.
    /// </para>
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and Password are required." });
        }

        // ── Role validation ───────────────────────────────────────────────────
        string role;

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            // No role supplied → default to Employee
            role = "Employee";
        }
        else if (!AllowedRoles.Contains(request.Role))
        {
            // Unknown role string supplied
            return BadRequest(new
            {
                message        = $"'{request.Role}' is not a valid role.",
                allowedRoles   = SelfAssignableRoles.Order(),
                hint           = "Omit the Role field to default to Employee."
            });
        }
        else if (!SelfAssignableRoles.Contains(request.Role))
        {
            // Known role but not self-assignable (i.e., Admin)
            return BadRequest(new
            {
                message      = $"Role '{request.Role}' cannot be self-assigned during registration.",
                allowedRoles = SelfAssignableRoles.Order(),
                hint         = "Admin accounts must be created by an existing Admin."
            });
        }
        else
        {
            // Valid, self-assignable role — use exactly as supplied (preserves casing normalisation)
            role = SelfAssignableRoles.First(
                r => r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        }


        var userId = Guid.NewGuid().ToString();
        var record = new UserRecord(userId, request.Username, HashPassword(request.Password), role);

        // Reject duplicate usernames
        if (!_users.TryAdd(request.Username, record))
        {
            return Conflict(new { message = $"Username '{request.Username}' is already taken." });
        }

        var response = _jwt.GenerateToken(userId, request.Username, role);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // ── POST /api/auth/login ─────────────────────────────────────────────────────

    /// <summary>
    /// Authenticate with username + password and receive a JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!_users.TryGetValue(request.Username, out var user))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var response = _jwt.GenerateToken(user.UserId, user.Username, user.Role);
        return Ok(response);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simple SHA-256 password hash (production should use BCrypt/Argon2).
    /// </summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Internal user record ─────────────────────────────────────────────────────
internal sealed record UserRecord(
    string UserId,
    string Username,
    string PasswordHash,
    string Role);