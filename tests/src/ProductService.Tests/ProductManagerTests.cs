using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Services;
using ProductService.Models;
using Xunit;

namespace ProductService.Tests
{
    public class ProductManagerTests : IDisposable
    {
        private readonly ProductDbContext _context;
        private readonly ProductManager _manager;

        public ProductManagerTests()
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ProductDbContext(options);
            _manager = new ProductManager(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateProductAsync_ValidDto_CreatesProductAndInventory()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Sku = "SKU-TEST-01",
                Name = "Test Product",
                Price = 99.99m,
                InitialStock = 50,
                LowStockThreshold = 10
            };
            var userId = Guid.NewGuid();

            // Act
            var result = await _manager.CreateProductAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SKU-TEST-01", result.Sku);
            Assert.Equal(50, result.QuantityAvailable);
            Assert.Equal(userId, result.CreatedByUserId);

            var dbProduct = await _context.Products.Include(p => p.Inventory).SingleAsync();
            Assert.Equal(50, dbProduct.QuantityAvailable);
            Assert.NotNull(dbProduct.Inventory);
            Assert.Equal(50, dbProduct.Inventory.QuantityAvailable);
            Assert.Equal(10, dbProduct.Inventory.LowStockThreshold);
        }

        [Fact]
        public async Task GetProductsAsync_PaginationAndFiltering_WorksCorrectly()
        {
            // Arrange
            var c = new Category { Name = "Toys" };
            _context.Categories.Add(c);
            _context.Products.Add(new Product { Sku = "1", Name = "Apple", Category = c, Price = 1m });
            _context.Products.Add(new Product { Sku = "2", Name = "Banana", Category = c, Price = 1m });
            _context.Products.Add(new Product { Sku = "3", Name = "Axe", Price = 1m });
            await _context.SaveChangesAsync();

            // Act
            var resultNameFilter = await _manager.GetProductsAsync(1, 10, null, "a");
            var resultCategoryFilter = await _manager.GetProductsAsync(1, 10, "Toys", null);

            // Assert
            Assert.Equal(3, resultNameFilter.TotalRecords);

            Assert.Equal(2, resultCategoryFilter.TotalRecords); // Only Apple, Banana are in "Toys"
        }

        [Fact]
        public async Task DeductStockAsync_InsufficientStock_ReturnsFalse()
        {
            // Arrange
            var pId = Guid.NewGuid();
            _context.Products.Add(new Product { Id = pId, Sku = "1", Name = "P1", Price = 10, QuantityAvailable = 5 });
            _context.Inventory.Add(new Inventory { ProductId = pId, QuantityAvailable = 5 });
            await _context.SaveChangesAsync();

            var dto = new DeductStockDto
            {
                ProductId = pId,
                OrderId = Guid.NewGuid(),
                Quantity = 10
            };

            // Act
            var result = await _manager.DeductStockAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Insufficient stock", result.Message);
        }

        [Fact]
        public async Task DeductStockAsync_SufficientStock_DeductsAndSyncsTables()
        {
            // Arrange
            var pId = Guid.NewGuid();
            _context.Products.Add(new Product { Id = pId, Sku = "1", Name = "P1", Price = 10, QuantityAvailable = 20 });
            _context.Inventory.Add(new Inventory { ProductId = pId, QuantityAvailable = 20 });
            await _context.SaveChangesAsync();

            var dto = new DeductStockDto
            {
                ProductId = pId,
                OrderId = Guid.NewGuid(),
                Quantity = 5
            };

            // Act
            var result = await _manager.DeductStockAsync(dto);

            // Assert
            Assert.True(result.Success);
            
            var dbProduct = await _context.Products.FindAsync(pId);
            var dbInventory = await _context.Inventory.SingleAsync();

            Assert.Equal(15, dbProduct!.QuantityAvailable);
            Assert.Equal(15, dbInventory.QuantityAvailable);
            Assert.Single(_context.InventoryReservations);
        }
    }
}
