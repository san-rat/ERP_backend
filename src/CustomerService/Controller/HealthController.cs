using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controller
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("/health")]
        [HttpGet("/api/health")]
        public IActionResult Get()
        {
            return Ok(new
            {
                success = true,
                message = "Customer Commerce API is running"
            });
        }
    }
}
