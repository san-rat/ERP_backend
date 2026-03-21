using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.DTOs;

namespace ProductService.Interfaces
{
    public interface IProductManager
    {
        // ── Read ──────────────────────────────────────────────────────────────
        Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(int pageNumber, int pageSize, string? categoryName, string? name);
        Task<ProductResponseDto?> GetProductByIdAsync(Guid id);

        // ── Write ─────────────────────────────────────────────────────────────
        Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);
        Task<ProductResponseDto?> UpdateProductAsync(Guid id, UpdateProductDto dto);
        Task<bool> DeleteProductAsync(Guid id);

        // ── Inventory ─────────────────────────────────────────────────────────
        /// <summary>Returns stock information for all active products (with QuantityAvailable count).</summary>
        Task<IEnumerable<StockResponseDto>> GetStockAsync();

        /// <summary>Returns stock information for a single product.</summary>
        Task<StockResponseDto?> GetStockByProductIdAsync(Guid productId);

        /// <summary>
        /// Deducts <paramref name="quantity"/> units from a product's available stock when
        /// an order is placed. Creates an InventoryReservation record and fires a low-stock
        /// alert if the threshold is crossed. Returns false when stock is insufficient.
        /// </summary>
        Task<(bool Success, string Message)> DeductStockAsync(DeductStockDto dto);
    }
}
