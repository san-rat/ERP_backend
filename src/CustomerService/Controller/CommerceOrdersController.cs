using CustomerService.Data;
using CustomerService.DTOs.Orders;
using CustomerService.Helpers;
using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/commerce")]
    public class CommerceOrdersController : CustomerControllerBase
    {
        private readonly CustomerDbContext _dbContext;
        private readonly IOrderProxyService _orderProxyService;

        public CommerceOrdersController(CustomerDbContext dbContext, IOrderProxyService orderProxyService)
        {
            _dbContext = dbContext;
            _orderProxyService = orderProxyService;
        }

        [HttpPost("checkout")]
        [HttpPost("orders/checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            var customerId = GetRequiredCustomerId();
            if (request.AddressId == Guid.Empty || string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                return BadRequest(new { success = false, message = "AddressId and paymentMethod are required." });
            }

            var cart = await _dbContext.CustomerCarts
                .Include(existingCart => existingCart.Items)
                .FirstOrDefaultAsync(existingCart => existingCart.CustomerId == customerId);

            if (cart is null || cart.Items.Count == 0)
            {
                return BadRequest(new { success = false, message = "Cart is empty." });
            }

            var address = await _dbContext.CustomerAddresses
                .FirstOrDefaultAsync(existingAddress => existingAddress.Id == request.AddressId && existingAddress.CustomerId == customerId);

            if (address is null)
            {
                return NotFound(new { success = false, message = "Shipping address not found." });
            }

            var externalOrderId = $"ECOM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
            var createOrderRequest = new CommerceCreateOrderRequestDto
            {
                CustomerId = customerId,
                ExternalOrderId = externalOrderId,
                PaymentMethod = request.PaymentMethod.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                ShippingAddressSnapshot = address.ToShippingAddress(),
                Items = cart.Items
                    .OrderBy(item => item.ProductId)
                    .Select(item => new CommerceOrderLineRequestDto
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    })
                    .ToList()
            };

            var order = await _orderProxyService.CreateOrderAsync(createOrderRequest);
            await UpsertOrderReferenceAsync(customerId, order);

            _dbContext.CustomerCartItems.RemoveRange(cart.Items);
            _dbContext.CustomerCarts.Remove(cart);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Checkout completed successfully.",
                data = order
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var customerId = GetRequiredCustomerId();
            var orders = await _orderProxyService.GetOrdersByCustomerAsync(customerId);
            foreach (var order in orders)
            {
                await UpsertOrderReferenceAsync(customerId, order);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = orders.OrderByDescending(order => order.CreatedAt).ToList()
            });
        }

        [HttpGet("orders/{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var customerId = GetRequiredCustomerId();
            var order = await _orderProxyService.GetOrderByIdAsync(id);
            if (order is null || order.CustomerId != customerId)
            {
                var localReference = await _dbContext.CustomerOrderReferences
                    .Include(reference => reference.Items)
                    .FirstOrDefaultAsync(reference => reference.CustomerId == customerId && reference.ErpOrderId == id);

                if (localReference is null)
                {
                    return NotFound(new { success = false, message = "Order not found." });
                }

                return Ok(new
                {
                    success = true,
                    data = localReference.ToOrderResponse()
                });
            }

            await UpsertOrderReferenceAsync(customerId, order);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = order
            });
        }

        [HttpPost("orders/{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelCustomerOrderRequestDto? request)
        {
            var customerId = GetRequiredCustomerId();
            var order = await _orderProxyService.GetOrderByIdAsync(id);
            if (order is null || order.CustomerId != customerId)
            {
                return NotFound(new { success = false, message = "Order not found." });
            }

            var cancelledOrder = await _orderProxyService.CancelOrderAsync(id, request?.Reason);
            await UpsertOrderReferenceAsync(customerId, cancelledOrder);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Order cancelled successfully.",
                data = cancelledOrder
            });
        }

        private async Task UpsertOrderReferenceAsync(Guid customerId, CommerceOrderResponseDto order)
        {
            var reference = await _dbContext.CustomerOrderReferences
                .Include(existingReference => existingReference.Items)
                .FirstOrDefaultAsync(existingReference => existingReference.CustomerId == customerId && existingReference.ErpOrderId == order.Id);

            if (reference is null)
            {
                reference = new CustomerOrderReference
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    ErpOrderId = order.Id,
                    CreatedAt = order.CreatedAt
                };
                _dbContext.CustomerOrderReferences.Add(reference);
            }

            reference.ExternalOrderId = order.ExternalOrderId;
            reference.TotalAmount = order.TotalAmount;
            reference.Currency = order.Currency;
            reference.PaymentMethod = order.PaymentMethod;
            reference.Status = order.Status;
            reference.ShippingFullName = order.ShippingAddress.FullName;
            reference.ShippingPhone = order.ShippingAddress.Phone;
            reference.ShippingAddressLine1 = order.ShippingAddress.AddressLine1;
            reference.ShippingAddressLine2 = order.ShippingAddress.AddressLine2;
            reference.ShippingCity = order.ShippingAddress.City;
            reference.ShippingState = order.ShippingAddress.State;
            reference.ShippingPostalCode = order.ShippingAddress.PostalCode;
            reference.ShippingCountry = order.ShippingAddress.Country;
            reference.Notes = order.Notes;
            reference.UpdatedAt = order.UpdatedAt == default ? DateTime.UtcNow : order.UpdatedAt;

            if (reference.Items.Count > 0)
            {
                _dbContext.CustomerOrderReferenceItems.RemoveRange(reference.Items);
            }

            reference.Items = order.Items
                .Select(item => new CustomerOrderReferenceItem
                {
                    Id = Guid.NewGuid(),
                    CustomerOrderReferenceId = reference.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList();
        }
    }
}
