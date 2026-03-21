using PredictionService.Models;

namespace PredictionService.Repositories;

public interface IChurnRepository
{
    /// <summary>
    /// Get customer features for ML prediction from single database
    /// </summary>
    Task<CustomerFeatures?> GetCustomerFeaturesAsync(Guid customerId);

    /// <summary>
    /// Save churn prediction to database
    /// </summary>
    Task<bool> SaveChurnPredictionAsync(ChurnPredictionOutput prediction);

    /// <summary>
    /// Save churn factors to database
    /// </summary>
    Task<bool> SaveChurnFactorsAsync(Guid predictionId, List<ChurnFactor> factors);

    /// <summary>
    /// Get recent predictions
    /// </summary>
    Task<List<ChurnPredictionOutput>> GetRecentPredictionsAsync(int days = 7);

    /// <summary>
    /// Get analytics by risk level
    /// </summary>
    Task<(int TotalCustomersAtRisk, decimal AverageProbability, decimal MaxProbability, decimal MinProbability)> 
        GetAnalyticsByRiskLevelAsync(string riskLevel);
}