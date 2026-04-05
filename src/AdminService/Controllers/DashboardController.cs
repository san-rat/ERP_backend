using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class DashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public DashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(AdminDashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetOverviewAsync(cancellationToken);
        return Ok(response);
    }
}
