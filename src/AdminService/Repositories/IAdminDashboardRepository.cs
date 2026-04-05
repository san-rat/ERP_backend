using AdminService.Models;

namespace AdminService.Repositories;

public interface IAdminDashboardRepository
{
    Task<AdminDashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
}
