using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Interfaces;

namespace ProductService.Controllers
{
    /// <summary>
    /// Manages products and inventory stock within the ERP system.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductManager _productManager;

        public ProductsController(IProductManager productManager)
        {
            _productManager = productManager;
        }

        // ────────────────────────────────────────────────────────────────────
        //  GET api/products  — paginated product list with optional filters
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Returns a paginated list of products with optional category / name filters.</summary>
        /// <param name="pageNumber">1-based page number (default 1).</param>
        /// <param name="pageSize">Number of items per page (default 10).</param>
        /// <param name="category">Filter by category name (exact, case-insensitive).</param>
        /// <param name="name">Filter by product name (partial match, case-insensitive).</param>
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(PaginatedResponse<ProductResponseDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize   = 10,
            [FromQuery] string? category = null,
            [FromQuery] string? name     = null)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return BadRequest("Invalid pagination parameters.");

            var result = await _productManager.GetProductsAsync(pageNumber, pageSize, category, name);
            return Ok(result);
        }

        // ────────────────────────────────────────────────────────────────────
        //  GET api/products/{id}
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Returns a single product by its GUID identifier.</summary>
        /// <param name="id">Product GUID.</param>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _productManager.GetProductByIdAsync(id);
            if (product == null) return NotFound(new { message = $"Product {id} not found." });
            return Ok(product);
        }

        // ────────────────────────────────────────────────────────────────────
        //  POST api/products  — CREATE a new product
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Creates a new product and initialises its inventory record.</summary>
        /// <remarks>
        /// Supply an <c>initialStock</c> value to pre-load inventory. A low-stock
        /// threshold can also be set here; defaults to 10.
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(ProductResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _productManager.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProductById), new { id = created.Id }, created);
        }

        // ────────────────────────────────────────────────────────────────────
        //  PUT api/products/{id}  — UPDATE a product
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Updates an existing product's details and stock quantity.</summary>
        /// <param name="id">Product GUID to update.</param>
        /// <param name="dto">Updated product data.</param>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _productManager.UpdateProductAsync(id, dto);
            if (updated == null) return NotFound(new { message = $"Product {id} not found." });
            return Ok(updated);
        }

        // ────────────────────────────────────────────────────────────────────
        //  DELETE api/products/{id}  — DELETE a product
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Permanently deletes a product and its related inventory records.
        /// Only Admin users can delete products.
        /// </summary>
        /// <param name="id">Product GUID to delete.</param>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var deleted = await _productManager.DeleteProductAsync(id);
            if (!deleted) return NotFound(new { message = $"Product {id} not found." });
            return NoContent();
        }

        // ────────────────────────────────────────────────────────────────────
        //  GET api/products/stock  — VIEW current stock for all products
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current stock count for all active products, including
        /// QuantityAvailable, QuantityReserved, TotalStock, and low-stock status.
        /// </summary>
        [HttpGet("stock")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<StockResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetStock()
        {
            var stocks = await _productManager.GetStockAsync();
            return Ok(stocks);
        }

        // ────────────────────────────────────────────────────────────────────
        //  GET api/products/{id}/stock  — VIEW stock for a single product
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Returns the current stock count for a specific product.</summary>
        /// <param name="id">Product GUID.</param>
        [HttpGet("{id:guid}/stock")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(typeof(StockResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStockByProductId(Guid id)
        {
            var stock = await _productManager.GetStockByProductIdAsync(id);
            if (stock == null) return NotFound(new { message = $"Product {id} not found." });
            return Ok(stock);
        }

        // ────────────────────────────────────────────────────────────────────
        //  POST api/products/deduct-stock  — DEDUCT stock on order placement
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Deducts stock from a product when an order is placed.
        /// Creates an InventoryReservation record and raises a low-stock alert if
        /// the remaining quantity falls at or below the configured threshold.
        /// Returns 409 Conflict when there is insufficient stock.
        /// </summary>
        [HttpPost("deduct-stock")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> DeductStock([FromBody] DeductStockDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, message) = await _productManager.DeductStockAsync(dto);

            if (!success)
                return Conflict(new { message });

            return Ok(new { message });
        }
    }
}
