using CustomerService.Models;

namespace CustomerService.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Customer customer);
    }
}