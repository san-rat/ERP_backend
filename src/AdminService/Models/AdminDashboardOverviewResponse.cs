namespace AdminService.Models;

public sealed class AdminDashboardOverviewResponse
{
    public required StaffSummaryResponse Staff { get; init; }
    public required BusinessSummaryResponse Business { get; init; }
}

public sealed class StaffSummaryResponse
{
    public required int TotalUsers { get; init; }
    public required int ActiveUsers { get; init; }
    public required int InactiveUsers { get; init; }
    public required int AdminUsers { get; init; }
    public required int ManagerUsers { get; init; }
    public required int EmployeeUsers { get; init; }
}

public sealed class BusinessSummaryResponse
{
    public required int Customers { get; init; }
    public required int Products { get; init; }
    public required int TotalOrders { get; init; }
    public required int DeliveredOrders { get; init; }
    public required int CancelledOrders { get; init; }
    public required int Returns { get; init; }
    public required decimal GrossRevenue { get; init; }
    public required decimal RefundedTotal { get; init; }
}
