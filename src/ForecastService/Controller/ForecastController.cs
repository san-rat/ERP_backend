using Microsoft.AspNetCore.Mvc;
using ForecastService.Models;
using ForecastService.Services;

namespace ForecastService.Controllers
{
    [ApiController]
    [Route("api/forecasting")]
    public class ForecastController : ControllerBase
    {
        private readonly ISalesForecasterService _forecastService;
        private readonly IProductDataService _productDataService;
        private readonly ILogger<ForecastController> _logger;

        public ForecastController(
            ISalesForecasterService forecastService,
            IProductDataService productDataService,
            ILogger<ForecastController> logger)
        {
            _forecastService = forecastService;
            _productDataService = productDataService;
            _logger = logger;
        }

        /// <summary>
        /// Returns a 7-day and 30-day sales forecast for every product.
        /// No parameters or request body required.
        /// GET /api/forecasting/products
        /// </summary>
        [HttpGet("products")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProductForecasts()
        {
            try
            {
                var allProducts = await _productDataService.GetAllProductMetricsAsync();

                if (allProducts.Count == 0)
                    return Ok(new
                    {
                        totalProducts = 0,
                        generatedAt   = DateTime.UtcNow,
                        results       = Array.Empty<object>()
                    });

                _logger.LogInformation("Generating forecasts for {Count} product(s)", allProducts.Count);

                var results      = new List<object>(allProducts.Count);
                int successCount = 0;
                int failedCount  = 0;

                foreach (var product in allProducts)
                {
                    try
                    {
                        var task7 = _forecastService.ForecastProductSalesAsync(new ForecastRequest
                        {
                            ProductId                 = product.ProductId,
                            ForecastDays              = 7,
                            Algorithm                 = "AUTO",
                            IncludeConfidenceInterval = true,
                            ConfidenceLevel           = 95
                        });

                        var task30 = _forecastService.ForecastProductSalesAsync(new ForecastRequest
                        {
                            ProductId                 = product.ProductId,
                            ForecastDays              = 30,
                            Algorithm                 = "AUTO",
                            IncludeConfidenceInterval = true,
                            ConfidenceLevel           = 95
                        });

                        await Task.WhenAll(task7, task30);

                        var forecast7  = task7.Result;
                        var forecast30 = task30.Result;

                        results.Add(new
                        {
                            productId   = product.ProductId,
                            productName = product.ProductName,
                            algorithm   = forecast30?.Algorithm ?? forecast7?.Algorithm ?? "UNKNOWN",
                            accuracy    = forecast30 is null ? null : new
                            {
                                mape     = Math.Round(forecast30.MAPE,      2),
                                rmse     = Math.Round(forecast30.RMSE,      2),
                                rSquared = Math.Round(forecast30.R_Squared, 4)
                            },
                            next7Days  = MapForecasts(forecast7),
                            next30Days = MapForecasts(forecast30)
                        });

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Forecast failed for product {ProductId}", product.ProductId);

                        results.Add(new
                        {
                            productId   = product.ProductId,
                            productName = product.ProductName,
                            error       = $"Forecast failed: {ex.Message}"
                        });

                        failedCount++;
                    }
                }

                return Ok(new
                {
                    totalProducts = allProducts.Count,
                    successCount,
                    failedCount,
                    generatedAt = DateTime.UtcNow,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while generating forecasts for all products");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns the most recent saved forecast for a single product.
        /// GET /api/forecasting/products/{productId}/latest
        /// </summary>
        [HttpGet("products/{productId:guid}/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLatestForecast(Guid productId)
        {
            try
            {
                var result = await _forecastService.GetLatestForecastAsync(productId);

                if (result is null)
                    return NotFound(new { error = $"No forecast found for product {productId}" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest forecast for product {ProductId}", productId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private static List<object>? MapForecasts(ForecastResult? result) =>
            result?.Forecasts.Select(d => (object)new
            {
                date              = d.Date.ToString("yyyy-MM-dd"),
                forecastedUnits   = Math.Round(d.ForecastedUnits,  0),
                forecastedRevenue = Math.Round(d.ForecastedRevenue, 2),
                minUnits          = d.Confidence != null ? Math.Round(d.Confidence.LowerBound, 0) : (decimal?)null,
                maxUnits          = d.Confidence != null ? Math.Round(d.Confidence.UpperBound, 0) : (decimal?)null
            }).ToList();
    }
}