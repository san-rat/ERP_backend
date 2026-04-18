using CustomerService.DTOs.Orders;

namespace CustomerService.Services.Interfaces
{
    public interface IOrderProxyService
    {
        Task<CommerceOrderResponseDto> CreateOrderAsync(CommerceCreateOrderRequestDto payload);
        Task<IReadOnlyList<CommerceOrderResponseDto>> GetOrdersByCustomerAsync(Guid customerId);
        Task<CommerceOrderResponseDto?> GetOrderByIdAsync(Guid orderId);
        Task<CommerceOrderResponseDto> CancelOrderAsync(Guid orderId, string? reason);
    }
}
