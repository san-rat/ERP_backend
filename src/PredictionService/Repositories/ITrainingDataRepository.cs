using PredictionService.Models;

namespace PredictionService.Repositories;

public interface ITrainingDataRepository
{
    Task<List<ChurnTrainingDataWithLabel>> GetAllTrainingDataAsync();
    Task SaveTrainingHistoryAsync(TrainingHistory history);
    Task<Guid> SaveModelVersionAsync(ModelVersionInfo modelInfo);
    Task<bool> SetActiveModelAsync(Guid modelVersionId);
    Task<ModelVersionInfo?> GetActiveModelAsync();
    Task<List<ModelVersionInfo>> GetAllModelVersionsAsync();
    Task<List<TrainingHistory>> GetTrainingHistoryAsync(int days = 30);
}