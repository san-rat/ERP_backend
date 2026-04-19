using CustomerService.Common.Exceptions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controller
{
    public abstract class CustomerControllerBase : ControllerBase
    {
        protected Guid GetRequiredCustomerId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claimValue, out var customerId))
            {
                throw new HttpResponseException(StatusCodes.Status401Unauthorized, "Invalid customer token.");
            }

            return customerId;
        }
    }
}
