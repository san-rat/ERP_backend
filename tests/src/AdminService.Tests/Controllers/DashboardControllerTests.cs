using AdminService.Controllers;
using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AdminService.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetOverview_ReturnsOkWithOverviewResponse()
    {
        var service = new Mock<IAdminDashboardService>();
        service.Setup(x => x.GetOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminDashboardOverviewResponse
            {
                Staff = new StaffSummaryResponse
                {
                    TotalUsers = 10,
                    ActiveUsers = 8,
                    InactiveUsers = 2,
                    AdminUsers = 1,
                    ManagerUsers = 3,
                    EmployeeUsers = 6
                },
                Business = new BusinessSummaryResponse
                {
                    Customers = 5,
                    Products = 7,
                    TotalOrders = 12,
                    DeliveredOrders = 9,
                    CancelledOrders = 1,
                    Returns = 2,
                    GrossRevenue = 1000m,
                    RefundedTotal = 50m
                }
            });

        var controller = new DashboardController(service.Object);

        var result = await controller.GetOverview();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AdminDashboardOverviewResponse>(ok.Value);
        Assert.Equal(10, body.Staff.TotalUsers);
        Assert.Equal(1000m, body.Business.GrossRevenue);
    }
}
