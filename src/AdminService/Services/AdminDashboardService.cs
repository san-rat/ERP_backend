using AdminService.Models;
using AdminService.Repositories;

namespace AdminService.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IAdminDashboardRepository _dashboardRepository;

    public AdminDashboardService(IAdminDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public Task<AdminDashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
        => _dashboardRepository.GetOverviewAsync(cancellationToken);
}
