using CustomerService.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace CustomerService.Common.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                if (ex is HttpResponseException httpResponseException)
                {
                    context.Response.StatusCode = httpResponseException.StatusCode;

                    if (!string.IsNullOrWhiteSpace(httpResponseException.ResponseBody))
                    {
                        await context.Response.WriteAsync(httpResponseException.ResponseBody);
                        return;
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                var result = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = ex.Message
                });

                await context.Response.WriteAsync(result);
            }
        }
    }
}
