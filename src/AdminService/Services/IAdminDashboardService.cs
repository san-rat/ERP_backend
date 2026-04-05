using AdminService.Models;

namespace AdminService.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
}
