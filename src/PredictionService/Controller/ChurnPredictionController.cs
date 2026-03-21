using Microsoft.AspNetCore.Mvc;
using PredictionService.Models;
using PredictionService.Services;
using PredictionService.Repositories;

namespace PredictionService.Controllers;

[ApiController]
[Route("api/ml/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly IChurnPredictionService _churnService;
    private readonly IChurnRepository _repository;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IChurnPredictionService churnService,
        IChurnRepository repository,
        ILogger<PredictionsController> logger)
    {
        _churnService = churnService;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Predict churn for a customer
    /// </summary>
    [HttpPost("churn")]
    public async Task<IActionResult> PredictChurn([FromBody] ChurnPredictionInput input)
    {
        try
        {
            _logger.LogInformation("Churn prediction request for customer {CustomerId}", input.CustomerId);
            
            var result = await _churnService.PredictChurnAsync(input.CustomerId);
            
            if (result == null)
                return NotFound(new { error = "Customer not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PredictChurn");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent predictions (last 7 days)
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentPredictions([FromQuery] int days = 7)
    {
        try
        {
            var predictions = await _repository.GetRecentPredictionsAsync(days);
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRecentPredictions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get analytics for a risk level
    /// </summary>
    [HttpGet("analytics/churn/{level}")]
    public async Task<IActionResult> GetChurnAnalytics(string level)
    {
        try
        {
            var (total, avg, max, min) = await _repository.GetAnalyticsByRiskLevelAsync(level);
            
            return Ok(new
            {
                RiskLevel = level,
                TotalCustomersAtRisk = total,
                AverageProbability = avg,
                MaxProbability = max,
                MinProbability = min,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetChurnAnalytics");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}