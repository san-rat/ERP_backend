using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controller
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
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