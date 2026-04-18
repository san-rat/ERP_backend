using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services;
using ProductService.Tests.Helpers;

namespace ProductService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProductManager"/> — product CRUD operations.
/// Uses the EF Core in-memory provider so no real database is required.
/// </summary>
public class ProductManagerTests_Products
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static CreateProductDto MakeCreateDto(
        string sku = "SKU-001",
        string name = "Test Widget",
        decimal price = 9.99m,
        int initialStock = 50,
        int lowStockThreshold = 10,
        int? categoryId = null) => new()
    {
        Sku              = sku,
        Name             = name,
        Description      = "A unit-test product",
        CategoryId       = categoryId,
        Price            = price,
        IsActive         = true,
        InitialStock     = initialStock,
        LowStockThreshold = lowStockThreshold
    };

    private static UpdateProductDto MakeUpdateDto(
        string sku = "SKU-001-UP",
        string name = "Updated Widget",
        decimal price = 19.99m,
        int qty = 40,
        int threshold = 5) => new()
    {
        Sku               = sku,
        Name              = name,
        Description       = "Updated description",
        CategoryId        = null,
        Price             = price,
        IsActive          = true,
        QuantityAvailable = qty,
        LowStockThreshold = threshold
    };

    // ─── GetProductByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProductByIdAsync_ExistingId_ReturnsMatchingProduct()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        var created = await manager.CreateProductAsync(MakeCreateDto(), userId);

        var result = await manager.GetProductByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Test Widget", result.Name);
        Assert.Equal("SKU-001", result.Sku);
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.GetProductByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── GetProductsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsAsync_NoFilter_ReturnsAllProducts()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        await manager.CreateProductAsync(MakeCreateDto("SKU-A", "Alpha"), userId);
        await manager.CreateProductAsync(MakeCreateDto("SKU-B", "Beta"),  userId);

        var result = await manager.GetProductsAsync(1, 10, null, null, null);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetProductsAsync_FilterByName_ReturnsMatchingProduct()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        await manager.CreateProductAsync(MakeCreateDto("SKU-A", "Alpha Widget"), userId);
        await manager.CreateProductAsync(MakeCreateDto("SKU-B", "Beta Gadget"),  userId);

        var result = await manager.GetProductsAsync(1, 10, null, null, "gadget");

        Assert.Equal(1, result.TotalRecords);
        Assert.Equal("Beta Gadget", result.Data.Single().Name);
    }

    [Fact]
    public async Task GetProductsAsync_Pagination_RespectsPageSizeAndOffset()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        // Insert 5 products (ordered alphabetically by name)
        foreach (var i in Enumerable.Range(1, 5))
            await manager.CreateProductAsync(MakeCreateDto($"SKU-{i:D2}", $"Product {i:D2}"), userId);

        var page1 = await manager.GetProductsAsync(1, 2, null, null, null);
        var page2 = await manager.GetProductsAsync(2, 2, null, null, null);

        Assert.Equal(5, page1.TotalRecords);
        Assert.Equal(2, page1.Data.Count());
        Assert.Equal(2, page2.Data.Count());

        // Ensure page 1 and page 2 contain different products
        var page1Ids = page1.Data.Select(p => p.Id).ToHashSet();
        var page2Ids = page2.Data.Select(p => p.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }

    [Fact]
    public async Task GetProductsAsync_FilterByCategoryId_ReturnsOnlyMatchingProducts()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        var dto1 = MakeCreateDto("SKU-C1", "Cat-1 Product", categoryId: 42);
        var dto2 = MakeCreateDto("SKU-C2", "Cat-2 Product", categoryId: 99);

        await manager.CreateProductAsync(dto1, userId);
        await manager.CreateProductAsync(dto2, userId);

        var result = await manager.GetProductsAsync(1, 10, null, 42, null);

        Assert.Equal(1, result.TotalRecords);
        Assert.Equal("Cat-1 Product", result.Data.Single().Name);
    }

    // ─── CreateProductAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateProductAsync_ValidDto_ReturnsCreatedProduct()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId = Guid.NewGuid();

        var result = await manager.CreateProductAsync(MakeCreateDto(), userId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Widget", result.Name);
        Assert.Equal("SKU-001", result.Sku);
        Assert.Equal(9.99m, result.Price);
        Assert.Equal(50, result.QuantityAvailable);
        Assert.Equal(10, result.LowStockThreshold);
        Assert.Equal(userId, result.CreatedByUserId);
    }

    [Fact]
    public async Task CreateProductAsync_CreatesInventoryRecord()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.CreateProductAsync(MakeCreateDto(initialStock: 100, lowStockThreshold: 20), Guid.NewGuid());

        var inventory = ctx.Inventory.Single(i => i.ProductId == result.Id);
        Assert.Equal(100, inventory.QuantityAvailable);
        Assert.Equal(20, inventory.LowStockThreshold);
        Assert.Equal(0,  inventory.QuantityReserved);
    }

    [Fact]
    public async Task CreateProductAsync_PersistsProductInDatabase()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.CreateProductAsync(MakeCreateDto(), Guid.NewGuid());

        var persisted = ctx.Products.Single(p => p.Id == result.Id);
        Assert.Equal("Test Widget", persisted.Name);
        Assert.Equal("SKU-001", persisted.Sku);
    }

    // ─── UpdateProductAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProductAsync_ExistingId_ReturnsUpdatedProduct()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(), Guid.NewGuid());
        var updateDto = MakeUpdateDto(qty: 30, threshold: 5);

        var result = await manager.UpdateProductAsync(created.Id, updateDto);

        Assert.NotNull(result);
        Assert.Equal("Updated Widget",  result.Name);
        Assert.Equal("SKU-001-UP",      result.Sku);
        Assert.Equal(19.99m,            result.Price);
        Assert.Equal(30,                result.QuantityAvailable);
        Assert.Equal(5,                 result.LowStockThreshold);
    }

    [Fact]
    public async Task UpdateProductAsync_NonExistentId_ReturnsNull()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.UpdateProductAsync(Guid.NewGuid(), MakeUpdateDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatesInventoryRecord()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 50, lowStockThreshold: 10), Guid.NewGuid());

        await manager.UpdateProductAsync(created.Id, MakeUpdateDto(qty: 80, threshold: 15));

        var inventory = ctx.Inventory.Single(i => i.ProductId == created.Id);
        Assert.Equal(80, inventory.QuantityAvailable);
        Assert.Equal(15, inventory.LowStockThreshold);
    }

    // ─── DeleteProductAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteProductAsync_ExistingId_ReturnsTrueAndRemovesProduct()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(), Guid.NewGuid());

        var deleted = await manager.DeleteProductAsync(created.Id);

        Assert.True(deleted);
        Assert.False(ctx.Products.Any(p => p.Id == created.Id));
    }

    [Fact]
    public async Task DeleteProductAsync_NonExistentId_ReturnsFalse()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.DeleteProductAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
