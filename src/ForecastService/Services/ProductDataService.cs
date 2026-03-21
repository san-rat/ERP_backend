using ForecastService.Models;
using ForecastService.Repositories;

namespace ForecastService.Services
{
    public class ProductDataService : IProductDataService
    {
        private readonly ISalesRepository _repository;
        private readonly ITimeSeriesAnalyzer _analyzer;
        private readonly ILogger<ProductDataService> _logger;

        public ProductDataService(ISalesRepository repository, ITimeSeriesAnalyzer analyzer, ILogger<ProductDataService> logger)
        {
            _repository = repository;
            _analyzer = analyzer;
            _logger = logger;
        }

        public async Task<ProductMetrics?> GetProductMetricsAsync(Guid productId)
        {
            try
            {
                var metrics = await _repository.GetProductMetricsAsync(productId);
                if (metrics is not null)
                {
                    metrics.TrendDirection = await CalculateTrendAsync(productId);
                    metrics.Volatility = await CalculateVolatilityAsync(productId);
                    metrics.SeasonalityIndex = await CalculateSeasonalityAsync(productId);
                }
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product metrics");
                return null;
            }
        }

        public async Task<List<SalesData>> GetProductSalesHistoryAsync(Guid productId, int days = 365)
        {
            try
            {
                return await _repository.GetProductSalesHistoryAsync(productId, days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales history");
                return new List<SalesData>();
            }
        }

        public async Task<List<ProductMetrics>> GetAllProductMetricsAsync()
        {
            try
            {
                var products = await _repository.GetAllProductMetricsAsync();
                foreach (var product in products)
                {
                    product.TrendDirection = await CalculateTrendAsync(product.ProductId);
                    product.Volatility = await CalculateVolatilityAsync(product.ProductId);
                    product.SeasonalityIndex = await CalculateSeasonalityAsync(product.ProductId);
                }
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all product metrics");
                return new List<ProductMetrics>();
            }
        }

        public async Task<SalesAnalytics?> AnalyzeProductSalesAsync(Guid productId)
        {
            try
            {
                var metrics = await GetProductMetricsAsync(productId);
                if (metrics is null) return null;

                var history = await GetProductSalesHistoryAsync(productId, days: 365);
                if (history.Count == 0) return null;

                var units = history.Select(x => (double)x.UnitsSold).ToArray();
                var avgUnits = units.Average();
                var maxUnits = units.Max();
                var minUnits = units.Min();
                var stdDev = Math.Sqrt(units.Average(x => Math.Pow(x - avgUnits, 2)));

                var trend = CalculateTrend(history);
                var growthRate = CalculateGrowthRate(history);
                var peakDays = DetectPeakDays(history);
                var isSeasonality = metrics.SeasonalityIndex > 0.3m;

                return new SalesAnalytics
                {
                    ProductId = productId,
                    ProductName = metrics.ProductName,
                    AvgDailySales = (decimal)avgUnits,
                    MaxDailySales = (decimal)maxUnits,
                    MinDailySales = (decimal)minUnits,
                    StandardDeviation = (decimal)stdDev,
                    Trend = trend,
                    GrowthRate = growthRate,
                    SeasonalPattern = isSeasonality ? "SEASONAL" : "NON_SEASONAL",
                    PeakDays = peakDays,
                    DaysWithData = history.Count,
                    FirstSaleDate = history.Min(x => x.Date),
                    LastSaleDate = history.Max(x => x.Date),
                    TotalRevenue = (decimal)history.Sum(x => x.Revenue)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sales");
                return null;
            }
        }

        private async Task<decimal> CalculateTrendAsync(Guid productId)
        {
            try
            {
                var history = await GetProductSalesHistoryAsync(productId, days: 90);
                if (history.Count < 2) return 0;
                return CalculateTrend(history) == "UPWARD" ? 1 : (CalculateTrend(history) == "DOWNWARD" ? -1 : 0);
            }
            catch { return 0; }
        }

        private async Task<decimal> CalculateVolatilityAsync(Guid productId)
        {
            try
            {
                var history = await GetProductSalesHistoryAsync(productId, days: 90);
                if (history.Count < 2) return 0;

                var units = history.Select(x => (double)x.UnitsSold).ToArray();
                var avg = units.Average();
                var variance = units.Average(x => Math.Pow(x - avg, 2));
                return (decimal)Math.Sqrt(variance);
            }
            catch { return 0; }
        }

        private async Task<decimal> CalculateSeasonalityAsync(Guid productId)
        {
            try
            {
                var history = await GetProductSalesHistoryAsync(productId, days: 90);
                if (history.Count < 14) return 0;

                var units = history.Select(x => (decimal)x.UnitsSold).ToArray();
                return _analyzer.CalculateSeasonality(units, seasonLength: 7);
            }
            catch { return 0; }
        }

        private string CalculateTrend(List<SalesData> history)
        {
            if (history.Count < 7) return "STABLE";

            var firstWeek = history.Take(7).Select(x => x.UnitsSold).Average();
            var lastWeek = history.Skip(Math.Max(0, history.Count - 7)).Take(7).Select(x => x.UnitsSold).Average();

            var change = ((decimal)lastWeek - (decimal)firstWeek);
            var percentChange = ((decimal)firstWeek > 0 ? (change / (decimal)firstWeek) * 100 : 0);

            if (percentChange > 10) return "UPWARD";
            if (percentChange < -10) return "DOWNWARD";
            return "STABLE";
        }

        private decimal CalculateGrowthRate(List<SalesData> history)
        {
            if (history.Count < 30) return 0;

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var currentMonth = history.Where(x => x.Date >= thirtyDaysAgo).Select(x => x.UnitsSold).Sum();

            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
            var previousMonth = history.Where(x => x.Date >= sixtyDaysAgo && x.Date < thirtyDaysAgo).Select(x => x.UnitsSold).Sum();

            if (previousMonth == 0) return 0;
            return ((decimal)(currentMonth - previousMonth) / previousMonth) * 100;
        }

        private List<int> DetectPeakDays(List<SalesData> history)
        {
            var peakDays = new List<int>();

            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                // Cast both to decimal
var avgForDay = history
    .Where(x => x.Date.DayOfWeek == (DayOfWeek)dayOfWeek)
    .Select(x => (decimal)x.UnitsSold)  // ← Cast to decimal
    .DefaultIfEmpty(0)
    .Average();

var overallAvg = history.Select(x => (decimal)x.UnitsSold).Average();  // ← Cast to decimal

if (avgForDay > overallAvg * 1.2m)  // ← Now both decimal, works!
{
    peakDays.Add(dayOfWeek);
}
            }

            return peakDays;
        }
    }
}