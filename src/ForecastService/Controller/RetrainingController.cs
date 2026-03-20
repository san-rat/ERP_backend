using Microsoft.AspNetCore.Mvc;
using ForecastService.Models;
using ForecastService.Services;

namespace ForecastService.Controllers
{
    [ApiController]
    [Route("api/forecasting/[controller]")]
    public class RetrainingController : ControllerBase
    {
        private readonly IRetrainingService _retrainingService;
        private readonly ILogger<RetrainingController> _logger;

        public RetrainingController(
            IRetrainingService retrainingService,
            ILogger<RetrainingController> logger)
        {
            _retrainingService = retrainingService;
            _logger = logger;
        }

        /// <summary>
        /// ✅ MAIN ENDPOINT: Manually trigger retraining for all products
        /// </summary>
        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerRetraining([FromBody] RetrainingRequest? request = null)
        {
            try
            {
                if (_retrainingService.IsRetrainingInProgress)
                {
                    return Conflict(new
                    {
                        error = "Retraining already in progress",
                        message = "Please wait for the current retraining to complete"
                    });
                }

                _logger.LogInformation("🔘 Manual retraining triggered - Reason: {Reason}",
                    request?.Reason ?? "No reason provided");

                var result = await _retrainingService.TriggerRetrainingAsync(request?.Reason);

                return Ok(new
                {
                    success = true,
                    retrainingId = result.RetrainingId,
                    status = result.Status,
                    startedAt = result.StartedAt,
                    message = "Retraining initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering retraining");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get current status of retraining (progress, next scheduled time)
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetRetrainingStatus()
        {
            try
            {
                var status = await _retrainingService.GetRetrainingStatusAsync();
                if (status is null)
                {
                    return NotFound(new { error = "No retraining status available" });
                }

                return Ok(new
                {
                    isInProgress = status.IsInProgress,
                    currentProgress = new
                    {
                        processed = status.ProductsProcessed,
                        total = status.TotalProducts,
                        percentage = Math.Round(status.ProgressPercentage, 2)
                    },
                    lastRetrain = new
                    {
                        date = status.LastRetrainingDate,
                        status = status.LastRetrainingStatus
                    },
                    nextScheduledRetrain = status.NextScheduledRetrain,
                    autoRetaining = new
                    {
                        enabled = status.AutoRetrainingEnabled,
                        scheduledDay = status.ScheduledDay.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retraining status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get history of all retraining operations
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetRetrainingHistory([FromQuery] int limit = 10)
        {
            try
            {
                var history = await _retrainingService.GetRetrainingHistoryAsync(limit);
                return Ok(new
                {
                    count = history.Count,
                    retrainingEvents = history.Select(h => new
                    {
                        retrainingId = h.RetrainingId,
                        startedAt = h.StartedAt,
                        completedAt = h.CompletedAt,
                        duration = h.Duration,
                        status = h.Status,
                        reason = h.TriggerReason,
                        results = new
                        {
                            totalProducts = h.TotalProductsRetrained,
                            successful = h.SuccessfullyTrained,
                            failed = h.FailedCount,
                            successRate = h.TotalProductsRetrained > 0
                                ? Math.Round((h.SuccessfullyTrained / (decimal)h.TotalProductsRetrained) * 100, 2)
                                : 0
                        }
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retraining history");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Enable or disable automatic weekly retraining
        /// </summary>
        [HttpPost("auto-retrain/toggle")]
        public IActionResult ToggleAutoRetaining([FromBody] ToggleAutoRetrainingRequest request)
        {
            try
            {
                _retrainingService.SetAutoRetrainingEnabled(request.Enabled);

                _logger.LogInformation(
                    "🔧 Auto retraining {Action}",
                    request.Enabled ? "ENABLED" : "DISABLED");

                return Ok(new
                {
                    success = true,
                    autoRetrainingEnabled = request.Enabled,
                    message = $"Auto retraining has been {(request.Enabled ? "enabled" : "disabled")}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling auto retraining");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Set the day of week for automatic retraining
        /// </summary>
        [HttpPost("auto-retrain/schedule")]
        public IActionResult SetRetrainingSchedule([FromBody] SetRetrainingScheduleRequest request)
        {
            try
            {
                if (!Enum.TryParse(request.DayOfWeek, out DayOfWeek day))
                {
                    return BadRequest(new
                    {
                        error = "Invalid day of week",
                        validDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
                    });
                }

                _retrainingService.SetAutoRetrainingDay(day);

                _logger.LogInformation(
                    "📅 Auto retraining schedule set to {Day}",
                    day);

                return Ok(new
                {
                    success = true,
                    scheduledDay = day.ToString(),
                    message = $"Auto retraining scheduled for {day}s at 2:00 AM UTC"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting retraining schedule");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed information about retraining configuration
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetRetrainingConfig()
        {
            try
            {
                return Ok(new
                {
                    backgroundWorker = new
                    {
                        enabled = true,
                        checkIntervalMinutes = 60,
                        description = "Checks every hour if it's time to retrain"
                    },
                    scheduling = new
                    {
                        description = "Weekly retraining at 2:00 AM UTC on configured day",
                        defaultDay = "Sunday",
                        configurable = true
                    },
                    features = new
                    {
                        manualTrigger = true,
                        autoScheduled = true,
                        progressTracking = true,
                        historyTracking = true,
                        enableDisable = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting retraining config");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for toggling auto retraining
    /// </summary>
    public class ToggleAutoRetrainingRequest
    {
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Request model for setting retraining schedule
    /// </summary>
    public class SetRetrainingScheduleRequest
    {
        public string DayOfWeek { get; set; } = "Sunday";
    }
}