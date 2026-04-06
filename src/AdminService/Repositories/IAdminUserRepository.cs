using AdminService.Models;

namespace AdminService.Repositories;

public interface IAdminUserRepository
{
    Task EnsureCanonicalRolesAsync(CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<StaffListItem>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<Guid> CreateUserAsync(string username, string email, string passwordHash, string? fullName, string role, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(Guid userId, string username, string email, string? fullName, string role, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
}
