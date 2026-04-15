using System;

namespace ProductService.DTOs
{
    public class LowStockAlertDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int QuantityAtAlert { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsResolved { get; set; }
        public DateTime AlertedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
