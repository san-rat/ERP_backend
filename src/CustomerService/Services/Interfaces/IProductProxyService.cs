namespace CustomerService.Services.Interfaces
{
    public interface IProductProxyService
    {
        Task<string> GetProductsAsync();
        Task<string> GetProductByIdAsync(string id);
    }
}