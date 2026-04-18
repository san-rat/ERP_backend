using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controller
{
    [ApiController]
    [Route("api/commerce/products")]
    public class CommerceProductsController : ControllerBase
    {
        private readonly IProductProxyService _productProxyService;

        public CommerceProductsController(IProductProxyService productProxyService)
        {
            _productProxyService = productProxyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? name = null)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid pagination parameters." });
            }

            var result = await _productProxyService.GetProductsAsync(pageNumber, pageSize, category, categoryId, name);
            result.Data = result.Data.Where(product => product.IsActive).ToList();

            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var result = await _productProxyService.GetProductByIdAsync(id);
            if (result is null || !result.IsActive)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            return Ok(new
            {
                success = true,
                data = result
            });
        }
    }
}
