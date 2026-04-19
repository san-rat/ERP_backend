using CustomerService.Data;
using CustomerService.DTOs.Cart;
using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/commerce/cart")]
    public class CartController : CustomerControllerBase
    {
        private readonly CustomerDbContext _dbContext;
        private readonly IProductProxyService _productProxyService;

        public CartController(CustomerDbContext dbContext, IProductProxyService productProxyService)
        {
            _dbContext = dbContext;
            _productProxyService = productProxyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var customerId = GetRequiredCustomerId();
            var cart = await LoadCartAsync(customerId);
            return Ok(new
            {
                success = true,
                data = await BuildCartResponseAsync(cart)
            });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequestDto request)
        {
            if (request.ProductId == Guid.Empty || request.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId and quantity are required." });
            }

            var product = await _productProxyService.GetProductByIdAsync(request.ProductId);
            if (product is null || !product.IsActive)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            var customerId = GetRequiredCustomerId();
            var cart = await GetOrCreateCartAsync(customerId);
            var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == request.ProductId);

            if (existingItem is null)
            {
                cart.Items.Add(new CustomerCartItem
                {
                    Id = Guid.NewGuid(),
                    CustomerCartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cart updated successfully.",
                data = await BuildCartResponseAsync(cart)
            });
        }

        [HttpPut("items/{itemId:guid}")]
        public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequestDto request)
        {
            if (request.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = "Quantity must be greater than zero." });
            }

            var customerId = GetRequiredCustomerId();
            var cart = await LoadCartAsync(customerId);
            if (cart is null)
            {
                return NotFound(new { success = false, message = "Cart not found." });
            }

            var item = cart.Items.FirstOrDefault(existingItem => existingItem.Id == itemId);
            if (item is null)
            {
                return NotFound(new { success = false, message = "Cart item not found." });
            }

            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
            cart.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cart item updated successfully.",
                data = await BuildCartResponseAsync(cart)
            });
        }

        [HttpDelete("items/{itemId:guid}")]
        public async Task<IActionResult> DeleteItem(Guid itemId)
        {
            var customerId = GetRequiredCustomerId();
            var cart = await LoadCartAsync(customerId);
            if (cart is null)
            {
                return NotFound(new { success = false, message = "Cart not found." });
            }

            var item = cart.Items.FirstOrDefault(existingItem => existingItem.Id == itemId);
            if (item is null)
            {
                return NotFound(new { success = false, message = "Cart item not found." });
            }

            _dbContext.CustomerCartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = GetRequiredCustomerId();
            var cart = await LoadCartAsync(customerId);
            if (cart is null)
            {
                return NoContent();
            }

            if (cart.Items.Count > 0)
            {
                _dbContext.CustomerCartItems.RemoveRange(cart.Items);
            }

            _dbContext.CustomerCarts.Remove(cart);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }

        private async Task<CustomerCart?> LoadCartAsync(Guid customerId)
        {
            return await _dbContext.CustomerCarts
                .Include(cart => cart.Items)
                .FirstOrDefaultAsync(cart => cart.CustomerId == customerId);
        }

        private async Task<CustomerCart> GetOrCreateCartAsync(Guid customerId)
        {
            var cart = await LoadCartAsync(customerId);
            if (cart is not null)
            {
                return cart;
            }

            cart = new CustomerCart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.CustomerCarts.Add(cart);
            await _dbContext.SaveChangesAsync();

            return await LoadCartAsync(customerId) ?? cart;
        }

        private async Task<CartResponseDto> BuildCartResponseAsync(CustomerCart? cart)
        {
            if (cart is null)
            {
                return new CartResponseDto();
            }

            var products = await _productProxyService.ResolveProductsAsync(cart.Items.Select(item => item.ProductId));
            var items = cart.Items
                .OrderByDescending(item => item.UpdatedAt)
                .Select(item =>
                {
                    products.TryGetValue(item.ProductId, out var product);
                    var productAvailable = product is not null && product.IsActive && product.QuantityAvailable >= item.Quantity;
                    var availabilityMessage = product switch
                    {
                        null => "Product is no longer available.",
                        { IsActive: false } => "Product is no longer active.",
                        _ when product.QuantityAvailable < item.Quantity => $"Only {product.QuantityAvailable} item(s) available.",
                        _ => null
                    };

                    return new CartItemResponseDto
                    {
                        ItemId = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Product = product,
                        ProductAvailable = productAvailable,
                        AvailabilityMessage = availabilityMessage,
                        LineTotal = product is null ? null : product.Price * item.Quantity
                    };
                })
                .ToList();

            return new CartResponseDto
            {
                CartId = cart.Id,
                TotalItems = items.Sum(item => item.Quantity),
                EstimatedTotal = items.Sum(item => item.LineTotal ?? 0m),
                UpdatedAt = cart.UpdatedAt,
                Items = items
            };
        }
    }
}
