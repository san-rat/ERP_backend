using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("")]
public class SwaggerRedirectsController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("apigateway/swagger")]
    [HttpGet("apigateway/swagger/index.html")]
    public IActionResult RedirectToGatewaySwagger() => LocalRedirect("/swagger/index.html");

    [AllowAnonymous]
    [HttpGet("apigateway/swagger/v1/swagger.json")]
    public IActionResult RedirectToGatewaySwaggerJson() => LocalRedirect("/swagger/v1/swagger.json");
}
