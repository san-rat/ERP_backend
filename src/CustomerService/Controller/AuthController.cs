using CustomerService.Data;
using CustomerService.DTOs.Auth;
using CustomerService.Models;
using CustomerService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CustomerService.Controller
{
    [ApiController]
    [Route("api/commerce/auth")]
    public class AuthController : ControllerBase
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
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Full name, email, and password are required"
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
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            var token = _jwtService.GenerateToken(customer);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                User = new
                {
                    customer.Id,
                    customer.FullName,
                    customer.Email,
                    customer.Phone
                }
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

            if (customer == null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
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
                User = new
                {
                    customer.Id,
                    customer.FullName,
                    customer.Email,
                    customer.Phone
                }
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid token"
                });
            }

            var customer = await _dbContext.Customers
                .Where(c => c.Id == customerId)
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Email,
                    c.Phone,
                    c.CreatedAt
                })
                .FirstOrDefaultAsync();

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
                message = "Profile fetched successfully",
                data = customer
            });
        }
    }
}