using Microsoft.AspNetCore.Mvc;
using ForecastService.Models;
using ForecastService.Services;

namespace ForecastService.Controllers
{
    [ApiController]
    [Route("api/forecasting/[controller]")]
    public class ForecastController : ControllerBase
    {
        private readonly ISalesForecasterService _forecastService;
        private readonly IProductDataService _productDataService;
        private readonly ILogger<ForecastController> _logger;

        public ForecastController(ISalesForecasterService forecastService, IProductDataService productDataService, ILogger<ForecastController> logger)
        {
            _forecastService = forecastService;
            _productDataService = productDataService;
            _logger = logger;
        }

        [HttpPost("product")]
        public async Task<IActionResult> ForecastProductSales([FromBody] ForecastRequest request)
        {
            try
            {
                _logger.LogInformation("Forecasting sales for product {ProductId}", request.ProductId);
                var result = await _forecastService.ForecastProductSalesAsync(request);
                if (result is null)
                    return NotFound(new { error = "Product not found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forecasting product sales");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> ForecastMultipleProducts([FromBody] List<ForecastRequest> requests)
        {
            try
            {
                _logger.LogInformation("Forecasting sales for {Count} products", requests.Count);
                var results = await _forecastService.ForecastMultipleProductsAsync(requests);
                return Ok(new { forecasts = results, count = results.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forecasting multiple products");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("product/{productId}/latest")]
        public async Task<IActionResult> GetLatestForecast(Guid productId)
        {
            try
            {
                var result = await _forecastService.GetLatestForecastAsync(productId);
                if (result is null)
                    return NotFound(new { error = "No forecast found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest forecast");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}