using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controller
{
    [ApiController]
    [Route("")]
    public class HealthController : ControllerBase
    {
        [HttpGet("health")]
        [HttpHead("health")]
        public IActionResult Get()
        {
            return Ok(new
            {
                service = "OrderService",
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
