using Microsoft.AspNetCore.Mvc;
using ForecastService.Services;

namespace ForecastService.Controllers;

[ApiController]
[Route("api/forecasting/ml")]
public class ModelManagementController : ControllerBase
{
    private readonly IRetrainingService _retrainingService;
    private readonly ILogger<ModelManagementController> _logger;

    public ModelManagementController(
        IRetrainingService retrainingService,
        ILogger<ModelManagementController> logger)
    {
        _retrainingService = retrainingService;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger model retraining for all products
    /// </summary>
    [HttpPost("retrain")]
    public async Task<IActionResult> RetrainModel()
    {
        _logger.LogInformation("Retrain request received from frontend");

        if (_retrainingService.IsRetrainingInProgress)
        {
            return Conflict(new
            {
                success = false,
                message = "Retraining already in progress. Please wait...",
                status = "IN_PROGRESS"
            });
        }

        var result = await _retrainingService.TriggerRetrainingAsync("MANUAL");

        return Ok(new
        {
            success = result.Status == "COMPLETED",
            message = result.Status == "COMPLETED"
                ? $"Model retrained successfully for {result.SuccessfullyTrained} products"
                : result.ErrorMessage ?? "Retraining failed",
            status = result.Status,
            retrainingId = result.RetrainingId,
            totalProducts = result.TotalProductsRetrained,
            successCount = result.SuccessfullyTrained,
            failedCount = result.FailedCount,
            durationSeconds = (int)(result.DurationSeconds?.TotalSeconds ?? 0)
        });
    }

    /// <summary>
    /// Get current training status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _retrainingService.GetRetrainingStatusAsync();

        if (status is null)
        {
            return Ok(new
            {
                currentStatus = "IDLE",
                isTrainingInProgress = false
            });
        }

        return Ok(new
        {
            currentStatus = status.IsInProgress ? "TRAINING_IN_PROGRESS" : "IDLE",
            isTrainingInProgress = status.IsInProgress,
            currentProgress = new
            {
                processed = status.ProductsProcessed,
                total = status.TotalProducts,
                percentage = Math.Round(status.ProgressPercentage, 2)
            },
            lastTrainingDate = status.LastRetrainingDate,
            lastTrainingStatus = status.LastRetrainingStatus,
            nextScheduledRetrain = status.NextScheduledRetrain,
            autoRetrainingEnabled = status.AutoRetrainingEnabled,
            scheduledDay = status.ScheduledDay.ToString()
        });
    }

    /// <summary>
    /// Get forecast model metrics from last retraining
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var history = await _retrainingService.GetRetrainingHistoryAsync(1);
        var last = history.FirstOrDefault();

        if (last is null)
        {
            return Ok(new { currentModelVersion = "No model trained" });
        }

        return Ok(new
        {
            currentModelVersion = $"v{last.StartedAt:yyyyMMdd_HHmmss}",
            trainingDate = last.CompletedAt,
            totalProductsTrained = last.TotalProductsRetrained,
            successfullyTrained = last.SuccessfullyTrained,
            failedCount = last.FailedCount,
            successRate = last.TotalProductsRetrained > 0
                ? Math.Round((decimal)last.SuccessfullyTrained / last.TotalProductsRetrained * 100, 2)
                : (decimal?)null,
            algorithm = "TimeSeries (Exponential Smoothing / Linear Regression / SSA)"
        });
    }

    /// <summary>
    /// Get training history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
    {
        var history = await _retrainingService.GetRetrainingHistoryAsync(days);

        return Ok(history.Select(h => new
        {
            id = h.RetrainingId,
            trainingDate = h.StartedAt,
            completedAt = h.CompletedAt,
            status = h.Status,
            totalProducts = h.TotalProductsRetrained,
            successCount = h.SuccessfullyTrained,
            failedCount = h.FailedCount,
            triggeredBy = h.TriggerReason ?? "MANUAL",
            durationSeconds = (int)(h.Duration?.TotalSeconds ?? 0)
        }).ToList());
    }

    /// <summary>
    /// Get all model versions (retraining sessions)
    /// </summary>
    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions()
    {
        var history = await _retrainingService.GetRetrainingHistoryAsync(100);

        return Ok(history.Select((h, i) => new
        {
            id = h.RetrainingId,
            version = $"v{h.StartedAt:yyyyMMdd_HHmmss}",
            trainingDate = h.StartedAt,
            productCount = h.TotalProductsRetrained,
            successRate = h.TotalProductsRetrained > 0
                ? Math.Round((decimal)h.SuccessfullyTrained / h.TotalProductsRetrained * 100, 2)
                : (decimal?)null,
            isActive = i == 0
        }).ToList());
    }
}
