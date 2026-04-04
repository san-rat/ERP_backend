using Xunit;
using Moq;
using ForecastService.Models;
using ForecastService.Services;
using Microsoft.Extensions.Logging;

namespace ForecastService.Tests.Services;

public class SalesForecasterServiceTests
{
    private readonly Mock<IProductDataService> _mockProductDataService;
    private readonly Mock<ITimeSeriesAnalyzer> _mockAnalyzer;
    private readonly Mock<ILogger<SalesForecasterService>> _mockLogger;
    private readonly SalesForecasterService _service;

    public SalesForecasterServiceTests()
    {
        _mockProductDataService = new Mock<IProductDataService>();
        _mockAnalyzer = new Mock<ITimeSeriesAnalyzer>();
        _mockLogger = new Mock<ILogger<SalesForecasterService>>();
        _service = new SalesForecasterService(
            _mockProductDataService.Object,
            _mockAnalyzer.Object,
            _mockLogger.Object);
    }

    #region ForecastProductSalesAsync Tests

    [Fact]
    public async Task ForecastProductSalesAsync_WithValidData_ReturnsForecastResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ForecastRequest
        {
            ProductId = productId,
            ForecastDays = 30,
            Algorithm = "AUTO",
            IncludeConfidenceInterval = true,
            ConfidenceLevel = 95
        };

