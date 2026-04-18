using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Interfaces;

namespace ProductService.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/internal/products")]
    public class InternalProductsController : ControllerBase
    {
        private readonly IProductManager _productManager;
        private readonly IConfiguration _configuration;

        public InternalProductsController(IProductManager productManager, IConfiguration configuration)
        {
            _productManager = productManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetCatalogProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? name = null)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "Invalid pagination parameters." });
            }

            var products = await _productManager.GetCatalogProductsAsync(pageNumber, pageSize, category, categoryId, name);
            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            var product = await _productManager.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product {id} not found." });
            }

            return Ok(product);
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> ResolveProducts([FromBody] ResolveProductsRequestDto dto)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var products = await _productManager.ResolveProductsAsync(dto.ProductIds);
            return Ok(products);
        }

        [HttpPost("deduct-stock")]
        public async Task<IActionResult> DeductStock([FromBody] DeductStockDto dto)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message) = await _productManager.DeductStockAsync(dto);
            if (!success)
            {
                return Conflict(new { message });
            }

            return Ok(new { message });
        }

        [HttpPost("release-stock")]
        public async Task<IActionResult> ReleaseStock([FromBody] ReleaseStockDto dto)
        {
            if (TryRejectUnauthorized(out var unauthorizedResult))
            {
                return unauthorizedResult;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, message) = await _productManager.ReleaseStockAsync(dto);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
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
