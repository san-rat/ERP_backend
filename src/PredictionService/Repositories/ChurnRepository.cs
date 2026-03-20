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

            const string query = @"
                SELECT 
                    customer_id,
                    ISNULL(days_since_last_order, 999) AS recency,
                    ISNULL(total_orders, 0) AS frequency,
                    ISNULL(total_spent, 0) AS monetary_value,
                    ISNULL(avg_order_value, 0) AS avg_order_value,
                    ISNULL(customer_tenure_days, 0) AS tenure_days,
                    ISNULL(unique_products_purchased, 0) AS product_diversity,
                    ISNULL(unique_categories_purchased, 0) AS category_diversity,
                    ISNULL(avg_products_per_order, 0) AS avg_products_per_order,
                    ISNULL(total_returns, 0) AS return_count,
                    ISNULL(return_rate, 0) AS return_rate,
                    ISNULL(total_refunded, 0) AS total_refunded,
                    ISNULL(completed_orders, 0) AS completed_orders,
                    ISNULL(cancelled_orders, 0) AS cancelled_orders,
                    ISNULL(cancellation_rate, 0) AS cancellation_rate,
                    ISNULL(account_age_days, 0) AS account_age_days,
                    ISNULL(days_since_last_activity, 999) AS days_since_activity,
                    ISNULL(inactive_flag, 0) AS inactive_flag
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
                _logger.LogWarning("Customer {CustomerId} not found or has no order history", customerId);
                return null;
            }

            var features = new CustomerFeatures
            {
                CustomerId = reader.GetGuid(0),
                Recency = SafeGetInt32(reader, 1),
                Frequency = SafeGetInt32(reader, 2),
                MonetaryValue = SafeGetDecimal(reader, 3),
                AvgOrderValue = SafeGetDecimal(reader, 4),
                TenureDays = SafeGetInt32(reader, 5),
                ProductDiversity = SafeGetInt32(reader, 6),
                CategoryDiversity = SafeGetInt32(reader, 7),
                AvgProductsPerOrder = SafeGetDecimal(reader, 8),
                ReturnCount = SafeGetInt32(reader, 9),
                ReturnRate = SafeGetDecimal(reader, 10),
                TotalRefunded = SafeGetDecimal(reader, 11),
                CompletedOrders = SafeGetInt32(reader, 12),
                CancelledOrders = SafeGetInt32(reader, 13),
                CancellationRate = SafeGetDecimal(reader, 14),
                AccountAgeDays = SafeGetInt32(reader, 15),
                DaysSinceActivity = SafeGetInt32(reader, 16),
                InactiveFlag = SafeGetInt32(reader, 17)
            };

            _logger.LogInformation("Features fetched successfully for customer {CustomerId}", customerId);
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
                _logger.LogInformation("Prediction saved: {PredictionId} for customer {CustomerId}", 
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

            _logger.LogInformation("Saved {FactorCount} factors for prediction {PredictionId}", 
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

    // ════════════════════════════════════════════════════════════════════════════════
    // FIX #1: GetRecentPredictionsAsync - Close reader BEFORE getting factors
    // ════════════════════════════════════════════════════════════════════════════════

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

            // ✅ FIX: Read all data FIRST, close reader, THEN get factors
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
            } // ✅ Reader is now closed

            // ✅ NOW we can get factors (reader is closed)
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

    // ════════════════════════════════════════════════════════════════════════════════
    // FIX #2: GetAnalyticsByRiskLevelAsync - Close reader BEFORE returning
    // ════════════════════════════════════════════════════════════════════════════════

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

            // ✅ FIX: Read data, then immediately dispose reader
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = (
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                    reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                    reader.IsDBNull(3) ? 0 : reader.GetDecimal(3)
                );

                await reader.DisposeAsync(); // ✅ Explicitly close reader
                return result;
            }

            await reader.DisposeAsync(); // ✅ Explicitly close reader
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

    // ════════════════════════════════════════════════════════════════════════════════
    // HELPER METHOD: GetFactorsAsync
    // ════════════════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════════════════
    // HELPER METHODS: Safe Type Conversion
    // ════════════════════════════════════════════════════════════════════════════════

    private static decimal SafeGetDecimal(SqlDataReader reader, int ordinal)
    {
        try
        {
            return reader.GetDecimal(ordinal);
        }
        catch
        {
            try
            {
                return Convert.ToDecimal(reader.GetDouble(ordinal));
            }
            catch
            {
                return 0m;
            }
        }
    }

    private static int SafeGetInt32(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }
}