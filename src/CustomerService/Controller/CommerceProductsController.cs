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
        public async Task<IActionResult> GetProducts()
        {
            var result = await _productProxyService.GetProductsAsync();
            return Content(result, "application/json");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            var result = await _productProxyService.GetProductByIdAsync(id);
            return Content(result, "application/json");
        }
    }
}