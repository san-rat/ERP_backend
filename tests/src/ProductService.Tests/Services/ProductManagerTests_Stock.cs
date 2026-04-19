using ProductService.DTOs;
using ProductService.Services;
using ProductService.Tests.Helpers;

namespace ProductService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProductManager"/> — stock deduction and stock query operations.
/// </summary>
public class ProductManagerTests_Stock
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static CreateProductDto MakeCreateDto(
        string sku = "SKU-001",
        string name = "Stock Widget",
        int initialStock = 50,
        int lowStockThreshold = 10) => new()
    {
        Sku               = sku,
        Name              = name,
        Price             = 9.99m,
        IsActive          = true,
        InitialStock      = initialStock,
        LowStockThreshold = lowStockThreshold
    };

    // ─── GetStockAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockAsync_ReturnsOnlyActiveProducts()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);
        var userId  = Guid.NewGuid();

        // Active product
        await manager.CreateProductAsync(MakeCreateDto("SKU-A", "Active Product"), userId);

        // Inactive product – insert directly so we can mark IsActive = false
        var inactiveDto = MakeCreateDto("SKU-B", "Inactive Product");
        var inactive = await manager.CreateProductAsync(inactiveDto, userId);
        var product  = ctx.Products.Single(p => p.Id == inactive.Id);
        product.IsActive = false;
        await ctx.SaveChangesAsync();

        var stocks = (await manager.GetStockAsync()).ToList();

        Assert.Single(stocks);
        Assert.Equal("Active Product", stocks[0].Name);
    }

    [Fact]
    public async Task GetStockAsync_ReturnsCorrectStockData()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 75, lowStockThreshold: 15), Guid.NewGuid());

        var stocks = (await manager.GetStockAsync()).ToList();

        Assert.Single(stocks);
        var stock = stocks[0];
        Assert.Equal(created.Id, stock.ProductId);
        Assert.Equal(75,         stock.QuantityAvailable);
        Assert.Equal(15,         stock.LowStockThreshold);
        Assert.False(stock.IsLowStock);
    }

    // ─── GetStockByProductIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetStockByProductIdAsync_ExistingProduct_ReturnsStock()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 30), Guid.NewGuid());

        var stock = await manager.GetStockByProductIdAsync(created.Id);

        Assert.NotNull(stock);
        Assert.Equal(created.Id, stock.ProductId);
        Assert.Equal(30, stock.QuantityAvailable);
    }

    [Fact]
    public async Task GetStockByProductIdAsync_NonExistentProduct_ReturnsNull()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var stock = await manager.GetStockByProductIdAsync(Guid.NewGuid());

        Assert.Null(stock);
    }

    // ─── DeductStockAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeductStockAsync_SufficientStock_ReturnsTrueAndDeductsQuantity()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 50), Guid.NewGuid());

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 10
        };

        var (success, message) = await manager.DeductStockAsync(dto);

        Assert.True(success);
        Assert.Contains("40", message); // "Remaining: 40"

        var inventory = ctx.Inventory.Single(i => i.ProductId == created.Id);
        Assert.Equal(40, inventory.QuantityAvailable);
    }

    [Fact]
    public async Task DeductStockAsync_InsufficientStock_ReturnsFalseWithMessage()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 5), Guid.NewGuid());

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 20  // more than available
        };

        var (success, message) = await manager.DeductStockAsync(dto);

        Assert.False(success);
        Assert.Contains("Insufficient", message);
    }

    [Fact]
    public async Task DeductStockAsync_NoInventoryRecord_ReturnsFalse()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var dto = new DeductStockDto
        {
            ProductId = Guid.NewGuid(),
            OrderId   = Guid.NewGuid(),
            Quantity  = 1
        };

        var (success, _) = await manager.DeductStockAsync(dto);

        Assert.False(success);
    }

    [Fact]
    public async Task DeductStockAsync_CreatesInventoryReservationRecord()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 50), Guid.NewGuid());
        var orderId = Guid.NewGuid();

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = orderId,
            Quantity  = 5
        };

        await manager.DeductStockAsync(dto);

        var reservation = ctx.InventoryReservations
            .Single(r => r.ProductId == created.Id && r.OrderId == orderId);

        Assert.Equal(5,          reservation.Quantity);
        Assert.Equal("DEDUCTED", reservation.Status);
    }

    [Fact]
    public async Task DeductStockAsync_WhenStockFallsBelowThreshold_CreatesLowStockAlert()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        // initialStock=15, threshold=10 → becomes low at 10 or below
        var created = await manager.CreateProductAsync(
            MakeCreateDto(initialStock: 15, lowStockThreshold: 10), Guid.NewGuid());

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 10  // 15 - 10 = 5, which is ≤ threshold(10)
        };

        await manager.DeductStockAsync(dto);

        var alert = ctx.LowStockAlerts.SingleOrDefault(a => a.ProductId == created.Id && !a.IsResolved);
        Assert.NotNull(alert);
        Assert.Equal(5, alert.QuantityAtAlert);
    }

    [Fact]
    public async Task DeductStockAsync_WhenStockStaysAboveThreshold_DoesNotCreateAlert()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        // initialStock=100, threshold=10 → 100-5=95 is still above threshold
        var created = await manager.CreateProductAsync(
            MakeCreateDto(initialStock: 100, lowStockThreshold: 10), Guid.NewGuid());

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 5
        };

        await manager.DeductStockAsync(dto);

        Assert.Empty(ctx.LowStockAlerts.Where(a => a.ProductId == created.Id));
    }

    [Fact]
    public async Task DeductStockAsync_UpdatesProductQuantityAvailableInProductsTable()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 50), Guid.NewGuid());

        var dto = new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 15
        };

        await manager.DeductStockAsync(dto);

        var product = ctx.Products.Single(p => p.Id == created.Id);
        Assert.Equal(35, product.QuantityAvailable);
    }
}
