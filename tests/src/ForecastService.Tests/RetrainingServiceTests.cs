using Xunit;
using Moq;
using ForecastService.Models;
using ForecastService.Services;
using Microsoft.Extensions.Logging;

namespace ForecastService.Tests.Services;

public class RetrainingServiceTests
{
    private readonly Mock<ISalesForecasterService> _mockForecastService;
    private readonly Mock<IProductDataService> _mockProductDataService;
    private readonly Mock<ILogger<RetrainingService>> _mockLogger;
    private readonly RetrainingService _service;

    public RetrainingServiceTests()
    {
        _mockForecastService = new Mock<ISalesForecasterService>();
        _mockProductDataService = new Mock<IProductDataService>();
        _mockLogger = new Mock<ILogger<RetrainingService>>();
        _service = new RetrainingService(
            _mockForecastService.Object,
            _mockProductDataService.Object,
            _mockLogger.Object);
    }

    #region TriggerRetrainingAsync Tests

    [Fact]
    public async Task TriggerRetrainingAsync_WithMultipleProducts_RetrainsAllProducts()
    {
        // Arrange
        var productIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var metrics = productIds.Select(id => new ProductMetrics
        {
            ProductId = id,
            ProductName = $"Product {id}",
            TotalUnitsSold = 100
        }).ToList();

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync((ForecastRequest request) => new ForecastResult
            {
                ForecastId = Guid.NewGuid(),
                ProductId = request.ProductId,
                ProductName = "Test",
                Forecasts = new List<DailyForecast>(),
                Algorithm = "EXPONENTIAL_SMOOTHING",
                MAPE = 0.05m,
                RMSE = 10m,
                R_Squared = 0.95m
            });

        // Act
        var result = await _service.TriggerRetrainingAsync("Test Trigger");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("COMPLETED", result.Status);
        Assert.Equal(3, result.TotalProductsRetrained);
        Assert.Equal(3, result.SuccessfullyTrained);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task TriggerRetrainingAsync_WhenAlreadyRetraining_ReturnsPreviousResult()
    {
        // Arrange
        var productIds = new[] { Guid.NewGuid() };
        var metrics = productIds.Select(id => new ProductMetrics
        {
            ProductId = id,
            ProductName = $"Product {id}",
            TotalUnitsSold = 100
        }).ToList();

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .Returns(async () =>
            {
                await Task.Delay(500); // Simulate long-running operation
                return new ForecastResult
                {
                    ForecastId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test",
                    Forecasts = new List<DailyForecast>(),
                    Algorithm = "EXPONENTIAL_SMOOTHING"
                };
            });

        // Start first retraining
        var task1 = _service.TriggerRetrainingAsync("First");

        // Give it a moment to start
        await Task.Delay(50);

        // Try to start second retraining
        var result = await _service.TriggerRetrainingAsync("Second");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IN_PROGRESS", result.Status);
        Assert.Contains("already in progress", result.ErrorMessage ?? "");

        // Cleanup
        await task1;
    }

    [Fact]
    public async Task TriggerRetrainingAsync_WithNoProducts_ReturnsCompletedWithZeroProducts()
    {
        // Arrange
        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(new List<ProductMetrics>());

        // Act
        var result = await _service.TriggerRetrainingAsync("No Products");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("COMPLETED", result.Status);
        Assert.Equal(0, result.TotalProductsRetrained);
    }

    [Fact]
    public async Task TriggerRetrainingAsync_StoresResultInHistory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var metrics = new List<ProductMetrics>
        {
            new ProductMetrics { ProductId = productId, ProductName = "Test Product" }
        };

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync(new ForecastResult
            {
                ForecastId = Guid.NewGuid(),
                ProductId = productId,
                ProductName = "Test Product",
                Forecasts = new List<DailyForecast>(),
                Algorithm = "EXPONENTIAL_SMOOTHING"
            });

        // Act
        var result = await _service.TriggerRetrainingAsync("Test");

        // Get history to verify storage
        var history = await _service.GetRetrainingHistoryAsync(1);

        // Assert
        Assert.NotEmpty(history);
        Assert.Equal(result.RetrainingId, history[0].RetrainingId);
        Assert.Equal("COMPLETED", history[0].Status);
    }

    #endregion

    #region RetrainProductAsync Tests

    [Fact]
    public async Task RetrainProductAsync_WithValidProduct_ReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync(new ForecastResult
            {
                ForecastId = Guid.NewGuid(),
                ProductId = productId,
                ProductName = "Test Product",
                Forecasts = new List<DailyForecast>(),
                Algorithm = "EXPONENTIAL_SMOOTHING",
                MAPE = 0.05m,
                RMSE = 10m,
                R_Squared = 0.95m
            });

