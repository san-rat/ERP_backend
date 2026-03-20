using PredictionService.ML;
using PredictionService.Models;
using PredictionService.Repositories;

namespace PredictionService.Services;

public class ModelRetrainingService : IModelRetrainingService
{
    private readonly ChurnModelManager _modelManager;
    private readonly ITrainingDataRepository _trainingDataRepository;
    private readonly ILogger<ModelRetrainingService> _logger;
    private bool _isTrainingInProgress = false;

    public ModelRetrainingService(
        ChurnModelManager modelManager,
        ITrainingDataRepository trainingDataRepository,
        ILogger<ModelRetrainingService> logger)
    {
        _modelManager = modelManager;
        _trainingDataRepository = trainingDataRepository;
        _logger = logger;
    }

    public async Task<ModelRetrainingResponse> RetrainModelAsync()
    {
        if (_isTrainingInProgress)
        {
            return new ModelRetrainingResponse
            {
                Success = false,
                Message = "Training already in progress. Please wait...",
                Status = "IN_PROGRESS"
            };
        }

        try
        {
            _isTrainingInProgress = true;
            var trainingHistoryId = Guid.NewGuid();

            _logger.LogInformation("🔄 Starting manual model retraining with ALL database records");

            var trainingHistory = new TrainingHistory
            {
                Id = trainingHistoryId,
                TrainingStartTime = DateTime.UtcNow,
                TrainingStatus = "IN_PROGRESS",
                TriggeredBy = "MANUAL"
            };

            await _trainingDataRepository.SaveTrainingHistoryAsync(trainingHistory);

            // Train model with ALL real data
            var trainingResult = await _modelManager.TrainModelWithRealDataAsync();

            // Update training history
            trainingHistory.TrainingEndTime = DateTime.UtcNow;
            trainingHistory.TrainingStatus = trainingResult.Status;
            trainingHistory.TotalRecordsUsed = trainingResult.TotalRecordsUsed;
            trainingHistory.ChurnedCount = trainingResult.ChurnedCount;
            trainingHistory.NonChurnedCount = trainingResult.NonChurnedCount;
            trainingHistory.ErrorMessage = trainingResult.ErrorMessage;

            if (trainingResult.Status == "COMPLETED")
            {
                var modelInfo = new ModelVersionInfo
                {
                    ModelVersion = $"v{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    Algorithm = "SdcaLogisticRegression",
                    TrainingDate = DateTime.UtcNow,
                    TrainingDataCount = trainingResult.TotalRecordsUsed ?? 0,
                    Accuracy = trainingResult.Accuracy,
                    Precision = trainingResult.Precision,
                    Recall = trainingResult.Recall,
                    AucRoc = trainingResult.AucRoc
                };

                var modelVersionId = await _trainingDataRepository.SaveModelVersionAsync(modelInfo);
                trainingHistory.ModelVersionId = modelVersionId;

                await _trainingDataRepository.SetActiveModelAsync(modelVersionId);

                _logger.LogInformation(
                    "✅ Training completed! Accuracy: {Accuracy:P2}, Records: {Count}, Duration: {Duration}s",
                    trainingResult.Accuracy, trainingResult.TotalRecordsUsed, trainingResult.Duration.TotalSeconds);

                await _trainingDataRepository.SaveTrainingHistoryAsync(trainingHistory);

                return new ModelRetrainingResponse
                {
                    Success = true,
                    Message = $"✅ Model trained successfully with {trainingResult.TotalRecordsUsed} records",
                    Status = "COMPLETED",
                    TrainingHistoryId = trainingHistoryId,
                    RecordsUsed = trainingResult.TotalRecordsUsed,
                    Accuracy = trainingResult.Accuracy,
                    DurationSeconds = (int)trainingResult.Duration.TotalSeconds
                };
            }
            else
            {
                await _trainingDataRepository.SaveTrainingHistoryAsync(trainingHistory);

                return new ModelRetrainingResponse
                {
                    Success = false,
                    Message = trainingResult.ErrorMessage ?? "Training failed",
                    Status = "FAILED",
                    TrainingHistoryId = trainingHistoryId
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during model retraining");
            return new ModelRetrainingResponse
            {
                Success = false,
                Message = ex.Message,
                Status = "FAILED"
            };
        }
        finally
        {
            _isTrainingInProgress = false;
        }
    }

    public async Task<TrainingStatusResponse> GetTrainingStatusAsync()
    {
        var activeModel = await _trainingDataRepository.GetActiveModelAsync();

        return new TrainingStatusResponse
        {
            CurrentStatus = _isTrainingInProgress ? "TRAINING_IN_PROGRESS" : "IDLE",
            CurrentModel = activeModel?.ModelVersion,
            LastTrainingDate = activeModel?.TrainingDate,
            LastTrainingRecordCount = activeModel?.TrainingDataCount,
            IsTrainingInProgress = _isTrainingInProgress
        };
    }

    public async Task<List<TrainingHistoryResponse>> GetTrainingHistoryAsync(int days = 30)
    {
        var history = await _trainingDataRepository.GetTrainingHistoryAsync(days);

        return history.Select(h => new TrainingHistoryResponse
        {
            Id = h.Id,
            TrainingDate = h.TrainingStartTime,
            Status = h.TrainingStatus ?? "UNKNOWN",
            RecordCount = h.TotalRecordsUsed ?? 0,
            ChurnedCount = h.ChurnedCount ?? 0,
            ActiveCount = (h.TotalRecordsUsed ?? 0) - (h.ChurnedCount ?? 0),
            Accuracy = null, // Would need to fetch from model_versions
            TriggeredBy = h.TriggeredBy,
            DurationSeconds = h.TrainingEndTime.HasValue 
                ? (int)(h.TrainingEndTime.Value - h.TrainingStartTime).TotalSeconds 
                : 0
        }).ToList();
    }

    public async Task<ModelMetricsResponse> GetModelMetricsAsync()
    {
        var activeModel = await _trainingDataRepository.GetActiveModelAsync();

        if (activeModel == null)
        {
            return new ModelMetricsResponse
            {
                CurrentModelVersion = "No active model"
            };
        }

        return new ModelMetricsResponse
        {
            CurrentModelVersion = activeModel.ModelVersion,
            TrainingDate = activeModel.TrainingDate,
            TrainingDataCount = activeModel.TrainingDataCount,
            Accuracy = activeModel.Accuracy,
            Precision = activeModel.Precision,
            Recall = activeModel.Recall,
            AucRoc = activeModel.AucRoc
        };
    }

    public async Task<List<ModelVersionResponse>> GetAllModelVersionsAsync()
    {
        var versions = await _trainingDataRepository.GetAllModelVersionsAsync();

        return versions.Select(v => new ModelVersionResponse
        {
            Id = v.Id ?? Guid.Empty,
            Version = v.ModelVersion,
            TrainingDate = v.TrainingDate,
            RecordCount = v.TrainingDataCount,
            Accuracy = v.Accuracy,
            IsActive = false // Would need to fetch from database
        }).ToList();
    }
}