using Microsoft.ML;
using Microsoft.ML.Data;
using PredictionService.Models;
using PredictionService.Repositories;

namespace PredictionService.ML;

public class ChurnModelManager
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly FeatureNormalizer _normalizer;
    private readonly ILogger<ChurnModelManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ChurnModelManager(
        ILogger<ChurnModelManager> logger,
        IServiceScopeFactory scopeFactory)
    {
        _mlContext = new MLContext(seed: 42);
        _normalizer = new FeatureNormalizer();
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Train model using ALL real data from database
    /// </summary>
    public virtual async Task<ModelTrainingResult> TrainModelWithRealDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting model training with ALL real data from database");

            var result = new ModelTrainingResult
            {
                TrainingStartTime = DateTime.UtcNow,
                Status = "IN_PROGRESS"
            };

            // STEP 1: Fetch ALL real data from database using a fresh scope
            _logger.LogInformation("Fetching ALL training data from database...");
            List<ChurnTrainingDataWithLabel> trainingData;
            using (var scope = _scopeFactory.CreateScope())
            {
                var trainingDataRepository = scope.ServiceProvider
                    .GetRequiredService<ITrainingDataRepository>();
                trainingData = await trainingDataRepository.GetAllTrainingDataAsync();
            }

            if (trainingData.Count == 0)
            {
                _logger.LogWarning("No training data found!");
                result.Status = "FAILED";
                result.ErrorMessage = "No training data available";
                return result;
            }

            _logger.LogInformation("✓ Fetched {Count} training records", trainingData.Count);
            result.TotalRecordsUsed = trainingData.Count;
            result.ChurnedCount = trainingData.Count(x => x.Label);
            result.NonChurnedCount = trainingData.Count(x => !x.Label);

            _logger.LogInformation("Distribution - Churned: {Churned}, Active: {Active}",
                result.ChurnedCount, result.NonChurnedCount);

            // STEP 2: Train model
            _logger.LogInformation("Training ML model on ALL {Count} records...", trainingData.Count);
            var data = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    new[]
                    {
                        nameof(ChurnTrainingDataWithLabel.Recency),
                        nameof(ChurnTrainingDataWithLabel.Frequency),
                        nameof(ChurnTrainingDataWithLabel.MonetaryValue),
                        nameof(ChurnTrainingDataWithLabel.AvgOrderValue),
                        nameof(ChurnTrainingDataWithLabel.TenureDays),
                        nameof(ChurnTrainingDataWithLabel.ProductDiversity),
                        nameof(ChurnTrainingDataWithLabel.ReturnCount),
                        nameof(ChurnTrainingDataWithLabel.ReturnRate),
                        nameof(ChurnTrainingDataWithLabel.CancellationRate),
                        nameof(ChurnTrainingDataWithLabel.CompletedOrders),
                        nameof(ChurnTrainingDataWithLabel.CancelledOrders),
                        nameof(ChurnTrainingDataWithLabel.InactiveFlag)
                    })
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(ChurnTrainingDataWithLabel.Label),
                    featureColumnName: "Features"));

            _model = pipeline.Fit(data);
            _logger.LogInformation("✓ Model trained successfully");

            // STEP 3: Evaluate model
            _logger.LogInformation("Evaluating model performance...");
            var predictions = _model.Transform(data);
            var metrics = _mlContext.BinaryClassification.Evaluate(predictions);

            result.Accuracy = (decimal)metrics.Accuracy;
            result.Precision = (decimal)metrics.PositivePrecision;
            result.Recall = (decimal)metrics.PositiveRecall;
            result.AucRoc = (decimal)metrics.AreaUnderRocCurve;

            _logger.LogInformation(
                "✓ Model Metrics - Accuracy: {Accuracy:P2}, Precision: {Precision:P2}, Recall: {Recall:P2}, AUC: {AUC:P2}",
                metrics.Accuracy, metrics.PositivePrecision, metrics.PositiveRecall,
                metrics.AreaUnderRocCurve);

            result.Status = "COMPLETED";
            result.TrainingEndTime = DateTime.UtcNow;

            _logger.LogInformation("Training completed in {Duration} seconds",
                result.Duration.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model with real data");
            return new ModelTrainingResult
            {
                Status = "FAILED",
                ErrorMessage = ex.Message,
                TrainingStartTime = DateTime.UtcNow,
                TrainingEndTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Make prediction using current model
    /// </summary>
    public virtual (decimal probability, string riskLabel, List<(string, decimal)> featureImportance)
        PredictChurn(CustomerFeatures features)
    {
        if (_model == null)
            return (0.5m, "UNKNOWN", new List<(string, decimal)>());

        try
        {
            var normalizedFeatures = _normalizer.NormalizeFeatures(features);

            var input = new ChurnModelInput
            {
                Recency = normalizedFeatures[0],
                Frequency = normalizedFeatures[1],
                MonetaryValue = normalizedFeatures[2],
                AvgOrderValue = normalizedFeatures[3],
                TenureDays = normalizedFeatures[4],
                ProductDiversity = normalizedFeatures[5],
                ReturnCount = normalizedFeatures[6],
                ReturnRate = normalizedFeatures[7],
                CancellationRate = normalizedFeatures[8],
                CompletedOrders = normalizedFeatures[9],
                CancelledOrders = normalizedFeatures[10],
                InactiveFlag = normalizedFeatures[11]
            };

            var predictionEngine = _mlContext.Model
                .CreatePredictionEngine<ChurnModelInput, ChurnModelOutput>(_model);
            var prediction = predictionEngine.Predict(input);

            var probability = (decimal)prediction.Probability;
            var riskLabel = probability switch
            {
                < 0.33m => "LOW",
                < 0.67m => "MEDIUM",
                _ => "HIGH"
            };

            var featureImportance = new List<(string, decimal)>
            {
                ("Recency", (decimal)features.Recency / 365 * probability),
                ("Return Rate", features.ReturnRate * probability),
                ("Cancellation Rate", features.CancellationRate * probability),
                ("Inactivity", (decimal)features.InactiveFlag * probability),
                ("Order Frequency", (decimal)features.Frequency / 100 * (1 - probability))
            };

            return (probability, riskLabel, featureImportance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prediction error");
            return (0.5m, "ERROR", new List<(string, decimal)>());
        }
    }
}