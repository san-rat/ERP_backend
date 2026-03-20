namespace PredictionService.Services;

public interface IModelRetrainingService
{
    Task<ModelRetrainingResponse> RetrainModelAsync();
    Task<TrainingStatusResponse> GetTrainingStatusAsync();
    Task<List<TrainingHistoryResponse>> GetTrainingHistoryAsync(int days = 30);
    Task<ModelMetricsResponse> GetModelMetricsAsync();
    Task<List<ModelVersionResponse>> GetAllModelVersionsAsync();
}

public class ModelRetrainingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? TrainingHistoryId { get; set; }
    public int? RecordsUsed { get; set; }
    public decimal? Accuracy { get; set; }
    public int DurationSeconds { get; set; }
}

public class TrainingStatusResponse
{
    public string CurrentStatus { get; set; } = string.Empty;
    public string? CurrentModel { get; set; }
    public DateTime? LastTrainingDate { get; set; }
    public int? LastTrainingRecordCount { get; set; }
    public bool IsTrainingInProgress { get; set; }
}

public class TrainingHistoryResponse
{
    public Guid Id { get; set; }
    public DateTime TrainingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public int ChurnedCount { get; set; }
    public int ActiveCount { get; set; }
    public decimal? Accuracy { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
}

public class ModelMetricsResponse
{
    public string CurrentModelVersion { get; set; } = string.Empty;
    public DateTime? TrainingDate { get; set; }
    public int? TrainingDataCount { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Precision { get; set; }
    public decimal? Recall { get; set; }
    public decimal? AucRoc { get; set; }
}

public class ModelVersionResponse
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime TrainingDate { get; set; }
    public int RecordCount { get; set; }
    public decimal? Accuracy { get; set; }
    public bool IsActive { get; set; }
}