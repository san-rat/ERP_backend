using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto> GetOrderByIdAsync(int id);
        Task<OrderResponseDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<object> GetOrderReportAsync();
    }
}