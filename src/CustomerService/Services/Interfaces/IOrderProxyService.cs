namespace CustomerService.Services.Interfaces
{
    public interface IOrderProxyService
    {
        Task<string> CreateOrderAsync(object payload);
    }
}