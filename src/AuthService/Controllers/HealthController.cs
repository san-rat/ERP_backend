using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AuthService.Controllers;

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
    {
        return Ok("AuthService is running - and sanuk is testing - dulain is watching.");
    }

    [HttpGet("db-check")]

    public async Task<IActionResult> DbCheck()
    {
        var cs = _config.GetConnectionString("AuthDb") ?? "";
        var safe = Regex.Replace(cs, "(Password=)([^;]*)", "$1***", RegexOptions.IgnoreCase);

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var who = new SqlCommand("SELECT SUSER_SNAME(), CURRENT_USER, @@SERVERNAME, (SELECT COUNT(*) FROM auth.users);", conn);
            await using var reader = await who.ExecuteReaderAsync();
            await reader.ReadAsync();

            var user = reader.GetString(0);
            var currentUser = reader.GetString(1);
            var host = reader.GetString(2);
            var userCount = reader.GetInt32(3);

            return Ok(new
            {
                status = "DB Connected",
                schema = "auth",
                usingConn = safe,
                user,
                currentUser,
                host,
                userCount
            });
        }
        catch (Exception ex)
        {
            return Problem(
                title: "DB connection failed",
                detail: ex.Message + " | usingConn=" + safe,
                statusCode: 500
            );
        }
    }
}