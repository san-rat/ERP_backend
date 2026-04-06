using AdminService.Models;
using Microsoft.Data.SqlClient;

namespace AdminService.Repositories;

public sealed class AdminDashboardRepository : IAdminDashboardRepository
{
    private readonly IConfiguration _configuration;

    public AdminDashboardRepository(IConfiguration configuration)
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

    public async Task<AdminDashboardOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS total_users,
                COALESCE(SUM(CASE WHEN u.is_active = 1 THEN 1 ELSE 0 END), 0) AS active_users,
                COALESCE(SUM(CASE WHEN u.is_active = 0 THEN 1 ELSE 0 END), 0) AS inactive_users,
                COALESCE(SUM(CASE WHEN UPPER(ISNULL(rolepick.role_name, '')) = 'ADMIN' THEN 1 ELSE 0 END), 0) AS admin_users,
                COALESCE(SUM(CASE WHEN UPPER(ISNULL(rolepick.role_name, '')) = 'MANAGER' THEN 1 ELSE 0 END), 0) AS manager_users,
                COALESCE(SUM(CASE WHEN rolepick.role_name IS NULL OR UPPER(rolepick.role_name) IN ('USER', 'EMPLOYEE') THEN 1 ELSE 0 END), 0) AS employee_users
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
            ) rolepick;

            SELECT
                (SELECT COUNT(*) FROM dbo.customers) AS customers,
                (SELECT COUNT(*) FROM dbo.products) AS products,
                (SELECT COUNT(*) FROM dbo.orders) AS total_orders,
                (SELECT COUNT(*) FROM dbo.orders WHERE status = 'DELIVERED') AS delivered_orders,
                (SELECT COUNT(*) FROM dbo.orders WHERE status = 'CANCELLED') AS cancelled_orders,
                (SELECT COUNT(*) FROM dbo.returns) AS returns_count,
                (SELECT COALESCE(SUM(total_amount), 0) FROM dbo.orders WHERE status = 'DELIVERED') AS gross_revenue,
                (SELECT COALESCE(SUM(refund_amount), 0) FROM dbo.returns WHERE status IN ('APPROVED', 'COMPLETED')) AS refunded_total;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var staff = new StaffSummaryResponse
        {
            TotalUsers = reader.GetInt32(0),
            ActiveUsers = reader.GetInt32(1),
            InactiveUsers = reader.GetInt32(2),
            AdminUsers = reader.GetInt32(3),
            ManagerUsers = reader.GetInt32(4),
            EmployeeUsers = reader.GetInt32(5)
        };

        await reader.NextResultAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var business = new BusinessSummaryResponse
        {
            Customers = reader.GetInt32(0),
            Products = reader.GetInt32(1),
            TotalOrders = reader.GetInt32(2),
            DeliveredOrders = reader.GetInt32(3),
            CancelledOrders = reader.GetInt32(4),
            Returns = reader.GetInt32(5),
            GrossRevenue = reader.GetDecimal(6),
            RefundedTotal = reader.GetDecimal(7)
        };

        return new AdminDashboardOverviewResponse
        {
            Staff = staff,
            Business = business
        };
    }
}
