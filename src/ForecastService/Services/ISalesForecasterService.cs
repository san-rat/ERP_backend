using ForecastService.Models;

namespace ForecastService.Services
{
    public interface ISalesForecasterService
    {
        Task<ForecastResult?> ForecastProductSalesAsync(ForecastRequest request);
        Task<List<ForecastResult>> ForecastMultipleProductsAsync(List<ForecastRequest> requests);
        Task<ForecastResult?> GetLatestForecastAsync(Guid productId);
        Task<bool> SaveForecastAsync(ForecastResult forecast);
    }
}