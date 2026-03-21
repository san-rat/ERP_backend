namespace ForecastService.Models
{
    /// <summary>
    /// Request model for manual retraining trigger
    /// </summary>
    public class RetrainingRequest
    {
        public string? Reason { get; set; }
        public List<Guid>? ProductIds { get; set; } // If null, retrain all
        public bool RetainExistingForecasts { get; set; } = true;
    }

    /// <summary>
    /// Result of a retraining operation
    /// </summary>
    public class RetrainingResult
    {
        public Guid RetrainingId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalProductsRetrained { get; set; }
        public int SuccessfullyTrained { get; set; }
        public int FailedCount { get; set; }
        public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, COMPLETED, FAILED
        public string? ErrorMessage { get; set; }
        public TimeSpan? DurationSeconds { get; set; }
        public List<string> RetainedForecastIds { get; set; } = new();
        public string? TriggerReason { get; set; }
    }

    /// <summary>
    /// Current status of retraining
    /// </summary>
    public class RetrainingStatus
    {
        public bool IsInProgress { get; set; }
        public Guid? CurrentRetrainingId { get; set; }
        public int ProductsProcessed { get; set; }
        public int TotalProducts { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime? LastRetrainingDate { get; set; }
        public string? LastRetrainingStatus { get; set; }
        public DateTime NextScheduledRetrain { get; set; }
        public bool AutoRetrainingEnabled { get; set; }
        public DayOfWeek ScheduledDay { get; set; }
    }

    /// <summary>
    /// Historical record of retraining events
    /// </summary>
    public class RetrainingHistory
    {
        public Guid RetrainingId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalProductsRetrained { get; set; }
        public int SuccessfullyTrained { get; set; }
        public int FailedCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TriggerReason { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    /// <summary>
    /// Stores the last trained forecast for a product
    /// </summary>
    public class ProductForecastCache
    {
        public Guid ProductId { get; set; }
        public Guid? LastForecastId { get; set; }
        public DateTime? LastTrainedDate { get; set; }
        public string Algorithm { get; set; } = "EXPONENTIAL_SMOOTHING";
        public decimal LastMAPE { get; set; }
        public decimal LastRMSE { get; set; }
        public decimal LastR2 { get; set; }
        public bool IsValid { get; set; } = true;
    }
}