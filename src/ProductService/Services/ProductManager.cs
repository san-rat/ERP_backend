using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Interfaces;
using ProductService.Models;

namespace ProductService.Services
{
    public class ProductManager : IProductManager
    {
        private readonly ProductDbContext _context;

        public ProductManager(ProductDbContext context)
        {
            _context = context;
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  READ
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        public async Task<PaginatedResponse<ProductResponseDto>> GetProductsAsync(
            int pageNumber, int pageSize, string? categoryName, int? categoryId, string? name)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryName))
                query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == categoryName.ToLower());

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => p.Name.ToLower().Contains(name.ToLower()));

            var totalRecords = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToProductResponse(p))
                .ToListAsync();

            return new PaginatedResponse<ProductResponseDto>
            {
                Data = products,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(Guid id)
        {
            var p = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == id);

            return p == null ? null : MapToProductResponse(p);
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  CREATE
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, Guid createdByUserId)
        {
            var product = new Product
            {
                Sku             = dto.Sku,
                Name            = dto.Name,
                Description     = dto.Description,
                CategoryId      = dto.CategoryId,
                Price           = dto.Price,
                IsActive        = dto.IsActive,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
                QuantityAvailable = dto.InitialStock
            };

            _context.Products.Add(product);

            // Create the corresponding Inventory record in the same transaction
            var inventory = new Inventory
            {
                ProductId          = product.Id,
                QuantityAvailable  = dto.InitialStock,
                QuantityReserved   = 0,
                LowStockThreshold  = dto.LowStockThreshold,
                UpdatedAt          = DateTime.UtcNow
            };

            _context.Inventory.Add(inventory);
            await _context.SaveChangesAsync();

            // Load category for the response
            if (product.CategoryId.HasValue)
                await _context.Entry(product).Reference(x => x.Category).LoadAsync();

            product.Inventory = inventory;

            return MapToProductResponse(product);
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  UPDATE
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        public async Task<ProductResponseDto?> UpdateProductAsync(Guid id, UpdateProductDto dto)
        {
            var p = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return null;

            p.Sku         = dto.Sku;
            p.Name        = dto.Name;
            p.Description = dto.Description;
            p.CategoryId  = dto.CategoryId;
            p.Price       = dto.Price;
            p.IsActive    = dto.IsActive;
            p.UpdatedAt   = DateTime.UtcNow;
            p.QuantityAvailable = dto.QuantityAvailable;

            if (p.Inventory != null)
            {
                p.Inventory.QuantityAvailable = dto.QuantityAvailable;
                p.Inventory.LowStockThreshold = dto.LowStockThreshold;
                p.Inventory.UpdatedAt         = DateTime.UtcNow;
            }
            else
            {
                p.Inventory = new Inventory
                {
                    ProductId         = id,
                    QuantityAvailable = dto.QuantityAvailable,
                    LowStockThreshold = dto.LowStockThreshold
                };
            }

            // Auto-resolve open alerts when stock is back above threshold; raise one if still low
            if (p.Inventory.IsLowStock)
            {
                await EnsureLowStockAlertAsync(id, p.Inventory.QuantityAvailable);
            }
            else
            {
                await AutoResolveAlertIfRestockedAsync(id);
            }

            await _context.SaveChangesAsync();

            if (p.CategoryId.HasValue)
                await _context.Entry(p).Reference(x => x.Category).LoadAsync();

            return MapToProductResponse(p);
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  DELETE
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  INVENTORY / STOCK
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        public async Task<IEnumerable<StockResponseDto>> GetStockAsync()
        {
            var stocks = await _context.Products
                .Include(p => p.Inventory)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => MapToStockResponse(p))
                .ToListAsync();

            return stocks;
        }

        public async Task<StockResponseDto?> GetStockByProductIdAsync(Guid productId)
        {
            var p = await _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == productId);

            return p == null ? null : MapToStockResponse(p);
        }

        public async Task<(bool Success, string Message)> DeductStockAsync(DeductStockDto dto)
        {
            // Load product with inventory in a single round-trip
            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);

            if (inventory == null)
                return (false, $"No inventory record found for product {dto.ProductId}.");

            if (inventory.QuantityAvailable < dto.Quantity)
                return (false, $"Insufficient stock. Available: {inventory.QuantityAvailable}, Requested: {dto.Quantity}.");

            // Deduct stock in Inventory
            inventory.QuantityAvailable -= dto.Quantity;
            inventory.UpdatedAt          = DateTime.UtcNow;
            
            // Deduct stock in Product table natively
            var productForReduction = await _context.Products.FindAsync(dto.ProductId);
            if (productForReduction != null)
                productForReduction.QuantityAvailable = inventory.QuantityAvailable;

            // Record the reservation / deduction
            var reservation = new InventoryReservation
            {
                ProductId  = dto.ProductId,
                OrderId    = dto.OrderId,
                Quantity   = dto.Quantity,
                Status     = "DEDUCTED",
                ReservedAt = DateTime.UtcNow
            };
            _context.InventoryReservations.Add(reservation);

            // Fire low-stock alert if the threshold is now breached
            if (inventory.IsLowStock)
                await EnsureLowStockAlertAsync(dto.ProductId, inventory.QuantityAvailable);

            await _context.SaveChangesAsync();

            return (true, $"Stock deducted successfully. Remaining: {inventory.QuantityAvailable}.");
        }

        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ
        //  PRIVATE HELPERS
        // ΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇΓöÇ

        private async Task EnsureLowStockAlertAsync(Guid productId, int currentQty)
        {
            var existing = await _context.LowStockAlerts
                .FirstOrDefaultAsync(a => a.ProductId == productId && !a.IsResolved);

            if (existing == null)
            {
                _context.LowStockAlerts.Add(new LowStockAlert
                {
                    ProductId       = productId,
                    QuantityAtAlert = currentQty
                });
            }
        }

        /// <summary>
        /// Automatically resolves any open low-stock alert for a product when its
        /// stock has been replenished above the configured threshold.
        /// </summary>
        private async Task AutoResolveAlertIfRestockedAsync(Guid productId)
        {
            var openAlert = await _context.LowStockAlerts
                .FirstOrDefaultAsync(a => a.ProductId == productId && !a.IsResolved);

            if (openAlert != null)
            {
                openAlert.IsResolved = true;
                openAlert.ResolvedAt = DateTime.UtcNow;
            }
        }

        public async Task<IEnumerable<LowStockAlertDto>> GetLowStockAlertsAsync(bool unresolvedOnly = false)
        {
            var query = _context.LowStockAlerts
                .Include(a => a.Product)
                .AsQueryable();

            if (unresolvedOnly)
                query = query.Where(a => !a.IsResolved);

            var alerts = await query
                .OrderByDescending(a => a.AlertedAt)
                .Select(a => new LowStockAlertDto
                {
                    Id               = a.Id,
                    ProductId        = a.ProductId,
                    ProductName      = a.Product != null ? a.Product.Name : "Unknown",
                    Sku              = a.Product != null ? a.Product.Sku  : "N/A",
                    QuantityAtAlert  = a.QuantityAtAlert,
                    LowStockThreshold = a.Product != null && a.Product.Inventory != null
                                        ? a.Product.Inventory.LowStockThreshold : 0,
                    IsResolved       = a.IsResolved,
                    AlertedAt        = a.AlertedAt,
                    ResolvedAt       = a.ResolvedAt
                })
                .ToListAsync();

            return alerts;
        }

        public async Task<bool> ResolveAlertAsync(Guid alertId)
        {
            var alert = await _context.LowStockAlerts.FindAsync(alertId);
            if (alert == null) return false;

            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static ProductResponseDto MapToProductResponse(Product p) => new()
        {
            Id                = p.Id,
            Sku               = p.Sku,
            Name              = p.Name,
            Description       = p.Description,
            CategoryId        = p.CategoryId,
            CategoryName      = p.Category?.Name,
            Price             = p.Price,
            IsActive          = p.IsActive,
            QuantityAvailable = p.QuantityAvailable,
            QuantityReserved  = p.Inventory?.QuantityReserved  ?? 0,
            LowStockThreshold = p.Inventory?.LowStockThreshold ?? 0,
            IsLowStock        = p.Inventory?.IsLowStock        ?? false,
            CreatedByUserId   = p.CreatedByUserId
        };

        private static StockResponseDto MapToStockResponse(Product p) => new()
        {
            ProductId         = p.Id,
            Sku               = p.Sku,
            Name              = p.Name,
            QuantityAvailable = p.Inventory?.QuantityAvailable ?? 0,
            QuantityReserved  = p.Inventory?.QuantityReserved  ?? 0,
            LowStockThreshold = p.Inventory?.LowStockThreshold ?? 0,
            IsLowStock        = p.Inventory?.IsLowStock        ?? false
        };
    }
}
