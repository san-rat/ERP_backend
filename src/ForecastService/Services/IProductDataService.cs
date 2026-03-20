using ForecastService.Models;

namespace ForecastService.Services
{
    public interface IProductDataService
    {
        Task<ProductMetrics?> GetProductMetricsAsync(Guid productId);
        Task<List<SalesData>> GetProductSalesHistoryAsync(Guid productId, int days = 365);
        Task<List<ProductMetrics>> GetAllProductMetricsAsync();
        Task<SalesAnalytics?> AnalyzeProductSalesAsync(Guid productId);
    }
}