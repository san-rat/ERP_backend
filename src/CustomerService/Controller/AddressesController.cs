using CustomerService.Data;
using CustomerService.DTOs.Addresses;
using CustomerService.Helpers;
using CustomerService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/commerce/addresses")]
    public class AddressesController : CustomerControllerBase
    {
        private readonly CustomerDbContext _dbContext;

        public AddressesController(CustomerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var customerId = GetRequiredCustomerId();
            var addresses = await _dbContext.CustomerAddresses
                .Where(address => address.CustomerId == customerId)
                .OrderByDescending(address => address.IsDefault)
                .ThenByDescending(address => address.UpdatedAt)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = addresses.Select(address => address.ToAddressResponse()).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] UpsertAddressRequestDto request)
        {
            var customerId = GetRequiredCustomerId();
            if (!IsValidAddress(request, out var message))
            {
                return BadRequest(new { success = false, message });
            }

            var existingAddresses = await _dbContext.CustomerAddresses
                .Where(address => address.CustomerId == customerId)
                .ToListAsync();

            var shouldBeDefault = request.IsDefault || existingAddresses.Count == 0;
            if (shouldBeDefault)
            {
                foreach (var existingAddress in existingAddresses.Where(address => address.IsDefault))
                {
                    existingAddress.IsDefault = false;
                    existingAddress.UpdatedAt = DateTime.UtcNow;
                }
            }

            var address = new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                FullName = request.FullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                AddressLine1 = request.AddressLine1.Trim(),
                AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim(),
                City = request.City.Trim(),
                State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
                Country = request.Country.Trim(),
                IsDefault = shouldBeDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.CustomerAddresses.Add(address);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddresses), new { id = address.Id }, new
            {
                success = true,
                message = "Address created successfully.",
                data = address.ToAddressResponse()
            });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpsertAddressRequestDto request)
        {
            var customerId = GetRequiredCustomerId();
            if (!IsValidAddress(request, out var message))
            {
                return BadRequest(new { success = false, message });
            }

            var address = await _dbContext.CustomerAddresses
                .FirstOrDefaultAsync(existingAddress => existingAddress.Id == id && existingAddress.CustomerId == customerId);

            if (address is null)
            {
                return NotFound(new { success = false, message = "Address not found." });
            }

            if (request.IsDefault)
            {
                var existingAddresses = await _dbContext.CustomerAddresses
                    .Where(existingAddress => existingAddress.CustomerId == customerId && existingAddress.Id != id && existingAddress.IsDefault)
                    .ToListAsync();

                foreach (var existingAddress in existingAddresses)
                {
                    existingAddress.IsDefault = false;
                    existingAddress.UpdatedAt = DateTime.UtcNow;
                }
            }

            address.FullName = request.FullName.Trim();
            address.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            address.AddressLine1 = request.AddressLine1.Trim();
            address.AddressLine2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2.Trim();
            address.City = request.City.Trim();
            address.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim();
            address.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
            address.Country = request.Country.Trim();
            address.IsDefault = request.IsDefault || !await _dbContext.CustomerAddresses.AnyAsync(existingAddress => existingAddress.CustomerId == customerId && existingAddress.Id != id && existingAddress.IsDefault);
            address.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Address updated successfully.",
                data = address.ToAddressResponse()
            });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var customerId = GetRequiredCustomerId();
            var address = await _dbContext.CustomerAddresses
                .FirstOrDefaultAsync(existingAddress => existingAddress.Id == id && existingAddress.CustomerId == customerId);

            if (address is null)
            {
                return NotFound(new { success = false, message = "Address not found." });
            }

            var wasDefault = address.IsDefault;
            _dbContext.CustomerAddresses.Remove(address);
            await _dbContext.SaveChangesAsync();

            if (wasDefault)
            {
                var nextAddress = await _dbContext.CustomerAddresses
                    .Where(existingAddress => existingAddress.CustomerId == customerId)
                    .OrderByDescending(existingAddress => existingAddress.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (nextAddress is not null)
                {
                    nextAddress.IsDefault = true;
                    nextAddress.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        private static bool IsValidAddress(UpsertAddressRequestDto request, out string message)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.AddressLine1) ||
                string.IsNullOrWhiteSpace(request.City) ||
                string.IsNullOrWhiteSpace(request.Country))
            {
                message = "Full name, address line 1, city, and country are required.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
