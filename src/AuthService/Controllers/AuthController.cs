using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "admin" && request.Password == "password")
        {
            return Ok(new
            {
                token = "fake-jwt-token",
                message = "Login successful"
            });
        }

        return Unauthorized();
    }
}

public record LoginRequest(string Username, string Password);