using ForecastService.Models;

namespace ForecastService.Repositories
{
    public interface ISalesRepository
    {
        Task<ProductMetrics?> GetProductMetricsAsync(Guid productId);
        Task<List<SalesData>> GetProductSalesHistoryAsync(Guid productId, int days = 365);
        Task<List<ProductMetrics>> GetAllProductMetricsAsync();
    }
}