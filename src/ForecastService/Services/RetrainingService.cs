using ForecastService.Models;

namespace ForecastService.Services
{
    public class RetrainingService : IRetrainingService
    {
        private readonly ISalesForecasterService _forecastService;
        private readonly IProductDataService _productDataService;
        private readonly ILogger<RetrainingService> _logger;
        
        private bool _isRetrainingInProgress = false;
        private RetrainingResult? _currentRetrainingResult;
        private List<RetrainingHistory> _retrainingHistory = new();
        private Dictionary<Guid, ProductForecastCache> _forecastCache = new();
        
        private bool _autoRetrainingEnabled = true;
        private DayOfWeek _retrainingDay = DayOfWeek.Sunday; // Weekly retrain every Sunday
        private int _productsInCurrentRetrain = 0;
        private int _totalProductsInCurrentRetrain = 0;

        public RetrainingService(
            ISalesForecasterService forecastService,
            IProductDataService productDataService,
            ILogger<RetrainingService> logger)
        {
            _forecastService = forecastService;
            _productDataService = productDataService;
            _logger = logger;
        }

        public bool IsRetrainingInProgress => _isRetrainingInProgress;

        /// <summary>
        /// Manually trigger retraining for all or specific products
        /// </summary>
        public async Task<RetrainingResult> TriggerRetrainingAsync(string? triggerReason = null)
        {
            if (_isRetrainingInProgress)
            {
                _logger.LogWarning("⚠ Retraining already in progress, cannot start new session");
                return _currentRetrainingResult ?? new RetrainingResult
                {
                    Status = "IN_PROGRESS",
                    ErrorMessage = "Retraining already in progress"
                };
            }

            _isRetrainingInProgress = true;
            var retrainingId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("🔄 RETRAINING STARTED");
            _logger.LogInformation("📍 RetrainingId: {RetrainingId}", retrainingId);
            _logger.LogInformation("📍 Reason: {Reason}", triggerReason ?? "Manual Trigger");
            _logger.LogInformation("📍 Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}", startTime);

            try
            {
                var result = new RetrainingResult
                {
                    RetrainingId = retrainingId,
                    StartedAt = startTime,
                    TriggerReason = triggerReason ?? "Manual Trigger"
                };

                // Get all products
                var allMetrics = await _productDataService.GetAllProductMetricsAsync();
                if (allMetrics.Count == 0)
                {
                    _logger.LogWarning("⚠ No products found for retraining");
                    result.Status = "COMPLETED";
                    result.CompletedAt = DateTime.UtcNow;
                    result.DurationSeconds = result.CompletedAt.Value - result.StartedAt;
                    return result;
                }

                _totalProductsInCurrentRetrain = allMetrics.Count;
                result.TotalProductsRetrained = allMetrics.Count;

                _logger.LogInformation("📊 Found {Count} products for retraining", allMetrics.Count);

                // Retrain each product
                int successCount = 0;
                int failCount = 0;

                for (int i = 0; i < allMetrics.Count; i++)
                {
                    _productsInCurrentRetrain = i + 1;
                    var product = allMetrics[i];

                    try
                    {
                        _logger.LogInformation(
                            "  [{Current}/{Total}] Retraining {ProductName}...",
                            i + 1,
                            allMetrics.Count,
                            product.ProductName);

                        var success = await RetrainProductAsync(product.ProductId);

                        if (success)
                        {
                            successCount++;
                            _logger.LogInformation("  ✓ {ProductName} trained successfully", product.ProductName);
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning("  ✗ {ProductName} training failed", product.ProductName);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex, "  ❌ Exception training {ProductName}", product.ProductName);
                    }
                }

                var completionTime = DateTime.UtcNow;
                result.Status = "COMPLETED";
                result.CompletedAt = completionTime;
                result.SuccessfullyTrained = successCount;
                result.FailedCount = failCount;
                result.DurationSeconds = completionTime - startTime;

                _logger.LogInformation("════════════════════════════════════════════════════════");
                _logger.LogInformation("✅ RETRAINING COMPLETED");
                _logger.LogInformation("📊 Results: {Success}/{Total} successful, {Failed} failed",
                    successCount, allMetrics.Count, failCount);
                _logger.LogInformation("⏱️ Duration: {Duration:hh\\:mm\\:ss}", result.DurationSeconds);
                _logger.LogInformation("════════════════════════════════════════════════════════");

                // Store in history
                _retrainingHistory.Insert(0, new RetrainingHistory
                {
                    RetrainingId = retrainingId,
                    StartedAt = startTime,
                    CompletedAt = completionTime,
                    TotalProductsRetrained = allMetrics.Count,
                    SuccessfullyTrained = successCount,
                    FailedCount = failCount,
                    Status = "COMPLETED",
                    TriggerReason = triggerReason,
                    Duration = result.DurationSeconds
                });

                _currentRetrainingResult = result;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RETRAINING FAILED with exception");
                var errorResult = new RetrainingResult
                {
                    RetrainingId = retrainingId,
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow,
                    Status = "FAILED",
                    ErrorMessage = ex.Message,
                    DurationSeconds = DateTime.UtcNow - startTime
                };
                _currentRetrainingResult = errorResult;
                return errorResult;
            }
            finally
            {
                _isRetrainingInProgress = false;
                _productsInCurrentRetrain = 0;
                _totalProductsInCurrentRetrain = 0;
            }
        }

