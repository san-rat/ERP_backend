using System;

namespace ProductService.DTOs
{
    /// <summary>
    /// Stock information for a single product ΓÇö used by the "view current stock" endpoint.
    /// </summary>
    public class StockResponseDto
    {
        /// <summary>Product identifier.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Stock Keeping Unit of the product.</summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>Display name of the product.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Compatibility alias for legacy frontend consumers.</summary>
        public string ProductName => Name;

        /// <summary>Number of units currently available for ordering.</summary>
        public int QuantityAvailable { get; set; }

        /// <summary>Number of units currently reserved (e.g. pending orders).</summary>
        public int QuantityReserved { get; set; }

        /// <summary>Total physical stock = QuantityAvailable + QuantityReserved.</summary>
        public int TotalStock => QuantityAvailable + QuantityReserved;

        /// <summary>Threshold below which the product is considered low-stock.</summary>
        public int LowStockThreshold { get; set; }

        /// <summary>True when QuantityAvailable is at or below the LowStockThreshold.</summary>
        public bool IsLowStock { get; set; }
    }
}
