using AdminService.Models;

namespace AdminService.Services;

public interface IAdminUserService
{
    Task<CreateStaffResponse> CreateStaffAsync(CreateStaffRequest request, string role, CancellationToken cancellationToken = default);
    Task<PagedResponse<StaffListItem>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<StaffListItem> UpdateUserAsync(Guid userId, UpdateStaffRequest request, CancellationToken cancellationToken = default);
    Task<StaffListItem> UpdateUserStatusAsync(Guid userId, UpdateStaffStatusRequest request, CancellationToken cancellationToken = default);
    Task<ResetPasswordResponse> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);
}