        /// <summary>
        /// Retrain a specific product (generate new forecast)
        /// </summary>
        public async Task<bool> RetrainProductAsync(Guid productId)
        {
            try
            {
                var request = new ForecastRequest
                {
                    ProductId = productId,
                    ForecastDays = 30,
                    Algorithm = "AUTO",
                    IncludeConfidenceInterval = true,
                    ConfidenceLevel = 95
                };

                var forecast = await _forecastService.ForecastProductSalesAsync(request);

                if (forecast is null)
                {
                    _logger.LogWarning("No forecast generated for product {ProductId}", productId);
                    return false;
                }

                // Cache the forecast
                _forecastCache[productId] = new ProductForecastCache
                {
                    ProductId = productId,
                    LastForecastId = forecast.ForecastId,
                    LastTrainedDate = DateTime.UtcNow,
                    Algorithm = forecast.Algorithm,
                    LastMAPE = forecast.MAPE,
                    LastRMSE = forecast.RMSE,
                    LastR2 = forecast.R_Squared,
                    IsValid = true
                };

                _logger.LogDebug(
                    "Product {ProductId} cached - MAPE: {MAPE}%, RMSE: {RMSE}, R²: {R2}",
                    productId, forecast.MAPE, forecast.RMSE, forecast.R_Squared);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retraining product {ProductId}", productId);
                return false;
            }
        }

        /// <summary>
        /// Get current retraining status
        /// </summary>
        public async Task<RetrainingStatus?> GetRetrainingStatusAsync()
        {
            var lastRetrain = _retrainingHistory.FirstOrDefault();
            var nextRetrain = CalculateNextRetrainingTime();

            return new RetrainingStatus
            {
                IsInProgress = _isRetrainingInProgress,
                CurrentRetrainingId = _currentRetrainingResult?.RetrainingId,
                ProductsProcessed = _productsInCurrentRetrain,
                TotalProducts = _totalProductsInCurrentRetrain,
                ProgressPercentage = _totalProductsInCurrentRetrain > 0
                    ? (_productsInCurrentRetrain / (decimal)_totalProductsInCurrentRetrain) * 100
                    : 0,
                LastRetrainingDate = lastRetrain?.CompletedAt,
                LastRetrainingStatus = lastRetrain?.Status,
                NextScheduledRetrain = nextRetrain,
                AutoRetrainingEnabled = _autoRetrainingEnabled,
                ScheduledDay = _retrainingDay
            };
        }

        /// <summary>
        /// Get retraining history
        /// </summary>
        public async Task<List<RetrainingHistory>> GetRetrainingHistoryAsync(int limit = 10)
        {
            return await Task.FromResult(_retrainingHistory.Take(limit).ToList());
        }

        /// <summary>
        /// Enable/disable automatic retraining
        /// </summary>
        public void SetAutoRetrainingEnabled(bool enabled)
        {
            _autoRetrainingEnabled = enabled;
            _logger.LogInformation("Auto retraining {Status}", enabled ? "ENABLED" : "DISABLED");
        }

        /// <summary>
        /// Set the day of week for automatic retraining
        /// </summary>
        public void SetAutoRetrainingDay(DayOfWeek day)
        {
            _retrainingDay = day;
            _logger.LogInformation("Automatic retraining scheduled for {Day}", day);
        }

        /// <summary>
        /// Calculate the next scheduled retraining time
        /// </summary>
        private DateTime CalculateNextRetrainingTime()
        {
            var now = DateTime.UtcNow;
            var nextRetrain = now.AddDays(1);

            // Find the next occurrence of the scheduled day
            while (nextRetrain.DayOfWeek != _retrainingDay)
            {
                nextRetrain = nextRetrain.AddDays(1);
            }

            // Set to a specific time (e.g., 2 AM UTC)
            nextRetrain = nextRetrain.Date.AddHours(2);

            return nextRetrain;
        }

        /// <summary>
        /// Check if it's time to retrain (used by background worker)
        /// </summary>
        public bool ShouldRetrain()
        {
            if (!_autoRetrainingEnabled)
                return false;

            var now = DateTime.UtcNow;
            var lastRetrain = _retrainingHistory.FirstOrDefault();

            // Retrain if we haven't retrained today AND today is the scheduled day
            if (now.DayOfWeek == _retrainingDay)
            {
                if (lastRetrain is null || lastRetrain.CompletedAt?.Date < now.Date)
                {
                    // Also check if we're past the scheduled hour (2 AM)
                    if (now.Hour >= 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}