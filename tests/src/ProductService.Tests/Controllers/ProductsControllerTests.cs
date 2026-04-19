using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductService.Controllers;
using ProductService.DTOs;
using ProductService.Interfaces;

namespace ProductService.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="ProductsController"/> — product and stock endpoints.
/// The controller is tested in isolation using a mocked <see cref="IProductManager"/>.
/// </summary>
public class ProductsControllerTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static ProductsController CreateController(IProductManager manager)
    {
        var controller = new ProductsController(manager);

        // Provide a minimal HttpContext so Response.Headers can be written
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static ProductResponseDto SampleProduct(Guid? id = null) => new()
    {
        Id                = id ?? Guid.NewGuid(),
        Sku               = "SKU-001",
        Name              = "Test Widget",
        Price             = 9.99m,
        IsActive          = true,
        QuantityAvailable = 50,
        LowStockThreshold = 10,
        IsLowStock        = false
    };

    // ─── GET /api/products ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_ValidParameters_ReturnsOkWithData()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetProductsAsync(1, 10, null, null, null))
            .ReturnsAsync(new PaginatedResponse<ProductResponseDto>
            {
                Data         = [SampleProduct()],
                PageNumber   = 1,
                PageSize     = 10,
                TotalRecords = 1
            });

        var controller = CreateController(mock.Object);
        var result = await controller.GetProducts(1, 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public async Task GetProducts_InvalidPagination_ReturnsBadRequest(int page, int size)
    {
        var mock       = new Mock<IProductManager>();
        var controller = CreateController(mock.Object);

        var result = await controller.GetProducts(page, size);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ─── GET /api/products/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOkWithProduct()
    {
        var productId = Guid.NewGuid();
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetProductByIdAsync(productId))
            .ReturnsAsync(SampleProduct(productId));

        var controller = CreateController(mock.Object);
        var result = await controller.GetProductById(productId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ProductResponseDto>(ok.Value);
        Assert.Equal(productId, body.Id);
    }

    [Fact]
    public async Task GetProductById_NonExistentId_ReturnsNotFound()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetProductByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProductResponseDto?)null);

        var controller = CreateController(mock.Object);
        var result = await controller.GetProductById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── GET /api/products/stock ─────────────────────────────────────────────

    [Fact]
    public async Task GetStock_ReturnsOkWithStockList()
    {
        var stocks = new List<StockResponseDto>
        {
            new() { ProductId = Guid.NewGuid(), Name = "Widget A", QuantityAvailable = 100 }
        };

        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetStockAsync()).ReturnsAsync(stocks);

        var controller = CreateController(mock.Object);
        var result = await controller.GetStock();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // ─── GET /api/products/{id}/stock ───────────────────────────────────────

    [Fact]
    public async Task GetStockByProductId_ExistingProduct_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var stock = new StockResponseDto { ProductId = productId, QuantityAvailable = 20 };

        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetStockByProductIdAsync(productId)).ReturnsAsync(stock);

        var controller = CreateController(mock.Object);
        var result = await controller.GetStockByProductId(productId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<StockResponseDto>(ok.Value);
        Assert.Equal(productId, body.ProductId);
    }

    [Fact]
    public async Task GetStockByProductId_NonExistentProduct_ReturnsNotFound()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetStockByProductIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((StockResponseDto?)null);

        var controller = CreateController(mock.Object);
        var result = await controller.GetStockByProductId(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── PUT /api/products/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_ExistingId_ReturnsOkWithUpdatedProduct()
    {
        var productId = Guid.NewGuid();
        var updated   = SampleProduct(productId);
        updated.Name  = "Updated Widget";

        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.UpdateProductAsync(productId, It.IsAny<UpdateProductDto>()))
            .ReturnsAsync(updated);

        var controller = CreateController(mock.Object);
        var result = await controller.UpdateProduct(productId, new UpdateProductDto
        {
            Sku               = "SKU-001",
            Name              = "Updated Widget",
            Price             = 19.99m,
            QuantityAvailable = 40,
            LowStockThreshold = 5
        });

        var ok   = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ProductResponseDto>(ok.Value);
        Assert.Equal("Updated Widget", body.Name);
    }

    [Fact]
    public async Task UpdateProduct_NonExistentId_ReturnsNotFound()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.UpdateProductAsync(It.IsAny<Guid>(), It.IsAny<UpdateProductDto>()))
            .ReturnsAsync((ProductResponseDto?)null);

        var controller = CreateController(mock.Object);
        var result = await controller.UpdateProduct(Guid.NewGuid(), new UpdateProductDto
        {
            Sku               = "SKU-X",
            Name              = "Ghost",
            Price             = 1m,
            QuantityAvailable = 0,
            LowStockThreshold = 5
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── DELETE /api/products/{id} ───────────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsNoContent()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.DeleteProductAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var controller = CreateController(mock.Object);
        var result = await controller.DeleteProduct(Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_NonExistentId_ReturnsNotFound()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.DeleteProductAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = CreateController(mock.Object);
        var result = await controller.DeleteProduct(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── POST /api/products/deduct-stock ────────────────────────────────────

    [Fact]
    public async Task DeductStock_SufficientStock_ReturnsOk()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.DeductStockAsync(It.IsAny<DeductStockDto>()))
            .ReturnsAsync((true, "Stock deducted successfully. Remaining: 45."));

        var controller = CreateController(mock.Object);
        var result = await controller.DeductStock(new DeductStockDto
        {
            ProductId = Guid.NewGuid(),
            OrderId   = Guid.NewGuid(),
            Quantity  = 5
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeductStock_InsufficientStock_ReturnsConflict()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.DeductStockAsync(It.IsAny<DeductStockDto>()))
            .ReturnsAsync((false, "Insufficient stock. Available: 2, Requested: 10."));

        var controller = CreateController(mock.Object);
        var result = await controller.DeductStock(new DeductStockDto
        {
            ProductId = Guid.NewGuid(),
            OrderId   = Guid.NewGuid(),
            Quantity  = 10
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ─── GET /api/products/alerts ────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_ReturnsOkWithAlertList()
    {
        var alerts = new List<LowStockAlertDto>
        {
            new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Widget A", IsResolved = false }
        };

        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetLowStockAlertsAsync(false)).ReturnsAsync(alerts);

        var controller = CreateController(mock.Object);
        var result = await controller.GetAlerts(unresolvedOnly: false);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // ─── PATCH /api/products/alerts/{id}/resolve ─────────────────────────────

    [Fact]
    public async Task ResolveAlert_ExistingAlert_ReturnsOk()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.ResolveAlertAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var controller = CreateController(mock.Object);
        var result = await controller.ResolveAlert(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResolveAlert_NonExistentAlert_ReturnsNotFound()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.ResolveAlertAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = CreateController(mock.Object);
        var result = await controller.ResolveAlert(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── GET /api/products/search/{name} ────────────────────────────────────

    [Fact]
    public async Task SearchProductsByName_ValidName_ReturnsOkWithResults()
    {
        var mock = new Mock<IProductManager>();
        mock.Setup(m => m.GetProductsAsync(1, 100, null, null, "widget"))
            .ReturnsAsync(new PaginatedResponse<ProductResponseDto>
            {
                Data         = [SampleProduct()],
                PageNumber   = 1,
                PageSize     = 100,
                TotalRecords = 1
            });

        var controller = CreateController(mock.Object);
        var result = await controller.SearchProductsByName("widget");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
