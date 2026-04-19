using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<EcommerceOrderResponseDto> CreateEcommerceOrderAsync(EcommerceCreateOrderDto dto);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto> GetOrderByIdAsync(Guid id);
        Task<IReadOnlyList<EcommerceOrderResponseDto>> GetOrdersByCustomerAsync(Guid customerId);
        Task<EcommerceOrderResponseDto> GetEcommerceOrderByIdAsync(Guid id);
        Task<EcommerceOrderResponseDto> CancelEcommerceOrderAsync(Guid id, string? reason);
        Task<OrderResponseDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto);
        Task<object> GetOrderReportAsync();
    }
}