        // Act
        var result = await _service.RetrainProductAsync(productId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RetrainProductAsync_WithNullForecast_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync((ForecastResult?)null);

        // Act
        var result = await _service.RetrainProductAsync(productId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetRetrainingStatusAsync Tests

    [Fact]
    public async Task GetRetrainingStatusAsync_WithoutActiveRetraining_ReturnsStatus()
    {
        // Act
        var status = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.False(status.IsInProgress);
        Assert.True(status.AutoRetrainingEnabled);
        Assert.Equal(DayOfWeek.Sunday, status.ScheduledDay);
    }

    [Fact]
    public async Task GetRetrainingStatusAsync_DuringActiveRetraining_ShowsProgress()
    {
        // Arrange
        var productIds = Enumerable.Range(0, 5)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var metrics = productIds.Select(id => new ProductMetrics
        {
            ProductId = id,
            ProductName = $"Product {id}"
        }).ToList();

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .Returns(async () =>
            {
                await Task.Delay(100);
                return new ForecastResult
                {
                    ForecastId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test",
                    Forecasts = new List<DailyForecast>()
                };
            });

        // Start retraining
        var retrainingTask = _service.TriggerRetrainingAsync("Test");

        // Give it a moment to start
        await Task.Delay(50);

        // Act
        var status = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.True(status.IsInProgress);
        Assert.True(status.ProductsProcessed > 0);
        Assert.Equal(5, status.TotalProducts);

        // Cleanup
        await retrainingTask;
    }

    #endregion

    #region GetRetrainingHistoryAsync Tests

    [Fact]
    public async Task GetRetrainingHistoryAsync_ReturnsHistoryInReverseOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var metrics = new List<ProductMetrics>
        {
            new ProductMetrics { ProductId = productId, ProductName = "Test" }
        };

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync(new ForecastResult
            {
                ForecastId = Guid.NewGuid(),
                ProductId = productId,
                ProductName = "Test",
                Forecasts = new List<DailyForecast>()
            });

        // Trigger multiple retraining operations
        await _service.TriggerRetrainingAsync("First");
        await Task.Delay(10);
        await _service.TriggerRetrainingAsync("Second");

        // Act
        var history = await _service.GetRetrainingHistoryAsync(10);

        // Assert
        Assert.NotEmpty(history);
        Assert.True(history.Count >= 2);
        // Most recent should be first
        Assert.Equal("Second", history[0].TriggerReason);
        Assert.Equal("First", history[1].TriggerReason);
    }

    [Fact]
    public async Task GetRetrainingHistoryAsync_RespectsLimitParameter()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var metrics = new List<ProductMetrics>
        {
            new ProductMetrics { ProductId = productId, ProductName = "Test" }
        };

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .ReturnsAsync(new ForecastResult
            {
                ForecastId = Guid.NewGuid(),
                ProductId = productId,
                ProductName = "Test",
                Forecasts = new List<DailyForecast>()
            });

        // Trigger multiple retraining operations
        for (int i = 0; i < 5; i++)
        {
            await _service.TriggerRetrainingAsync($"Retrain {i}");
        }

        // Act
        var history = await _service.GetRetrainingHistoryAsync(limit: 2);

        // Assert
        Assert.Equal(2, history.Count);
    }

    #endregion

    #region SetAutoRetrainingEnabled Tests

    [Fact]
    public async Task SetAutoRetrainingEnabled_WithTrue_EnablesAutoRetrain()
    {
        // Arrange
        _service.SetAutoRetrainingEnabled(true);

        // Act
        var status = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.True(status.AutoRetrainingEnabled);
    }

    [Fact]
    public async Task SetAutoRetrainingEnabled_WithFalse_DisablesAutoRetrain()
    {
        // Arrange
        _service.SetAutoRetrainingEnabled(false);

        // Act
        var status = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.False(status.AutoRetrainingEnabled);
    }

    #endregion

    #region SetAutoRetrainingDay Tests

    [Fact]
    public async Task SetAutoRetrainingDay_WithValidDay_SetsScheduledDay()
    {
        // Arrange
        _service.SetAutoRetrainingDay(DayOfWeek.Monday);

        // Act
        var status = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.Equal(DayOfWeek.Monday, status.ScheduledDay);
    }

    [Fact]
    public async Task SetAutoRetrainingDay_WithMultipleDays_UpdatesSchedule()
    {
        // Arrange & Act
        _service.SetAutoRetrainingDay(DayOfWeek.Wednesday);
        var status1 = await _service.GetRetrainingStatusAsync();

        _service.SetAutoRetrainingDay(DayOfWeek.Friday);
        var status2 = await _service.GetRetrainingStatusAsync();

        // Assert
        Assert.Equal(DayOfWeek.Wednesday, status1.ScheduledDay);
        Assert.Equal(DayOfWeek.Friday, status2.ScheduledDay);
    }

    #endregion

    #region IsRetrainingInProgress Tests

    [Fact]
    public async Task IsRetrainingInProgress_BeforeTriggering_ReturnsFalse()
    {
        // Assert
        Assert.False(_service.IsRetrainingInProgress);
    }

    [Fact]
    public async Task IsRetrainingInProgress_DuringRetraining_ReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var metrics = new List<ProductMetrics>
        {
            new ProductMetrics { ProductId = productId, ProductName = "Test" }
        };

        _mockProductDataService
            .Setup(s => s.GetAllProductMetricsAsync())
            .ReturnsAsync(metrics);

        _mockForecastService
            .Setup(s => s.ForecastProductSalesAsync(It.IsAny<ForecastRequest>()))
            .Returns(async () =>
            {
                await Task.Delay(200);
                return new ForecastResult
                {
                    ForecastId = Guid.NewGuid(),
                    ProductId = productId,
                    ProductName = "Test",
                    Forecasts = new List<DailyForecast>()
                };
            });

        // Act
        var retrainingTask = _service.TriggerRetrainingAsync("Test");
        await Task.Delay(50);
        var isDuringRetrain = _service.IsRetrainingInProgress;

        // Cleanup
        await retrainingTask;
        var isAfterRetrain = _service.IsRetrainingInProgress;

        // Assert
        Assert.True(isDuringRetrain);
        Assert.False(isAfterRetrain);
    }

    #endregion
}