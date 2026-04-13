using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controller
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
                success = true,
                message = "Customer Commerce API is running"
            });
        }
    }
}