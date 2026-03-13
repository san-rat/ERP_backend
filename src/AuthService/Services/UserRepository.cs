using Microsoft.Data.SqlClient;

namespace AuthService.Services;

/// <summary>
/// Handles all database operations for the auth.users and auth.roles tables.
/// </summary>
public sealed class UserRepository
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
            SELECT u.id, u.username, u.email, u.password_hash, u.full_name, u.is_active, r.role_name
            FROM auth.users u
            LEFT JOIN auth.user_roles ur ON u.id = ur.user_id
            LEFT JOIN auth.roles r ON ur.role_id = r.id
            WHERE u.username = @username", conn);

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
            Role:         reader.IsDBNull(6) ? "Employee" : reader.GetString(6)
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

        // Look up role id
        await using var getRoleId = new SqlCommand(
            "SELECT id FROM auth.roles WHERE role_name = @role", conn);
        getRoleId.Parameters.AddWithValue("@role", roleName);
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
