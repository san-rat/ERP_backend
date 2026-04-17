using Microsoft.Data.SqlClient;
using PredictionService.Models;

namespace PredictionService.Repositories;

public class TrainingDataRepository : ITrainingDataRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<TrainingDataRepository> _logger;
    private const string ConnectionName = "ChurnDb";

    public TrainingDataRepository(IConfiguration config, ILogger<TrainingDataRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Fetch ALL customer data from database for training.
    /// Churn label is determined by multiple risk signals:
    ///   - Never placed an order
    ///   - No order in 90+ days
    ///   - Cancellation rate above 50%
    ///   - Return rate above 40%
    /// </summary>
    public async Task<List<ChurnTrainingDataWithLabel>> GetAllTrainingDataAsync()
    {
        try
        {
            _logger.LogInformation("Fetching ALL training data from database");

            var connectionString = _config.GetConnectionString(ConnectionName);
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string not configured");
                return new List<ChurnTrainingDataWithLabel>();
            }

            const string query = @"
                SELECT 
                    c.id AS customer_id,
                    ISNULL(f.days_since_last_order, 999)            AS recency,
                    ISNULL(f.total_orders, 0)                       AS frequency,
                    ISNULL(f.total_spent, 0)                        AS monetary_value,
                    ISNULL(f.avg_order_value, 0)                    AS avg_order_value,
                    ISNULL(f.customer_tenure_days, 0)               AS tenure_days,
                    ISNULL(f.unique_products_purchased, 0)          AS product_diversity,
                    ISNULL(f.total_returns, 0)                      AS return_count,
                    CAST(ISNULL(f.return_rate, 0) AS FLOAT)         AS return_rate,
                    CAST(ISNULL(f.cancellation_rate, 0) AS FLOAT)   AS cancellation_rate,
                    ISNULL(f.completed_orders, 0)                   AS completed_orders,
                    ISNULL(f.cancelled_orders, 0)                   AS cancelled_orders,
                    ISNULL(f.inactive_flag, 0)                      AS inactive_flag,
                    CASE
                        -- Never placed an order
                        WHEN ISNULL(f.total_orders, 0) = 0
                        THEN 1
                        -- No order in 90+ days
                        WHEN ISNULL(f.days_since_last_order, 999) > 90
                        THEN 1
                        -- Cancelled more than half of their orders
                        WHEN CAST(ISNULL(f.cancellation_rate, 0) AS FLOAT) > 0.5
                        THEN 1
                        -- High return rate
                        WHEN CAST(ISNULL(f.return_rate, 0) AS FLOAT) > 0.4
                        THEN 1
                        ELSE 0
                    END AS label
                FROM dbo.customers c
                LEFT JOIN ml.v_customer_features_for_prediction f ON c.id = f.customer_id
                ORDER BY c.created_at DESC";

            var trainingData = new List<ChurnTrainingDataWithLabel>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 300;

            _logger.LogInformation("Executing query to fetch ALL training data...");
            await using var reader = await command.ExecuteReaderAsync();

            int recordCount = 0;
            while (await reader.ReadAsync())
            {
                trainingData.Add(new ChurnTrainingDataWithLabel
                {
                    CustomerId       = reader.GetGuid(0),
                    Recency          = SafeGetFloat(reader, 1),
                    Frequency        = SafeGetFloat(reader, 2),
                    MonetaryValue    = SafeGetFloat(reader, 3),
                    AvgOrderValue    = SafeGetFloat(reader, 4),
                    TenureDays       = SafeGetFloat(reader, 5),
                    ProductDiversity = SafeGetFloat(reader, 6),
                    ReturnCount      = SafeGetFloat(reader, 7),
                    ReturnRate       = SafeGetFloat(reader, 8),
                    CancellationRate = SafeGetFloat(reader, 9),
                    CompletedOrders  = SafeGetFloat(reader, 10),
                    CancelledOrders  = SafeGetFloat(reader, 11),
                    InactiveFlag     = SafeGetFloat(reader, 12),
                    Label            = reader.GetInt32(13) == 1
                });
                recordCount++;

                if (recordCount % 1000 == 0)
                    _logger.LogInformation("Fetched {Count} records so far...", recordCount);
            }

            _logger.LogInformation("Completed fetching ALL {Count} training records", trainingData.Count);

            int churnedCount = trainingData.Count(x => x.Label);
            int activeCount  = trainingData.Count - churnedCount;
            _logger.LogInformation("Churn distribution - Churned: {Churned}, Active: {Active}",
                churnedCount, activeCount);

            return trainingData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching training data");
            return new List<ChurnTrainingDataWithLabel>();
        }
    }

    /// <summary>
    /// Save or update training history (UPSERT to avoid duplicate PK on second save)
    /// </summary>
    public async Task SaveTrainingHistoryAsync(TrainingHistory history)
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);

            const string query = @"
                IF EXISTS (SELECT 1 FROM ml.training_history WHERE id = @id)
                    UPDATE ml.training_history SET
                        model_version_id   = @modelVersionId,
                        training_end_time  = @endTime,
                        training_status    = @status,
                        total_records_used = @recordCount,
                        churned_count      = @churnedCount,
                        non_churned_count  = @nonChurnedCount,
                        error_message      = @errorMessage
                    WHERE id = @id
                ELSE
                    INSERT INTO ml.training_history
                    (id, model_version_id, training_start_time, training_end_time, training_status,
                     total_records_used, churned_count, non_churned_count, error_message, triggered_by, created_at)
                    VALUES
                    (@id, @modelVersionId, @startTime, @endTime, @status, @recordCount,
                     @churnedCount, @nonChurnedCount, @errorMessage, @triggeredBy, GETUTCDATE())";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id",              history.Id);
            command.Parameters.AddWithValue("@modelVersionId",  history.ModelVersionId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@startTime",       history.TrainingStartTime);
            command.Parameters.AddWithValue("@endTime",         history.TrainingEndTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status",          history.TrainingStatus);
            command.Parameters.AddWithValue("@recordCount",     history.TotalRecordsUsed ?? 0);
            command.Parameters.AddWithValue("@churnedCount",    history.ChurnedCount ?? 0);
            command.Parameters.AddWithValue("@nonChurnedCount", history.NonChurnedCount ?? 0);
            command.Parameters.AddWithValue("@errorMessage",    (object?)history.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@triggeredBy",     history.TriggeredBy);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Training history saved with status: {Status}", history.TrainingStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving training history");
        }
    }

    public async Task<Guid> SaveModelVersionAsync(ModelVersionInfo modelInfo)
    {
        try
        {
            var modelId = Guid.NewGuid();
            var connectionString = _config.GetConnectionString(ConnectionName);

            const string query = @"
                INSERT INTO ml.model_versions
                (id, model_version, algorithm, training_date, training_data_count,
                 accuracy, precision, recall, auc_roc, total_features, is_active, created_at)
                VALUES
                (@id, @version, @algorithm, @trainingDate, @dataCount,
                 @accuracy, @precision, @recall, @aucRoc, @features, 0, GETUTCDATE())";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id",           modelId);
            command.Parameters.AddWithValue("@version",      modelInfo.ModelVersion);
            command.Parameters.AddWithValue("@algorithm",    modelInfo.Algorithm);
            command.Parameters.AddWithValue("@trainingDate", modelInfo.TrainingDate);
            command.Parameters.AddWithValue("@dataCount",    modelInfo.TrainingDataCount);
            command.Parameters.AddWithValue("@accuracy",     modelInfo.Accuracy  ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@precision",    modelInfo.Precision ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@recall",       modelInfo.Recall    ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@aucRoc",       modelInfo.AucRoc    ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@features",     12);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Model version saved: {ModelVersion}", modelInfo.ModelVersion);
            return modelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving model version");
            throw;
        }
    }

    public async Task<bool> SetActiveModelAsync(Guid modelVersionId)
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);

            const string query = @"
                BEGIN TRANSACTION
                UPDATE ml.model_versions SET is_active = 0;
                UPDATE ml.model_versions SET is_active = 1 WHERE id = @modelVersionId;
                COMMIT TRANSACTION";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@modelVersionId", modelVersionId);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Model set as active");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active model");
            return false;
        }
    }

    public async Task<ModelVersionInfo?> GetActiveModelAsync()
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);

            const string query = @"
                SELECT TOP 1
                    id, model_version, algorithm, training_date, training_data_count,
                    accuracy, precision, recall, auc_roc
                FROM ml.model_versions
                WHERE is_active = 1
                ORDER BY training_date DESC";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            await using var reader  = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ModelVersionInfo
                {
                    Id                = reader.GetGuid(0),
                    ModelVersion      = reader.GetString(1),
                    Algorithm         = reader.GetString(2),
                    TrainingDate      = reader.GetDateTime(3),
                    TrainingDataCount = reader.GetInt32(4),
                    Accuracy  = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    Precision = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    Recall    = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    AucRoc    = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active model");
            return null;
        }
    }

    public async Task<List<ModelVersionInfo>> GetAllModelVersionsAsync()
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);
            var models = new List<ModelVersionInfo>();

            const string query = @"
                SELECT 
                    id, model_version, algorithm, training_date, training_data_count,
                    accuracy, precision, recall, auc_roc
                FROM ml.model_versions
                ORDER BY training_date DESC";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            await using var reader  = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                models.Add(new ModelVersionInfo
                {
                    Id                = reader.GetGuid(0),
                    ModelVersion      = reader.GetString(1),
                    Algorithm         = reader.GetString(2),
                    TrainingDate      = reader.GetDateTime(3),
                    TrainingDataCount = reader.GetInt32(4),
                    Accuracy  = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    Precision = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    Recall    = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    AucRoc    = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
                });
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model versions");
            return new List<ModelVersionInfo>();
        }
    }

    public async Task<List<TrainingHistory>> GetTrainingHistoryAsync(int days = 30)
    {
        try
        {
            var connectionString = _config.GetConnectionString(ConnectionName);
            var history = new List<TrainingHistory>();

            const string query = @"
                SELECT 
                    id, model_version_id, training_start_time, training_end_time,
                    training_status, total_records_used, churned_count, non_churned_count,
                    error_message, triggered_by
                FROM ml.training_history
                WHERE created_at >= DATEADD(DAY, -@days, GETUTCDATE())
                ORDER BY training_start_time DESC";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@days", days);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                history.Add(new TrainingHistory
                {
                    Id                = reader.GetGuid(0),
                    ModelVersionId    = reader.IsDBNull(1) ? null : reader.GetGuid(1),
                    TrainingStartTime = reader.GetDateTime(2),
                    TrainingEndTime   = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    TrainingStatus    = reader.GetString(4),
                    TotalRecordsUsed  = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ChurnedCount      = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    NonChurnedCount   = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    ErrorMessage      = reader.IsDBNull(8) ? null : reader.GetString(8),
                    TriggeredBy       = reader.GetString(9)
                });
            }

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training history");
            return new List<TrainingHistory>();
        }
    }

    /// <summary>
    /// Safely reads any numeric SQL column as float regardless of underlying type.
    /// Handles INT, BIGINT, FLOAT, DECIMAL, REAL from SQL Server.
    /// </summary>
    private static float SafeGetFloat(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return 0f;

        var fieldType = reader.GetFieldType(ordinal);

        if (fieldType == typeof(double))  return (float)reader.GetDouble(ordinal);
        if (fieldType == typeof(decimal)) return (float)reader.GetDecimal(ordinal);
        if (fieldType == typeof(int))     return (float)reader.GetInt32(ordinal);
        if (fieldType == typeof(long))    return (float)reader.GetInt64(ordinal);
        if (fieldType == typeof(float))   return reader.GetFloat(ordinal);

        return Convert.ToSingle(reader.GetValue(ordinal));
    }
}