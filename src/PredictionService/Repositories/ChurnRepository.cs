using Microsoft.Data.SqlClient;
using PredictionService.Models;

namespace PredictionService.Repositories;

public class ChurnRepository : IChurnRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<ChurnRepository> _logger;
    private const string ConnectionName = "ChurnDb";

    public ChurnRepository(IConfiguration config, ILogger<ChurnRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Get customer features for churn prediction
    /// Uses ONLY columns available in ml.v_customer_features_for_prediction
    /// </summary>
    public async Task<CustomerFeatures?> GetCustomerFeaturesAsync(Guid customerId)
    {
        try
        {
            _logger.LogInformation("Fetching features for customer {CustomerId}", customerId);

            var connectionString = _config.GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string '{ConnectionName}' not found", ConnectionName);
                return null;
            }

            // ✅ FIXED: Only select columns that exist in the view
            const string query = @"
                SELECT 
                    customer_id,
                    first_name,
                    last_name,
                    email,
                    phone,
                    created_at,
                    updated_at
                FROM ml.v_customer_features_for_prediction
                WHERE customer_id = @customerId";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@customerId", customerId);
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                _logger.LogWarning("Customer {CustomerId} not found", customerId);
                return null;
            }

            // Calculate derived features from available data
            var createdAt = reader.GetDateTime(5);
            var updatedAt = reader.GetDateTime(6);
            var tenureDays = (int)(DateTime.UtcNow - createdAt).TotalDays;
            var daysSinceActivity = (int)(DateTime.UtcNow - updatedAt).TotalDays;

            var features = new CustomerFeatures
            {
                CustomerId = reader.GetGuid(0),
                // Simple RFM: use tenure and inactivity as proxies
                Recency = daysSinceActivity,  // Days since last activity
                Frequency = 0,  // Would need order data, defaulting to 0
                MonetaryValue = 0,  // Would need order data, defaulting to 0
                AvgOrderValue = 0,  // Would need order data, defaulting to 0
                TenureDays = tenureDays,  // Account age in days
                ProductDiversity = 0,  // Would need product data
                CategoryDiversity = 0,  // Would need category data
                AvgProductsPerOrder = 0,  // Would need order data
                ReturnCount = 0,  // Would need return data
                ReturnRate = 0,  // Would need return data
                TotalRefunded = 0,  // Would need refund data
                CompletedOrders = 0,  // Would need order data
                CancelledOrders = 0,  // Would need order data
                CancellationRate = 0,  // Would need order data
                AccountAgeDays = tenureDays,
                DaysSinceActivity = daysSinceActivity,
                InactiveFlag = daysSinceActivity > 180 ? 1 : 0  // Inactive if 180+ days
            };

            _logger.LogInformation("✓ Features fetched for customer {CustomerId}", customerId);
            return features;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error fetching features for customer {CustomerId}", customerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching features for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<bool> SaveChurnPredictionAsync(ChurnPredictionOutput prediction)
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string not configured");
                return false;
            }

            const string query = @"
                INSERT INTO ml.churn_predictions 
                (id, customer_id, churn_probability, churn_risk_label, model_version, predicted_at, created_at)
                VALUES (@id, @customerId, @probability, @riskLabel, @modelVersion, @predictedAt, GETUTCDATE())";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", prediction.PredictionId);
            command.Parameters.AddWithValue("@customerId", prediction.CustomerId);
            command.Parameters.AddWithValue("@probability", prediction.ChurnProbability);
            command.Parameters.AddWithValue("@riskLabel", prediction.ChurnRiskLabel);
            command.Parameters.AddWithValue("@modelVersion", prediction.ModelVersion);
            command.Parameters.AddWithValue("@predictedAt", prediction.PredictedAt);
            command.CommandTimeout = 30;

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("✓ Prediction saved: {PredictionId} for customer {CustomerId}", 
                    prediction.PredictionId, prediction.CustomerId);
            }

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error saving prediction");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving prediction");
            return false;
        }
    }

    public async Task<bool> SaveChurnFactorsAsync(Guid predictionId, List<ChurnFactor> factors)
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string not configured");
                return false;
            }

            const string query = @"
                INSERT INTO ml.churn_factors 
                (id, churn_prediction_id, factor_name, factor_weight, feature_value)
                VALUES (@id, @predictionId, @factorName, @weight, @value)";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var factor in factors)
            {
                await using var command = new SqlCommand(query, connection);
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@id", Guid.NewGuid());
                command.Parameters.AddWithValue("@predictionId", predictionId);
                command.Parameters.AddWithValue("@factorName", factor.FactorName);
                command.Parameters.AddWithValue("@weight", factor.Weight);
                command.Parameters.AddWithValue("@value", (object?)factor.FeatureValue ?? DBNull.Value);
                command.CommandTimeout = 30;

                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("✓ Saved {FactorCount} factors for prediction {PredictionId}", 
                factors.Count, predictionId);
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error saving factors");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving factors");
            return false;
        }
    }

    public async Task<List<ChurnPredictionOutput>> GetRecentPredictionsAsync(int days = 7)
    {
        try
        {
            _logger.LogInformation("Retrieving churn predictions from the last {Days} days", days);

            var predictions = new List<ChurnPredictionOutput>();
            var connectionString = _config.GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string not configured");
                return predictions;
            }

            const string query = @"
                SELECT TOP 100
                    id,
                    customer_id,
                    churn_probability,
                    churn_risk_label,
                    model_version,
                    predicted_at
                FROM ml.churn_predictions
                WHERE predicted_at >= DATEADD(DAY, -@days, GETUTCDATE())
                ORDER BY predicted_at DESC";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@days", days);
            command.CommandTimeout = 30;

            var predictionData = new List<(Guid id, Guid customerId, decimal probability, string label, string version, DateTime time)>();

            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    predictionData.Add((
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetDecimal(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetDateTime(5)
                    ));
                }
            }

            foreach (var data in predictionData)
            {
                var factors = await GetFactorsAsync(connection, data.id);

                predictions.Add(new ChurnPredictionOutput
                {
                    PredictionId = data.id,
                    CustomerId = data.customerId,
                    ChurnProbability = data.probability,
                    ChurnRiskLabel = data.label,
                    ModelVersion = data.version,
                    PredictedAt = data.time,
                    TopFactors = factors
                });
            }

            _logger.LogInformation("Retrieved {Count} predictions from last {Days} days", 
                predictions.Count, days);

            return predictions;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving recent predictions");
            return new List<ChurnPredictionOutput>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving recent predictions");
            return new List<ChurnPredictionOutput>();
        }
    }

    public async Task<(int TotalCustomersAtRisk, decimal AverageProbability, decimal MaxProbability, decimal MinProbability)>
        GetAnalyticsByRiskLevelAsync(string riskLevel)
    {
        try
        {
            _logger.LogInformation("Retrieving analytics for risk level: {RiskLevel}", riskLevel);

            var connectionString = _config.GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string not configured");
                return (0, 0, 0, 0);
            }

            const string query = @"
                SELECT 
                    COUNT(*) as TotalCount,
                    AVG(CAST(churn_probability AS DECIMAL(10, 6))) as AvgProb,
                    MAX(churn_probability) as MaxProb,
                    MIN(churn_probability) as MinProb
                FROM ml.churn_predictions
                WHERE churn_risk_label = @riskLevel
                  AND predicted_at >= DATEADD(DAY, -30, GETUTCDATE())";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@riskLevel", riskLevel);
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = (
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                    reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                    reader.IsDBNull(3) ? 0 : reader.GetDecimal(3)
                );

                await reader.DisposeAsync();
                return result;
            }

            await reader.DisposeAsync();
            return (0, 0, 0, 0);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving analytics");
            return (0, 0, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving analytics");
            return (0, 0, 0, 0);
        }
    }

    private async Task<List<ChurnFactor>> GetFactorsAsync(SqlConnection connection, Guid predictionId)
    {
        try
        {
            var factors = new List<ChurnFactor>();

            const string query = @"
                SELECT factor_name, factor_weight, feature_value
                FROM ml.churn_factors
                WHERE churn_prediction_id = @predictionId
                ORDER BY factor_weight DESC";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@predictionId", predictionId);
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                factors.Add(new ChurnFactor
                {
                    FactorName = reader.GetString(0),
                    Weight = reader.GetDecimal(1),
                    FeatureValue = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                });
            }

            return factors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving factors for prediction {PredictionId}", predictionId);
            return new List<ChurnFactor>();
        }
    }
}