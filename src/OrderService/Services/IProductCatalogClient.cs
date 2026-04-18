using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IProductCatalogClient
    {
        Task<IReadOnlyDictionary<Guid, ResolvedProductDto>> ResolveProductsAsync(IEnumerable<Guid> productIds);
        Task DeductStockAsync(Guid orderId, Guid productId, int quantity);
        Task ReleaseStockAsync(Guid orderId, Guid productId, int quantity);
    }

    public class ResolvedProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int QuantityAvailable { get; set; }
    }
}
