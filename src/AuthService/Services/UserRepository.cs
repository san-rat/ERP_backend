using Microsoft.Data.SqlClient;

namespace AuthService.Services;

/// <summary>
/// Handles all database operations for the auth.users and auth.roles tables.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IConfiguration _config;

    public UserRepository(IConfiguration config)
    {
        _config = config;
    }

    private SqlConnection CreateConnection()
        => new SqlConnection(_config.GetConnectionString("AuthDb"));

    // ── Find user by username ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the user record matching the given username, or null if not found.
    /// </summary>
    public async Task<DbUser?> FindByUsernameAsync(string username)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(@"
            SELECT TOP 1 u.id, u.username, u.email, u.password_hash, u.full_name, u.is_active, r.role_name
            FROM auth.users u
            LEFT JOIN auth.user_roles ur ON u.id = ur.user_id
            LEFT JOIN auth.roles r ON ur.role_id = r.id
            WHERE u.username = @username
            ORDER BY
                CASE UPPER(ISNULL(r.role_name, ''))
                    WHEN 'ADMIN' THEN 0
                    WHEN 'MANAGER' THEN 1
                    WHEN 'EMPLOYEE' THEN 2
                    WHEN 'USER' THEN 2
                    ELSE 3
                END,
                CASE r.role_name
                    WHEN 'Admin' THEN 0
                    WHEN 'Manager' THEN 1
                    WHEN 'Employee' THEN 2
                    ELSE 3
                END", conn);

        cmd.Parameters.AddWithValue("@username", username);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new DbUser(
            Id:           reader.GetGuid(0),
            Username:     reader.GetString(1),
            Email:        reader.GetString(2),
            PasswordHash: reader.GetString(3),
            FullName:     reader.IsDBNull(4) ? null : reader.GetString(4),
            IsActive:     reader.GetBoolean(5),
            Role:         NormalizeRole(reader.IsDBNull(6) ? null : reader.GetString(6))
        );
    }

    // ── Check if username already exists ──────────────────────────────────────

    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM auth.users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("@username", username);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    // ── Create a new user ──────────────────────────────────────────────────────

    /// <summary>
    /// Inserts a new user row and assigns the given role.
    /// Returns the new user's GUID.
    /// </summary>
    public async Task<Guid> CreateUserAsync(string username, string email, string passwordHash,
                                            string? fullName, string roleName)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync();

        var userId = Guid.NewGuid();

        // Insert into auth.users
        await using var insertUser = new SqlCommand(@"
            INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
            VALUES (@id, @username, @email, @passwordHash, @fullName, 1, GETUTCDATE(), GETUTCDATE())", conn);

        insertUser.Parameters.AddWithValue("@id",           userId);
        insertUser.Parameters.AddWithValue("@username",     username);
        insertUser.Parameters.AddWithValue("@email",        email);
        insertUser.Parameters.AddWithValue("@passwordHash", passwordHash);
        insertUser.Parameters.AddWithValue("@fullName",     (object?)fullName ?? DBNull.Value);

        await insertUser.ExecuteNonQueryAsync();

        var normalizedRole = NormalizeRole(roleName);
        var roleCandidates = GetRoleCandidates(normalizedRole);

        // Look up role id, preferring canonical role rows when they exist.
        await using var getRoleId = new SqlCommand(@"
            SELECT TOP 1 id
            FROM auth.roles
            WHERE UPPER(role_name) IN (@candidate0, @candidate1)
            ORDER BY
                CASE role_name
                    WHEN @preferredRole THEN 0
                    ELSE 1
                END,
                CASE UPPER(role_name)
                    WHEN @preferredUpper THEN 0
                    ELSE 1
                END", conn);
        getRoleId.Parameters.AddWithValue("@candidate0", roleCandidates[0]);
        getRoleId.Parameters.AddWithValue("@candidate1", roleCandidates[1]);
        getRoleId.Parameters.AddWithValue("@preferredRole", normalizedRole);
        getRoleId.Parameters.AddWithValue("@preferredUpper", normalizedRole.ToUpperInvariant());
        var roleId = await getRoleId.ExecuteScalarAsync();

        if (roleId != null)
        {
            await using var insertRole = new SqlCommand(@"
                INSERT INTO auth.user_roles (user_id, role_id)
                VALUES (@userId, @roleId)", conn);
            insertRole.Parameters.AddWithValue("@userId", userId);
            insertRole.Parameters.AddWithValue("@roleId", roleId);
            await insertRole.ExecuteNonQueryAsync();
        }

        return userId;
    }

    private static string NormalizeRole(string? roleName)
    {
        return roleName?.Trim().ToUpperInvariant() switch
        {
            "ADMIN" => "Admin",
            "MANAGER" => "Manager",
            "USER" => "Employee",
            "EMPLOYEE" => "Employee",
            null or "" => "Employee",
            _ => roleName!.Trim()
        };
    }

    private static string[] GetRoleCandidates(string normalizedRole)
    {
        return normalizedRole switch
        {
            "Admin" => ["ADMIN", "ADMIN"],
            "Manager" => ["MANAGER", "MANAGER"],
            _ => ["EMPLOYEE", "USER"]
        };
    }
}

// ── DB result model ───────────────────────────────────────────────────────────
public sealed record DbUser(
    Guid    Id,
    string  Username,
    string  Email,
    string  PasswordHash,
    string? FullName,
    bool    IsActive,
    string  Role
);
