using CustomerService.DTOs;
using CustomerService.DTOs.Products;

namespace CustomerService.Services.Interfaces
{
    public interface IProductProxyService
    {
        Task<PaginatedResponse<CommerceProductDto>> GetProductsAsync(int pageNumber, int pageSize, string? category, int? categoryId, string? name);
        Task<CommerceProductDto?> GetProductByIdAsync(Guid id);
        Task<IReadOnlyDictionary<Guid, CommerceProductDto>> ResolveProductsAsync(IEnumerable<Guid> productIds);
    }
}
