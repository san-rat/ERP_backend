using Microsoft.AspNetCore.Mvc;
using PredictionService.Models;
using PredictionService.Services;
using PredictionService.Repositories;

namespace PredictionService.Controllers;

[ApiController]
[Route("api/ml")]
public class PredictionsController : ControllerBase
{
    private readonly IChurnPredictionService _churnService;
    private readonly IChurnRepository _repository;
    private readonly ILogger<PredictionsController> _logger;
    private readonly IConfiguration _configuration;

    public PredictionsController(
        IChurnPredictionService churnService,
        IChurnRepository repository,
        ILogger<PredictionsController> logger,
        IConfiguration configuration)
    {
        _churnService = churnService;
        _repository = repository;
        _logger = logger;
        _configuration = configuration;
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
    /// Run churn predictions for ALL customers in the database
    /// </summary>
    [HttpPost("churn/predict-all")]
    public async Task<IActionResult> PredictAllCustomers()
    {
        try
        {
            _logger.LogInformation("Running churn predictions for ALL customers");

            var connectionString = _configuration.GetConnectionString("ChurnDb");
            var customerIds = new List<Guid>();

            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT id FROM dbo.customers ORDER BY created_at DESC", connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                customerIds.Add(reader.GetGuid(0));

            _logger.LogInformation("Found {Count} customers to predict", customerIds.Count);

            var results = new List<object>();
            int success = 0, failed = 0;

            foreach (var customerId in customerIds)
            {
                var result = await _churnService.PredictChurnAsync(customerId);
                if (result != null)
                {
                    results.Add(new
                    {
                        result.CustomerId,
                        result.ChurnProbability,
                        result.ChurnRiskLabel,
                        result.ModelVersion,
                        result.PredictedAt,
                        TopFactors = result.TopFactors.Take(3).Select(f => new
                        {
                            f.FactorName,
                            f.FeatureValue,
                            f.Weight
                        })
                    });
                    success++;
                }
                else
                {
                    results.Add(new
                    {
                        CustomerId = customerId,
                        Error = "Prediction failed or customer not found"
                    });
                    failed++;
                }
            }

            return Ok(new
            {
                TotalCustomers = customerIds.Count,
                SuccessCount = success,
                FailedCount = failed,
                GeneratedAt = DateTime.UtcNow,
                Results = results.OrderByDescending(r =>
                {
                    var prop = r.GetType().GetProperty("ChurnProbability");
                    return prop != null ? (decimal)(prop.GetValue(r) ?? 0m) : 0m;
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting all customers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent predictions (last 7 days)
    /// </summary>
    [HttpGet("churn/recent")]
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
    [HttpGet("churn/analytics/churn/{level}")]
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