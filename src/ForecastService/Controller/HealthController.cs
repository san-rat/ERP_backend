using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ForecastService.Controllers;

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
        => Ok("ForecastService is running.");

    [HttpGet("db-check")]
    public async Task<IActionResult> DbCheck()
    {
        var cs = _config.GetConnectionString("insighterp_db") ?? string.Empty;
        var safe = Regex.Replace(cs, "(Password=)([^;]*)", "$1***", RegexOptions.IgnoreCase);

        if (string.IsNullOrWhiteSpace(cs))
        {
            return Problem(
                title: "DB connection string missing",
                detail: "ConnectionStrings:insighterp_db is not configured.",
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
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                     WHERE TABLE_SCHEMA = 'dbo') AS table_count;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return Ok(new
            {
                status = "DB Connected",
                usingConn = safe,
                database = reader.GetString(0),
                server = reader.GetString(1),
                tableCount = reader.GetInt32(2)
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