using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

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

    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok("AuthService is running - and sanuk is testing - dulain is watching.");
    }

    [Authorize]
    [HttpGet("db-check")]

    public async Task<IActionResult> DbCheck()
    {
        var cs = _config.GetConnectionString("AuthDb") ?? "";
        var safe = Regex.Replace(cs, "(Password=)([^;]*)", "$1***", RegexOptions.IgnoreCase);

        try
        {
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();

            await using var who = new MySqlCommand("SELECT USER(), CURRENT_USER(), @@hostname;", conn);
            await using var reader = await who.ExecuteReaderAsync();
            await reader.ReadAsync();

            var user = reader.GetString(0);
            var currentUser = reader.GetString(1);
            var host = reader.GetString(2);

            return Ok(new
            {
                status = "DB Connected",
                usingConn = safe,
                user,
                currentUser,
                host
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