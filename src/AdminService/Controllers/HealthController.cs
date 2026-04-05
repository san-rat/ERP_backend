using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Route("")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    [HttpHead("health")]
    public IActionResult Health()
        => Ok("AdminService is running.");
}
