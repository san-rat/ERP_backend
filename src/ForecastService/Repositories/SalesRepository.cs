using Microsoft.Data.SqlClient;
using ForecastService.Models;

namespace ForecastService.Repositories
{
    public class SalesRepository : ISalesRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SalesRepository> _logger;
        private const string ConnectionName = "insighterp_db";

        public SalesRepository(IConfiguration config, ILogger<SalesRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ProductMetrics?> GetProductMetricsAsync(Guid productId)
        {
            try
            {
                var connectionString = _config.GetConnectionString(ConnectionName);
                if (string.IsNullOrEmpty(connectionString)) return null;

                const string query = @"
                    SELECT
                        p.id,
                        p.sku,
                        p.name,
                        p.category_id,
                        p.price,
                        ISNULL(SUM(oi.quantity), 0)          AS total_units_sold,
                        ISNULL(SUM(oi.total_price), 0)       AS total_revenue,
                        ISNULL(AVG(oi.unit_price), p.price)  AS avg_unit_price,
                        COUNT(DISTINCT oi.order_id)          AS order_count
                    FROM dbo.products p
                    LEFT JOIN dbo.order_items oi ON p.id = oi.product_id
                    WHERE p.id = @productId
                    GROUP BY p.id, p.sku, p.name, p.category_id, p.price";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@productId", productId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new ProductMetrics
                    {
                        ProductId      = reader.GetGuid(0),
                        SKU            = reader.GetString(1),
                        ProductName    = reader.GetString(2),
                        CategoryId     = reader.GetInt32(3),
                        CurrentPrice   = reader.GetDecimal(4),
                        TotalUnitsSold = reader.GetInt32(5),
                        TotalRevenue   = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                        AvgUnitPrice   = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        OrderCount     = reader.GetInt32(8)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product metrics for {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// Returns ONE ROW PER DAY — this is what the forecasting algorithm needs.
        /// Groups by date so multiple orders on same day are combined.
        /// </summary>
        public async Task<List<SalesData>> GetProductSalesHistoryAsync(Guid productId, int days = 365)
        {
            try
            {
                var connectionString = _config.GetConnectionString(ConnectionName) ?? string.Empty;

                const string query = @"
                    SELECT
                        NEWID()                         AS id,
                        p.id                            AS product_id,
                        CAST(o.created_at AS DATE)      AS sale_date,
                        SUM(oi.quantity)                AS units_sold,
                        SUM(oi.total_price)             AS revenue,
                        AVG(oi.unit_price)              AS avg_price,
                        COUNT(DISTINCT o.id)            AS order_count
                    FROM dbo.order_items oi
                    JOIN dbo.products p ON oi.product_id = p.id
                    JOIN dbo.orders   o ON oi.order_id   = o.id
                    WHERE p.id     = @productId
                      AND o.status = 'DELIVERED'
                      AND o.created_at >= DATEADD(DAY, -@days, GETUTCDATE())
                    GROUP BY p.id, CAST(o.created_at AS DATE)
                    ORDER BY sale_date ASC";

                var salesData = new List<SalesData>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@productId", productId);
                command.Parameters.AddWithValue("@days", days);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    salesData.Add(new SalesData
                    {
                        Id           = reader.GetGuid(0),
                        ProductId    = reader.GetGuid(1),
                        Date         = reader.GetDateTime(2),
                        UnitsSold    = reader.GetInt32(3),
                        Revenue      = reader.GetDecimal(4),
                        AveragePrice = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        OrderCount   = reader.GetInt32(6)
                    });
                }

                _logger.LogInformation("Fetched {Count} daily records for product {ProductId}",
                    salesData.Count, productId);
                return salesData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales history for {ProductId}", productId);
                return new List<SalesData>();
            }
        }

        public async Task<List<ProductMetrics>> GetAllProductMetricsAsync()
        {
            try
            {
                var connectionString = _config.GetConnectionString(ConnectionName) ?? string.Empty;

                const string query = @"
                    SELECT
                        p.id,
                        p.sku,
                        p.name,
                        p.category_id,
                        p.price,
                        ISNULL(SUM(oi.quantity), 0)         AS total_units_sold,
                        ISNULL(SUM(oi.total_price), 0)      AS total_revenue,
                        ISNULL(AVG(oi.unit_price), p.price) AS avg_unit_price,
                        COUNT(DISTINCT oi.order_id)         AS order_count
                    FROM dbo.products p
                    LEFT JOIN dbo.order_items oi ON p.id = oi.product_id
                    GROUP BY p.id, p.sku, p.name, p.category_id, p.price
                    ORDER BY total_revenue DESC";

                var products = new List<ProductMetrics>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                using var reader  = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(new ProductMetrics
                    {
                        ProductId      = reader.GetGuid(0),
                        SKU            = reader.GetString(1),
                        ProductName    = reader.GetString(2),
                        CategoryId     = reader.GetInt32(3),
                        CurrentPrice   = reader.GetDecimal(4),
                        TotalUnitsSold = reader.GetInt32(5),
                        TotalRevenue   = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                        AvgUnitPrice   = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        OrderCount     = reader.GetInt32(8)
                    });
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all product metrics");
                return new List<ProductMetrics>();
            }
        }
    }
}