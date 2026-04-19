namespace CustomerService.DTOs.Products
{
    public class CommerceProductDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int QuantityAvailable { get; set; }
        public int QuantityReserved { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class ResolveProductsRequestDto
    {
        public IReadOnlyList<Guid> ProductIds { get; set; } = Array.Empty<Guid>();
    }
}
