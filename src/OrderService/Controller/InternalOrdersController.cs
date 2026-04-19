using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controller
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/internal/orders")]
    public class InternalOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;

        public InternalOrdersController(IOrderService orderService, IConfiguration configuration)
        {
            _orderService = orderService;
            _configuration = configuration;
        }

        [HttpPost("ecommerce")]
        public async Task<IActionResult> CreateEcommerceOrder([FromBody] EcommerceCreateOrderDto dto)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = await _orderService.CreateEcommerceOrderAsync(dto);
            return Ok(order);
        }

        [HttpGet("by-customer/{customerId:guid}")]
        public async Task<IActionResult> GetOrdersByCustomer(Guid customerId)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            var orders = await _orderService.GetOrdersByCustomerAsync(customerId);
            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            var order = await _orderService.GetEcommerceOrderByIdAsync(id);
            return Ok(order);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelEcommerceOrderDto? dto)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            var order = await _orderService.CancelEcommerceOrderAsync(id, dto?.Reason);
            return Ok(order);
        }

        private bool TryRejectUnauthorized(out IActionResult result)
        {
            var configuredKey = _configuration["InternalServiceAuth:ServiceKey"];
            var headerName = _configuration["InternalServiceAuth:HeaderName"] ?? "X-Internal-Service-Key";
            var providedKey = Request.Headers[headerName].ToString();

            if (string.IsNullOrWhiteSpace(configuredKey) || string.IsNullOrWhiteSpace(providedKey))
            {
                result = Unauthorized(new { message = "Missing or invalid internal service credentials." });
                return true;
            }

            if (!FixedTimeEquals(configuredKey, providedKey))
            {
                result = Unauthorized(new { message = "Missing or invalid internal service credentials." });
                return true;
            }

            result = null!;
            return false;
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
