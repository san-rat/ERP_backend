using OrderService.Models;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order);
        Task<Order> AddWithItemsAsync(Order order, IEnumerable<OrderItem> items);
        Task<Order?> GetByIdAsync(Guid id);
        Task<Order?> GetByIdWithItemsAsync(Guid id);
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetByCustomerIdAsync(Guid customerId);
        Task UpdateAsync(Order order);
        Task DeleteAsync(Order order);
    }
}
