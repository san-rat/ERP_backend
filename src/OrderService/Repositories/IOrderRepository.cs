using OrderService.Models;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetByExternalOrderIdAsync(string externalOrderId);
        Task<List<Order>> GetAllAsync();
        Task UpdateAsync(Order order);
    }
}