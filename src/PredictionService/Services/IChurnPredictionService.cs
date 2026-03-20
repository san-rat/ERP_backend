using PredictionService.Models;

namespace PredictionService.Services;

public interface IChurnPredictionService
{
    Task<ChurnPredictionOutput?> PredictChurnAsync(Guid customerId);
}