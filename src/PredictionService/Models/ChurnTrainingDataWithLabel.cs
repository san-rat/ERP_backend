namespace PredictionService.Models;

public class ChurnTrainingDataWithLabel
{
    
    [Microsoft.ML.Data.NoColumn]

    public Guid CustomerId { get; set; }
    public float Recency { get; set; }
    public float Frequency { get; set; }
    public float MonetaryValue { get; set; }
    public float AvgOrderValue { get; set; }
    public float TenureDays { get; set; }
    public float ProductDiversity { get; set; }
    public float ReturnCount { get; set; }
    public float ReturnRate { get; set; }
    public float CancellationRate { get; set; }
    public float CompletedOrders { get; set; }
    public float CancelledOrders { get; set; }
    public float InactiveFlag { get; set; }
    public bool Label { get; set; }  // TRUE = churned, FALSE = active
}

public class TrainingHistory
{
    public Guid Id { get; set; }
    public Guid? ModelVersionId { get; set; }
    public DateTime TrainingStartTime { get; set; }
    public DateTime? TrainingEndTime { get; set; }
    public string TrainingStatus { get; set; } = string.Empty;
    public int? TotalRecordsUsed { get; set; }
    public int? ChurnedCount { get; set; }
    public int? NonChurnedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
}

public class ModelVersionInfo
{
    public Guid? Id { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public DateTime TrainingDate { get; set; }
    public int TrainingDataCount { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Precision { get; set; }
    public decimal? Recall { get; set; }
    public decimal? AucRoc { get; set; }
}

public class ModelTrainingResult
{
    public string Status { get; set; } = string.Empty;
    public DateTime TrainingStartTime { get; set; }
    public DateTime? TrainingEndTime { get; set; }
    public int? TotalRecordsUsed { get; set; }
    public int? ChurnedCount { get; set; }
    public int? NonChurnedCount { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Precision { get; set; }
    public decimal? Recall { get; set; }
    public decimal? AucRoc { get; set; }
    public string? ErrorMessage { get; set; }

    public TimeSpan Duration => TrainingEndTime.HasValue 
        ? TrainingEndTime.Value - TrainingStartTime 
        : TimeSpan.Zero;
}