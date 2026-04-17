using Microsoft.AspNetCore.Mvc;
using PredictionService.Services;

namespace PredictionService.Controllers;

[ApiController]
[Route("api/ml")]
public class ModelManagementController : ControllerBase
{
    private readonly IModelRetrainingService _retrainingService;
    private readonly ILogger<ModelManagementController> _logger;

    public ModelManagementController(
        IModelRetrainingService retrainingService,
        ILogger<ModelManagementController> logger)
    {
        _retrainingService = retrainingService;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger model retraining with ALL database records
    /// </summary>
    [HttpPost("retrain")]
    public async Task<IActionResult> RetrainModel()
    {
        _logger.LogInformation("📢 Retrain request received from frontend");
        var result = await _retrainingService.RetrainModelAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get current training status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _retrainingService.GetTrainingStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Get model metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _retrainingService.GetModelMetricsAsync();
        return Ok(metrics);
    }

    /// <summary>
    /// Get training history
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 30)
    {
        var history = await _retrainingService.GetTrainingHistoryAsync(days);
        return Ok(history);
    }

    /// <summary>
    /// Get all model versions
    /// </summary>
    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions()
    {
        var versions = await _retrainingService.GetAllModelVersionsAsync();
        return Ok(versions);
    }
}