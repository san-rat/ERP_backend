using Microsoft.Data.SqlClient;
using ForecastService.Models;

namespace ForecastService.Repositories
{
    public class SalesRepository : ISalesRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SalesRepository> _logger;
        private const string ConnectionName = "insighterp_db";  // ← CHANGED FROM "ChurnDb"

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
                        p.id, p.sku, p.name, p.category_id, p.price,
                        COUNT(DISTINCT oi.order_id) AS order_count,
                        COUNT(oi.id) AS total_units,
                        SUM(oi.total_price) AS total_revenue,
                        AVG(oi.unit_price) AS avg_unit_price
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
                        ProductId = reader.GetGuid(0),
                        SKU = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        CategoryId = reader.GetInt32(3),
                        CurrentPrice = reader.GetDecimal(4),
                        OrderCount = reader.GetInt32(5),
                        TotalUnitsSold = reader.GetInt32(6),
                        TotalRevenue = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        AvgUnitPrice = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product metrics");
                return null;
            }
        }

        public async Task<List<SalesData>> GetProductSalesHistoryAsync(Guid productId, int days = 365)
        {
            try
            {
                var connectionString = _config.GetConnectionString(ConnectionName) ?? string.Empty;
                
                const string query = @"
                    SELECT 
                        oi.id, p.id, o.created_at,
                        SUM(oi.quantity) AS units_sold,
                        SUM(oi.total_price) AS revenue,
                        AVG(oi.unit_price) AS avg_price,
                        1 AS order_count
                    FROM dbo.order_items oi
                    JOIN dbo.products p ON oi.product_id = p.id
                    JOIN dbo.orders o ON oi.order_id = o.id
                    WHERE p.id = @productId
                      AND o.created_at >= DATEADD(DAY, -@days, GETUTCDATE())
                    GROUP BY oi.id, p.id, o.created_at
                    ORDER BY o.created_at ASC";

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
                        Id = reader.GetGuid(0),
                        ProductId = reader.GetGuid(1),
                        Date = reader.GetDateTime(2),
                        UnitsSold = reader.GetInt32(3),
                        Revenue = reader.GetDecimal(4),
                        AveragePrice = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                        OrderCount = reader.GetInt32(6)
                    });
                }

                return salesData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales history");
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
                        p.id, p.sku, p.name, p.category_id, p.price,
                        COUNT(DISTINCT oi.order_id),
                        COUNT(oi.id),
                        SUM(oi.total_price),
                        AVG(oi.unit_price)
                    FROM dbo.products p
                    LEFT JOIN dbo.order_items oi ON p.id = oi.product_id
                    GROUP BY p.id, p.sku, p.name, p.category_id, p.price";

                var products = new List<ProductMetrics>();

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    products.Add(new ProductMetrics
                    {
                        ProductId = reader.GetGuid(0),
                        SKU = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        CategoryId = reader.GetInt32(3),
                        CurrentPrice = reader.GetDecimal(4),
                        OrderCount = reader.GetInt32(5),
                        TotalUnitsSold = reader.GetInt32(6),
                        TotalRevenue = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        AvgUnitPrice = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8)
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