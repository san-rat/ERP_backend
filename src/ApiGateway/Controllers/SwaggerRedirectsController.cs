using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("")]
public class SwaggerRedirectsController : ControllerBase
{
    private static readonly HashSet<string> SupportedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "auth",
        "customer",
        "order",
        "product",
        "forecast",
        "prediction",
        "analytics",
        "admin"
    };

    [AllowAnonymous]
    [HttpGet("{service}/swagger")]
    public IActionResult RedirectToServiceSwagger(string service)
    {
        if (!SupportedServices.Contains(service))
        {
            return NotFound();
        }

        return LocalRedirect($"/{service}/swagger/index.html");
    }
}
