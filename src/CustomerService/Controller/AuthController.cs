using CustomerService.Data;
using CustomerService.DTOs.Auth;
using CustomerService.Helpers;
using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Controller
{
    [ApiController]
    [Route("api/commerce/auth")]
    public class AuthController : CustomerControllerBase
    {
        private readonly CustomerDbContext _dbContext;
        private readonly IJwtService _jwtService;

        public AuthController(CustomerDbContext dbContext, IJwtService jwtService)
        {
            _dbContext = dbContext;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email and password are required."
                });
            }

            if (!CommerceMappings.TryResolveNameParts(request.FirstName, request.LastName, request.FullName, out var firstName, out var lastName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "First name and last name are required."
                });
            }

            var normalizedEmail = request.Email.Trim().ToLower();

            var existingCustomer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

            if (existingCustomer != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email already exists"
                });
            }

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            var token = _jwtService.GenerateToken(customer);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                User = customer.ToAccountResponse()
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email and password are required"
                });
            }

            var normalizedEmail = request.Email.Trim().ToLower();

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

            if (customer == null || string.IsNullOrWhiteSpace(customer.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid credentials"
                });
            }

            var token = _jwtService.GenerateToken(customer);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = customer.ToAccountResponse()
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var customerId = GetRequiredCustomerId();

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Customer not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Profile fetched successfully.",
                data = customer.ToAccountResponse()
            });
        }
    }
}
