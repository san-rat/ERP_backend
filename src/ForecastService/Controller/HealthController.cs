using Microsoft.AspNetCore.Mvc;

namespace ForecastService.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
        => Ok("ForecastService is running.");
}