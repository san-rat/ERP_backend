using Microsoft.AspNetCore.Mvc;
using ForecastService.Services;

namespace ForecastService.Controllers
{
    [ApiController]
    [Route("api/forecasting/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IProductDataService _productDataService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IProductDataService productDataService, ILogger<AnalyticsController> logger)
        {
            _productDataService = productDataService;
            _logger = logger;
        }

        [HttpGet("product/{productId}/metrics")]
        public async Task<IActionResult> GetProductMetrics(Guid productId)
        {
            try
            {
                var metrics = await _productDataService.GetProductMetricsAsync(productId);
                if (metrics is null)
                    return NotFound(new { error = "Product not found" });
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product metrics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("product/{productId}/analysis")]
        public async Task<IActionResult> AnalyzeSalesHistory(Guid productId)
        {
            try
            {
                var analysis = await _productDataService.AnalyzeProductSalesAsync(productId);
                if (analysis is null)
                    return NotFound(new { error = "Product not found" });
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sales history");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("products/metrics")]
        public async Task<IActionResult> GetAllProductMetrics()
        {
            try
            {
                var metrics = await _productDataService.GetAllProductMetricsAsync();
                return Ok(new { products = metrics, count = metrics.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all product metrics");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}