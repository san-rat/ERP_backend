using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto> GetOrderByIdAsync(Guid id);
        Task<OrderResponseDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto);
        Task<object> GetOrderReportAsync();
    }
}
