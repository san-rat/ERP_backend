using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services;
using ProductService.Tests.Helpers;

namespace ProductService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProductManager"/> — low-stock alert management.
/// Covers GetLowStockAlertsAsync and ResolveAlertAsync.
/// </summary>
public class ProductManagerTests_Alerts
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static CreateProductDto MakeCreateDto(
        string sku = "SKU-A",
        string name = "Alert Widget",
        int initialStock = 50,
        int lowStockThreshold = 10) => new()
    {
        Sku               = sku,
        Name              = name,
        Price             = 5.00m,
        IsActive          = true,
        InitialStock      = initialStock,
        LowStockThreshold = lowStockThreshold
    };

    private static async Task<(ProductManager manager, Guid productId, Guid alertId)>
        SetupWithAlertAsync(string dbName)
    {
        var ctx     = DbContextFactory.Create(dbName);
        var manager = new ProductManager(ctx);

        // Create product with stock above threshold
        var created = await manager.CreateProductAsync(MakeCreateDto(initialStock: 15, lowStockThreshold: 10), Guid.NewGuid());

        // Trigger low-stock alert by deducting stock below the threshold
        await manager.DeductStockAsync(new DeductStockDto
        {
            ProductId = created.Id,
            OrderId   = Guid.NewGuid(),
            Quantity  = 10     // 15 - 10 = 5, which is ≤ threshold(10)
        });

        var alertId = ctx.LowStockAlerts
            .Single(a => a.ProductId == created.Id && !a.IsResolved)
            .Id;

        return (manager, created.Id, alertId);
    }

    // ─── GetLowStockAlertsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetLowStockAlertsAsync_NoFilter_ReturnsAllAlerts()
    {
        var dbName = Guid.NewGuid().ToString();
        var (manager, productId, alertId) = await SetupWithAlertAsync(dbName);

        // Also manually resolve one alert so we have both resolved & unresolved
        await manager.ResolveAlertAsync(alertId);

        // Add an un-resolved alert directly for another product to keep totals clear
        var ctx2 = DbContextFactory.Create(dbName);
        var manager2 = new ProductManager(ctx2);

        // the first alert is now resolved; create another product & alert
        var created2 = await manager2.CreateProductAsync(MakeCreateDto("SKU-Z", "Another", 12, 10), Guid.NewGuid());
        await manager2.DeductStockAsync(new DeductStockDto { ProductId = created2.Id, OrderId = Guid.NewGuid(), Quantity = 10 });

        var alerts = (await manager2.GetLowStockAlertsAsync(unresolvedOnly: false)).ToList();

        Assert.Equal(2, alerts.Count);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_UnresolvedOnly_ExcludesResolvedAlerts()
    {
        var dbName = Guid.NewGuid().ToString();
        var (manager, _, alertId) = await SetupWithAlertAsync(dbName);

        // Resolve the only alert
        await manager.ResolveAlertAsync(alertId);

        var alerts = (await manager.GetLowStockAlertsAsync(unresolvedOnly: true)).ToList();

        Assert.Empty(alerts);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_UnresolvedOnly_ReturnsOpenAlerts()
    {
        var (manager, _, _) = await SetupWithAlertAsync(Guid.NewGuid().ToString());

        var alerts = (await manager.GetLowStockAlertsAsync(unresolvedOnly: true)).ToList();

        Assert.Single(alerts);
        Assert.False(alerts[0].IsResolved);
    }

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsCorrectAlertData()
    {
        var (manager, productId, _) = await SetupWithAlertAsync(Guid.NewGuid().ToString());

        var alerts = (await manager.GetLowStockAlertsAsync()).ToList();

        Assert.Single(alerts);
        var alert = alerts[0];
        Assert.Equal(productId, alert.ProductId);
        Assert.Equal(5,         alert.QuantityAtAlert);   // 15 - 10 = 5
        Assert.False(alert.IsResolved);
        Assert.Null(alert.ResolvedAt);
    }

    // ─── ResolveAlertAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAlertAsync_ExistingAlert_ReturnsTrueAndMarksResolved()
    {
        var (manager, _, alertId) = await SetupWithAlertAsync(Guid.NewGuid().ToString());

        var result = await manager.ResolveAlertAsync(alertId);

        Assert.True(result);
    }

    [Fact]
    public async Task ResolveAlertAsync_NonExistentAlert_ReturnsFalse()
    {
        await using var ctx = DbContextFactory.Create();
        var manager = new ProductManager(ctx);

        var result = await manager.ResolveAlertAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task ResolveAlertAsync_SetsIsResolvedAndResolvedAt()
    {
        var dbName = Guid.NewGuid().ToString();
        var (manager, _, alertId) = await SetupWithAlertAsync(dbName);

        var before = DateTime.UtcNow;
        await manager.ResolveAlertAsync(alertId);

        // Re-open context to read the persisted state
        var ctx2  = DbContextFactory.Create(dbName);
        var alert = ctx2.LowStockAlerts.Single(a => a.Id == alertId);

        Assert.True(alert.IsResolved);
        Assert.NotNull(alert.ResolvedAt);
        Assert.True(alert.ResolvedAt >= before);
    }

    // ─── Auto-resolve on restock ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateProductAsync_WhenStockRestockedAboveThreshold_AutoResolvesOpenAlert()
    {
        var dbName = Guid.NewGuid().ToString();
        var (manager, productId, alertId) = await SetupWithAlertAsync(dbName);

        // Restock above threshold via update
        var updateDto = new UpdateProductDto
        {
            Sku               = "SKU-A",
            Name              = "Alert Widget",
            Price             = 5.00m,
            IsActive          = true,
            QuantityAvailable = 50,   // well above threshold
            LowStockThreshold = 10
        };

        await manager.UpdateProductAsync(productId, updateDto);

        var ctx2  = DbContextFactory.Create(dbName);
        var alert = ctx2.LowStockAlerts.Single(a => a.Id == alertId);
        Assert.True(alert.IsResolved);
        Assert.NotNull(alert.ResolvedAt);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenStockStillLow_DoesNotCreateDuplicateAlert()
    {
        var dbName = Guid.NewGuid().ToString();
        var (manager, productId, _) = await SetupWithAlertAsync(dbName);

        // Keep stock below threshold
        var updateDto = new UpdateProductDto
        {
            Sku               = "SKU-A",
            Name              = "Alert Widget",
            Price             = 5.00m,
            IsActive          = true,
            QuantityAvailable = 3,    // still ≤ threshold(10)
            LowStockThreshold = 10
        };

        await manager.UpdateProductAsync(productId, updateDto);

        var ctx2   = DbContextFactory.Create(dbName);
        var alerts = ctx2.LowStockAlerts
            .Where(a => a.ProductId == productId && !a.IsResolved)
            .ToList();

        // Should still be exactly ONE unresolved alert (no duplicate)
        Assert.Single(alerts);
    }
}