        var metrics = new ProductMetrics
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentPrice = 100m,
            TotalUnitsSold = 1000
        };

        var salesHistory = GenerateSalesHistory(productId, 365);

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(salesHistory);

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductId);
        Assert.Equal("Test Product", result.ProductName);
        Assert.Equal(30, result.DaysForecasted);
        Assert.NotEmpty(result.Forecasts);
    }

    [Fact]
    public async Task ForecastProductSalesAsync_WithNullMetrics_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ForecastRequest { ProductId = productId };

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync((ProductMetrics?)null);

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ForecastProductSalesAsync_WithInsufficientData_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ForecastRequest { ProductId = productId };

        var metrics = new ProductMetrics { ProductId = productId, ProductName = "Test" };

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(new List<SalesData>()); // Empty history

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ForecastProductSalesAsync_GeneratesForecastForRequestedDays()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var forecastDays = 30;
        var request = new ForecastRequest
        {
            ProductId = productId,
            ForecastDays = forecastDays,
            Algorithm = "AUTO"
        };

        var metrics = new ProductMetrics
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentPrice = 100m
        };

        var salesHistory = GenerateSalesHistory(productId, 365);

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(salesHistory);

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(forecastDays, result.Forecasts.Count);
    }

    [Fact]
    public async Task ForecastProductSalesAsync_IncludesConfidenceIntervals()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ForecastRequest
        {
            ProductId = productId,
            ForecastDays = 10,
            IncludeConfidenceInterval = true,
            ConfidenceLevel = 95
        };

        var metrics = new ProductMetrics
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentPrice = 100m
        };

        var salesHistory = GenerateSalesHistory(productId, 365);

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(salesHistory);

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Forecasts, forecast =>
        {
            Assert.NotNull(forecast.Confidence);
            Assert.True(forecast.Confidence.LowerBound >= 0);
            Assert.True(forecast.Confidence.UpperBound > forecast.Confidence.PointEstimate);
        });
    }

    [Fact]
    public async Task ForecastProductSalesAsync_CalculatesMetricsCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ForecastRequest { ProductId = productId };

        var metrics = new ProductMetrics
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentPrice = 50m
        };

        var salesHistory = GenerateSalesHistory(productId, 365);

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(salesHistory);

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        var expectedMAPE = 0.08m;
        var expectedRMSE = 5m;
        var expectedR2 = 0.92m;

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((expectedMAPE, expectedRMSE, expectedR2));

        // Act
        var result = await _service.ForecastProductSalesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedMAPE, result.MAPE);
        Assert.Equal(expectedRMSE, result.RMSE);
        Assert.Equal(expectedR2, result.R_Squared);
    }

    #endregion

    #region ForecastMultipleProductsAsync Tests

    [Fact]
    public async Task ForecastMultipleProductsAsync_WithMultipleProducts_ReturnsAllForecasts()
    {
        // Arrange
        var requests = new List<ForecastRequest>();
        var productIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var id in productIds)
        {
            requests.Add(new ForecastRequest { ProductId = id, ForecastDays = 30 });

            var metrics = new ProductMetrics
            {
                ProductId = id,
                ProductName = $"Product {id}",
                CurrentPrice = 100m
            };

            var salesHistory = GenerateSalesHistory(id, 365);

            _mockProductDataService
                .Setup(s => s.GetProductMetricsAsync(id))
                .ReturnsAsync(metrics);

            _mockProductDataService
                .Setup(s => s.GetProductSalesHistoryAsync(id, It.IsAny<int>()))
                .ReturnsAsync(salesHistory);
        }

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var results = await _service.ForecastMultipleProductsAsync(requests);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.NotNull(r.ForecastId));
    }

    [Fact]
    public async Task ForecastMultipleProductsAsync_WithPartialFailures_ReturnsSuccessfulForecasts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        var requests = new List<ForecastRequest>
        {
            new ForecastRequest { ProductId = productId1, ForecastDays = 30 },
            new ForecastRequest { ProductId = productId2, ForecastDays = 30 },
            new ForecastRequest { ProductId = productId3, ForecastDays = 30 }
        };

        // Setup successful forecast for product 1
        var metrics1 = new ProductMetrics { ProductId = productId1, ProductName = "Product 1", CurrentPrice = 100m };
        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId1))
            .ReturnsAsync(metrics1);
        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId1, It.IsAny<int>()))
            .ReturnsAsync(GenerateSalesHistory(productId1, 365));

        // Setup failure for product 2 (null metrics)
        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId2))
            .ReturnsAsync((ProductMetrics?)null);

        // Setup successful forecast for product 3
        var metrics3 = new ProductMetrics { ProductId = productId3, ProductName = "Product 3", CurrentPrice = 100m };
        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId3))
            .ReturnsAsync(metrics3);
        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId3, It.IsAny<int>()))
            .ReturnsAsync(GenerateSalesHistory(productId3, 365));

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var results = await _service.ForecastMultipleProductsAsync(requests);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(2, results.Count); // Only 2 successful
        Assert.DoesNotContain(results, r => r.ProductId == productId2);
    }

    #endregion

    #region GetLatestForecastAsync Tests

    [Fact]
    public async Task GetLatestForecastAsync_WithValidProduct_ReturnsForecast()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var metrics = new ProductMetrics
        {
            ProductId = productId,
            ProductName = "Test Product",
            CurrentPrice = 100m
        };

        var salesHistory = GenerateSalesHistory(productId, 365);

        _mockProductDataService
            .Setup(s => s.GetProductMetricsAsync(productId))
            .ReturnsAsync(metrics);

        _mockProductDataService
            .Setup(s => s.GetProductSalesHistoryAsync(productId, It.IsAny<int>()))
            .ReturnsAsync(salesHistory);

        _mockAnalyzer
            .Setup(a => a.CalculateExponentialSmoothing(It.IsAny<decimal[]>(), It.IsAny<decimal>()))
            .Returns((decimal[] data, decimal alpha) => data);

        _mockAnalyzer
            .Setup(a => a.CalculateMetrics(It.IsAny<decimal[]>(), It.IsAny<decimal[]>()))
            .Returns((0.05m, 10m, 0.95m));

        // Act
        var result = await _service.GetLatestForecastAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductId);
    }

    #endregion

    #region SaveForecastAsync Tests

    [Fact]
    public async Task SaveForecastAsync_WithValidForecast_ReturnsTrue()
    {
        // Arrange
        var forecast = new ForecastResult
        {
            ForecastId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Forecasts = new List<DailyForecast>()
        };

        // Act
        var result = await _service.SaveForecastAsync(forecast);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Methods

    private List<SalesData> GenerateSalesHistory(Guid productId, int days)
    {
        var history = new List<SalesData>();
        var random = new Random();

        for (int i = 0; i < days; i++)
        {
            history.Add(new SalesData
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Date = DateTime.UtcNow.AddDays(-days + i),
                UnitsSold = random.Next(10, 100),
                Revenue = (decimal)random.Next(1000, 10000),
                AveragePrice = 100m,
                OrderCount = random.Next(1, 10)
            });
        }

        return history;
    }

    #endregion
}