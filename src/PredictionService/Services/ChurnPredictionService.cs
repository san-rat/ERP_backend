using PredictionService.ML;
using PredictionService.Models;
using PredictionService.Repositories;

namespace PredictionService.Services;

public class ChurnPredictionService : IChurnPredictionService
{
    private readonly IChurnRepository _repository;
    private readonly ChurnModelManager _modelManager;
    private readonly ILogger<ChurnPredictionService> _logger;

    public ChurnPredictionService(
        IChurnRepository repository,
        ChurnModelManager modelManager,
        ILogger<ChurnPredictionService> logger)
    {
        _repository = repository;
        _modelManager = modelManager;
        _logger = logger;
    }

    public async Task<ChurnPredictionOutput?> PredictChurnAsync(Guid customerId)
    {
        try
        {
            _logger.LogInformation("Predicting churn for customer {CustomerId}", customerId);

            // Step 1: Get features from SINGLE database
            var features = await _repository.GetCustomerFeaturesAsync(customerId);

            if (features == null)
            {
                _logger.LogWarning("Customer not found: {CustomerId}", customerId);
                return null;
            }

            // Step 2: Get tuple from ML model (ONLY CALL ONCE!)
            var (probability, riskLabel, importanceFeatures) = _modelManager.PredictChurn(features);

            // Step 3: Create prediction ID
            var predictionId = Guid.NewGuid();

            // Step 4: Build output object from tuple
            var prediction = new ChurnPredictionOutput
            {
                PredictionId = predictionId,
                CustomerId = customerId,
                ChurnProbability = probability,
                ChurnRiskLabel = riskLabel,
                ModelVersion = "v1.0",
                TopFactors = importanceFeatures
                    .Select(f => new ChurnFactor
                    {
                        FactorName = f.Item1,
                        Weight = f.Item2,
                        FeatureValue = GetFeatureValue(f.Item1, features),
                        ContributionPercentage = Math.Abs((decimal)(f.Item2 * probability)) * 100
                    })
                    .ToList(),
                PredictedAt = DateTime.UtcNow
            };

            // Step 5: Save to database
            await _repository.SaveChurnPredictionAsync(prediction);
            _logger.LogInformation("Prediction saved for customer {CustomerId}", prediction.CustomerId);

            // Step 6: Save factors
            await _repository.SaveChurnFactorsAsync(predictionId, prediction.TopFactors);
            _logger.LogInformation("Saved {FactorCount} factors", prediction.TopFactors.Count);

            // Step 7: Return prediction
            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting churn for customer {CustomerId}", customerId);
            return null;
        }
    }

    /// <summary>
    /// Convert feature name to human-readable value
    /// </summary>
    private string GetFeatureValue(string featureName, CustomerFeatures features)
    {
        return featureName switch
        {
            "Recency" => $"{features.Recency} days ago",
            "Return Rate" => $"{features.ReturnRate:P1} ({features.ReturnCount} returns)",
            "Cancellation Rate" => $"{features.CancellationRate:P1} ({features.CancelledOrders} cancelled)",
            "Inactivity" => features.InactiveFlag == 1 ? "Inactive for 180+ days" : "Active",
            "Order Frequency" => $"{features.Frequency} total orders",
            "Monetary Value" => $"${features.MonetaryValue:F2} lifetime value",
            "Product Diversity" => $"{features.ProductDiversity} unique products",
            "Account Age" => $"{features.AccountAgeDays} days old",
            _ => "N/A"
        };
    }
}