using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PredictionService.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _config;

    public HealthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("health")]
    public IActionResult Health()
        => Ok("PredictionService is running.");

    [HttpGet("db-check")]
    public async Task<IActionResult> DbCheck()
    {
        var cs = _config.GetConnectionString("ChurnDb") ?? string.Empty;
        var safe = Regex.Replace(cs, "(Password=)([^;]*)", "$1***", RegexOptions.IgnoreCase);

        if (string.IsNullOrWhiteSpace(cs))
        {
            return Problem(
                title: "DB connection string missing",
                detail: "ConnectionStrings:ChurnDb is not configured.",
                statusCode: 500);
        }

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    DB_NAME() AS database_name,
                    @@SERVERNAME AS server_name,
                    (SELECT COUNT(*) FROM dbo.customers) AS customer_count,
                    (SELECT COUNT(*) FROM ml.churn_predictions) AS prediction_count,
                    (SELECT COUNT(*) FROM ml.model_versions) AS model_version_count,
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS
                     WHERE TABLE_SCHEMA = 'ml'
                       AND TABLE_NAME = 'v_customer_features_for_prediction') AS feature_view_exists;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            var dbName      = reader.GetString(0);
            var serverName  = reader.GetString(1);
            var custCount   = reader.GetInt32(2);
            var predCount   = reader.GetInt32(3);
            var modelCount  = reader.GetInt32(4);
            var viewInCatalog = reader.GetInt32(5) == 1;
            await reader.DisposeAsync();

            // Try to actually query the feature view — catches missing sub-views
            string? featureViewError = null;
            int featureViewRowCount = 0;
            try
            {
                await using var viewCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ml.v_customer_features_for_prediction", conn);
                featureViewRowCount = (int)(await viewCmd.ExecuteScalarAsync())!;
            }
            catch (Exception ex)
            {
                featureViewError = ex.Message;
            }

            return Ok(new
            {
                status = "DB Connected",
                usingConn = safe,
                database = dbName,
                server = serverName,
                customerCount = custCount,
                predictionCount = predCount,
                modelVersionCount = modelCount,
                featureViewInCatalog = viewInCatalog,
                featureViewRowCount,
                featureViewError
            });
        }
        catch (Exception ex)
        {
            return Problem(
                title: "DB connection failed",
                detail: ex.Message + " | usingConn=" + safe,
                statusCode: 500);
        }
    }
}
