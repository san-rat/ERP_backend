using CustomerService.Data;
using CustomerService.DTOs.Auth;
using CustomerService.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/commerce/account")]
    public class AccountController : CustomerControllerBase
    {
        private readonly CustomerDbContext _dbContext;

        public AccountController(CustomerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccount()
        {
            var customerId = GetRequiredCustomerId();
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer is null)
            {
                return NotFound(new { success = false, message = "Customer account not found." });
            }

            return Ok(new
            {
                success = true,
                data = customer.ToAccountResponse()
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequestDto request)
        {
            var customerId = GetRequiredCustomerId();
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer is null)
            {
                return NotFound(new { success = false, message = "Customer account not found." });
            }

            if (!CommerceMappings.TryResolveNameParts(request.FirstName, request.LastName, request.FullName, out var firstName, out var lastName))
            {
                return BadRequest(new { success = false, message = "First name and last name are required." });
            }

            customer.FirstName = firstName;
            customer.LastName = lastName;
            customer.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            customer.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Account updated successfully.",
                data = customer.ToAccountResponse()
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            var customerId = GetRequiredCustomerId();
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer is null)
            {
                return NotFound(new { success = false, message = "Customer account not found." });
            }

            var addresses = await _dbContext.CustomerAddresses.Where(address => address.CustomerId == customerId).ToListAsync();
            if (addresses.Count > 0)
            {
                _dbContext.CustomerAddresses.RemoveRange(addresses);
            }

            var cart = await _dbContext.CustomerCarts
                .Include(existingCart => existingCart.Items)
                .FirstOrDefaultAsync(existingCart => existingCart.CustomerId == customerId);

            if (cart is not null)
            {
                if (cart.Items.Count > 0)
                {
                    _dbContext.CustomerCartItems.RemoveRange(cart.Items);
                }

                _dbContext.CustomerCarts.Remove(cart);
            }

            customer.Email = $"deleted+{customer.Id:N}@customers.local";
            customer.FirstName = "Deleted";
            customer.LastName = "Account";
            customer.Phone = null;
            customer.PasswordHash = null;
            customer.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
