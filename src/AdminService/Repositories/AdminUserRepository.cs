using AdminService.Models;
using AdminService.Services;
using Microsoft.Data.SqlClient;

namespace AdminService.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly IConfiguration _configuration;

    public AdminUserRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("AuthDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:AuthDb is not configured.");
        }

        return new SqlConnection(connectionString);
    }

    public async Task EnsureCanonicalRolesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'Admin')
                INSERT INTO auth.roles (role_name) VALUES ('Admin');

            IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'Manager')
                INSERT INTO auth.roles (role_name) VALUES ('Manager');

            IF NOT EXISTS (SELECT 1 FROM auth.roles WHERE role_name = 'Employee')
                INSERT INTO auth.roles (role_name) VALUES ('Employee');
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => await ExistsAsync("username", username, excludeUserId, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => await ExistsAsync("email", email, excludeUserId, cancellationToken);

    public async Task<AdminUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sql = BuildUserSelectSql("""
            WHERE u.id = @userId
            """);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapAdminUserRecord(reader);
    }

    public async Task<PagedResponse<StaffListItem>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var safePageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var safePageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

        var parameters = new List<SqlParameter>();
        var whereClause = BuildWhereClause(query, parameters);
        var orderByClause = BuildOrderByClause(query.SortBy, query.SortOrder);
        var offset = (safePageNumber - 1) * safePageSize;

        var countSql = $"""
            SELECT COUNT(*)
            FROM auth.users u
            OUTER APPLY (
                SELECT TOP 1 r.role_name
                FROM auth.user_roles ur
                INNER JOIN auth.roles r ON ur.role_id = r.id
                WHERE ur.user_id = u.id
                ORDER BY CASE UPPER(r.role_name)
                    WHEN 'ADMIN' THEN 0
                    WHEN 'MANAGER' THEN 1
                    WHEN 'EMPLOYEE' THEN 2
                    WHEN 'USER' THEN 2
                    ELSE 3
                END, r.id
            ) rolepick
            {whereClause}
            """;

        var dataSql = $"""
            {BuildUserSelectSql(whereClause)}
            ORDER BY {orderByClause}
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var totalCount = await ExecuteCountAsync(connection, countSql, parameters, cancellationToken);

        await using var command = new SqlCommand(dataSql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.Value));
        }

        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@pageSize", safePageSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<StaffListItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapStaffListItem(reader));
        }

        return new PagedResponse<StaffListItem>
        {
            Items = items,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize)
        };
    }

    public async Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string? fullName, string role, CancellationToken cancellationToken = default)
    {
        await EnsureCanonicalRolesAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var userId = Guid.NewGuid();

            await using var insertUser = new SqlCommand("""
                INSERT INTO auth.users (id, username, email, password_hash, full_name, is_active, created_at, updated_at)
                VALUES (@id, @username, @email, @passwordHash, @fullName, 1, GETUTCDATE(), GETUTCDATE())
                """, connection, transaction);

            insertUser.Parameters.AddWithValue("@id", userId);
            insertUser.Parameters.AddWithValue("@username", username);
            insertUser.Parameters.AddWithValue("@email", email);
            insertUser.Parameters.AddWithValue("@passwordHash", passwordHash);
            insertUser.Parameters.AddWithValue("@fullName", (object?)fullName ?? DBNull.Value);

            await insertUser.ExecuteNonQueryAsync(cancellationToken);

            var roleId = await GetCanonicalRoleIdAsync(connection, transaction, role, cancellationToken);

            await using var insertRole = new SqlCommand("""
                INSERT INTO auth.user_roles (user_id, role_id)
                VALUES (@userId, @roleId)
                """, connection, transaction);

            insertRole.Parameters.AddWithValue("@userId", userId);
            insertRole.Parameters.AddWithValue("@roleId", roleId);
            await insertRole.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return userId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid userId, string username, string email, string? fullName, string role, CancellationToken cancellationToken = default)
    {
        await EnsureCanonicalRolesAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var updateUser = new SqlCommand("""
                UPDATE auth.users
                SET username = @username,
                    email = @email,
                    full_name = @fullName,
                    updated_at = GETUTCDATE()
                WHERE id = @userId
                """, connection, transaction);

            updateUser.Parameters.AddWithValue("@userId", userId);
            updateUser.Parameters.AddWithValue("@username", username);
            updateUser.Parameters.AddWithValue("@email", email);
            updateUser.Parameters.AddWithValue("@fullName", (object?)fullName ?? DBNull.Value);

            var rows = await updateUser.ExecuteNonQueryAsync(cancellationToken);
            if (rows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var roleId = await GetCanonicalRoleIdAsync(connection, transaction, role, cancellationToken);

            await using var deleteRoles = new SqlCommand("""
                DELETE FROM auth.user_roles
                WHERE user_id = @userId
                """, connection, transaction);
            deleteRoles.Parameters.AddWithValue("@userId", userId);
            await deleteRoles.ExecuteNonQueryAsync(cancellationToken);

            await using var insertRole = new SqlCommand("""
                INSERT INTO auth.user_roles (user_id, role_id)
                VALUES (@userId, @roleId)
                """, connection, transaction);
            insertRole.Parameters.AddWithValue("@userId", userId);
            insertRole.Parameters.AddWithValue("@roleId", roleId);
            await insertRole.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            UPDATE auth.users
            SET is_active = @isActive,
                updated_at = GETUTCDATE()
            WHERE id = @userId
            """, connection);

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@isActive", isActive);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            UPDATE auth.users
            SET password_hash = @passwordHash,
                updated_at = GETUTCDATE()
            WHERE id = @userId
            """, connection);

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private async Task<bool> ExistsAsync(string columnName, string value, Guid? excludeUserId, CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT(1) FROM auth.users WHERE {columnName} = @value";
        if (excludeUserId.HasValue)
        {
            sql += " AND id <> @excludeUserId";
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@value", value);
        if (excludeUserId.HasValue)
        {
            command.Parameters.AddWithValue("@excludeUserId", excludeUserId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static string BuildUserSelectSql(string whereClause)
    {
        return $"""
            SELECT
                u.id,
                u.username,
                u.email,
                u.full_name,
                CASE
                    WHEN rolepick.role_name IS NULL THEN 'Employee'
                    WHEN UPPER(rolepick.role_name) = 'ADMIN' THEN 'Admin'
                    WHEN UPPER(rolepick.role_name) = 'MANAGER' THEN 'Manager'
                    WHEN UPPER(rolepick.role_name) IN ('USER', 'EMPLOYEE') THEN 'Employee'
                    ELSE rolepick.role_name
                END AS normalized_role,
                u.is_active,
                u.created_at,
                u.updated_at
            FROM auth.users u
            OUTER APPLY (
                SELECT TOP 1 r.role_name
                FROM auth.user_roles ur
                INNER JOIN auth.roles r ON ur.role_id = r.id
                WHERE ur.user_id = u.id
                ORDER BY CASE UPPER(r.role_name)
                    WHEN 'ADMIN' THEN 0
                    WHEN 'MANAGER' THEN 1
                    WHEN 'EMPLOYEE' THEN 2
                    WHEN 'USER' THEN 2
                    ELSE 3
                END, r.id
            ) rolepick
            {whereClause}
            """;
    }

    private static string BuildWhereClause(UserListQuery query, List<SqlParameter> parameters)
    {
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            clauses.Add("(u.username LIKE @search OR u.email LIKE @search OR ISNULL(u.full_name, '') LIKE @search)");
            parameters.Add(new SqlParameter("@search", $"%{query.Search.Trim()}%"));
        }

        if (query.IsActive.HasValue)
        {
            clauses.Add("u.is_active = @isActive");
            parameters.Add(new SqlParameter("@isActive", query.IsActive.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = RoleNormalizer.Normalize(query.Role);
            switch (normalizedRole)
            {
                case RoleNormalizer.Admin:
                    clauses.Add("UPPER(ISNULL(rolepick.role_name, '')) = 'ADMIN'");
                    break;
                case RoleNormalizer.Manager:
                    clauses.Add("UPPER(ISNULL(rolepick.role_name, '')) = 'MANAGER'");
                    break;
                case RoleNormalizer.Employee:
                    clauses.Add("(rolepick.role_name IS NULL OR UPPER(rolepick.role_name) IN ('USER', 'EMPLOYEE'))");
                    break;
            }
        }

        return clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";
    }

    private static string BuildOrderByClause(string? sortBy, string? sortOrder)
    {
        var direction = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        var column = sortBy?.Trim().ToLowerInvariant() switch
        {
            "username" => "u.username",
            "email" => "u.email",
            "fullname" => "u.full_name",
            "role" => "UPPER(ISNULL(rolepick.role_name, 'EMPLOYEE'))",
            "isactive" => "u.is_active",
            "updatedat" => "u.updated_at",
            _ => "u.created_at"
        };

        return $"{column} {direction}, u.username ASC";
    }

    private static async Task<int> ExecuteCountAsync(SqlConnection connection, string sql, IEnumerable<SqlParameter> parameters, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.Value));
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static AdminUserRecord MapAdminUserRecord(SqlDataReader reader)
    {
        return new AdminUserRecord
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            FullName = reader.IsDBNull(3) ? null : reader.GetString(3),
            Role = reader.GetString(4),
            IsActive = reader.GetBoolean(5),
            CreatedAt = reader.GetDateTime(6),
            UpdatedAt = reader.GetDateTime(7)
        };
    }

    private static StaffListItem MapStaffListItem(SqlDataReader reader)
    {
        return new StaffListItem
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2),
            FullName = reader.IsDBNull(3) ? null : reader.GetString(3),
            Role = reader.GetString(4),
            IsActive = reader.GetBoolean(5),
            CreatedAt = reader.GetDateTime(6),
            UpdatedAt = reader.GetDateTime(7)
        };
    }

    private static async Task<int> GetCanonicalRoleIdAsync(SqlConnection connection, SqlTransaction transaction, string role, CancellationToken cancellationToken)
    {
        var canonicalRole = RoleNormalizer.ToCanonicalRole(role);

        await using var roleCommand = new SqlCommand("""
            SELECT TOP 1 id
            FROM auth.roles
            WHERE role_name = @role
            """, connection, transaction);

        roleCommand.Parameters.AddWithValue("@role", canonicalRole);
        var result = await roleCommand.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException($"Canonical role '{canonicalRole}' was not found.");
        }

        return Convert.ToInt32(result);
    }
}
