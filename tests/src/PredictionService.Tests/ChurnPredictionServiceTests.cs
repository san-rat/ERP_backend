using Moq;
using PredictionService.Models;
using PredictionService.Repositories;
using PredictionService.Services;
using PredictionService.ML;
using Xunit;
using Microsoft.Extensions.Logging;

namespace PredictionService.Tests.Services;

public class ChurnPredictionServiceTests
{
    private readonly Mock<IChurnRepository> _mockRepository;
    private readonly Mock<ChurnModelManager> _mockModelManager;
    private readonly Mock<ILogger<ChurnPredictionService>> _mockLogger;
    private readonly ChurnPredictionService _service;

    public ChurnPredictionServiceTests()
    {
        _mockRepository = new Mock<IChurnRepository>();
        _mockModelManager = new Mock<ChurnModelManager>(
            new Mock<ILogger<ChurnModelManager>>().Object,
            new Mock<ITrainingDataRepository>().Object
        );
        _mockLogger = new Mock<ILogger<ChurnPredictionService>>();
        _service = new ChurnPredictionService(_mockRepository.Object, _mockModelManager.Object, _mockLogger.Object);
    }

    #region PredictChurnAsync Tests

    [Fact]
    public async Task PredictChurnAsync_WithValidCustomerId_ReturnsPrediction()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);
        
        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.75m, "HIGH", new List<(string, decimal)>
            {
                ("Recency", 0.3m),
                ("Return Rate", 0.25m),
                ("Cancellation Rate", 0.2m)
            }));

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(0.75m, result.ChurnProbability);
        Assert.Equal("HIGH", result.ChurnRiskLabel);
        Assert.NotEmpty(result.TopFactors);
    }

    [Fact]
    public async Task PredictChurnAsync_WithNonExistentCustomer_ReturnsNull()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync((CustomerFeatures?)null);

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetCustomerFeaturesAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task PredictChurnAsync_SavesPredictionToRepository()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.5m, "MEDIUM", new List<(string, decimal)>()));

        _mockRepository
            .Setup(r => r.SaveChurnPredictionAsync(It.IsAny<ChurnPredictionOutput>()))
            .ReturnsAsync(true);

        // Act
        await _service.PredictChurnAsync(customerId);

        // Assert
        _mockRepository.Verify(
            r => r.SaveChurnPredictionAsync(It.IsAny<ChurnPredictionOutput>()),
            Times.Once
        );
    }

    [Fact]
    public async Task PredictChurnAsync_SavesFactorsToRepository()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.6m, "MEDIUM", new List<(string, decimal)>
            {
                ("Recency", 0.3m),
                ("Return Rate", 0.2m)
            }));

        _mockRepository
            .Setup(r => r.SaveChurnPredictionAsync(It.IsAny<ChurnPredictionOutput>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.SaveChurnFactorsAsync(It.IsAny<Guid>(), It.IsAny<List<ChurnFactor>>()))
            .ReturnsAsync(true);

        // Act
        await _service.PredictChurnAsync(customerId);

        // Assert
        _mockRepository.Verify(
            r => r.SaveChurnFactorsAsync(It.IsAny<Guid>(), It.IsAny<List<ChurnFactor>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task PredictChurnAsync_WithLowProbability_ReturnsLowRisk()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.2m, "LOW", new List<(string, decimal)>()));

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LOW", result.ChurnRiskLabel);
        Assert.Equal(0.2m, result.ChurnProbability);
    }

    [Fact]
    public async Task PredictChurnAsync_WithHighProbability_ReturnsHighRisk()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.85m, "HIGH", new List<(string, decimal)>()));

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HIGH", result.ChurnRiskLabel);
        Assert.Equal(0.85m, result.ChurnProbability);
    }

    [Fact]
    public async Task PredictChurnAsync_WhenRepositoryThrows_ReturnsNull()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PredictChurnAsync_CreatesUniquePredictionId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.5m, "MEDIUM", new List<(string, decimal)>()));

        var savedPredictions = new List<ChurnPredictionOutput>();

        _mockRepository
            .Setup(r => r.SaveChurnPredictionAsync(It.IsAny<ChurnPredictionOutput>()))
            .Callback<ChurnPredictionOutput>(p => savedPredictions.Add(p))
            .ReturnsAsync(true);

        // Act
        var result1 = await _service.PredictChurnAsync(customerId);
        var result2 = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.NotEqual(result1!.PredictionId, result2!.PredictionId);
    }

    [Fact]
    public async Task PredictChurnAsync_PopulatesTopFactors()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var features = CreateTestCustomerFeatures(customerId);

        _mockRepository
            .Setup(r => r.GetCustomerFeaturesAsync(customerId))
            .ReturnsAsync(features);

        _mockModelManager
            .Setup(m => m.PredictChurn(It.IsAny<CustomerFeatures>()))
            .Returns((0.7m, "HIGH", new List<(string, decimal)>
            {
                ("Recency", 0.35m),
                ("Cancellation Rate", 0.25m),
                ("Return Rate", 0.1m)
            }));

        // Act
        var result = await _service.PredictChurnAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TopFactors.Count);
        Assert.All(result.TopFactors, factor => 
        {
            Assert.NotNull(factor.FactorName);
            Assert.True(factor.Weight >= 0);
        });
    }

    #endregion

    #region Helper Methods

    private CustomerFeatures CreateTestCustomerFeatures(Guid customerId)
    {
        return new CustomerFeatures
        {
            CustomerId = customerId,
            Recency = 45,
            Frequency = 15,
            MonetaryValue = 2500,
            AvgOrderValue = 150,
            TenureDays = 730,
            ProductDiversity = 8,
            CategoryDiversity = 5,
            AvgProductsPerOrder = (decimal)2.5f,
            ReturnCount = 2,
            ReturnRate = 0.05m,
            TotalRefunded = 300,
            CompletedOrders = 20,
            CancelledOrders = 1,
            CancellationRate = 0.05m,
            AccountAgeDays = 730,
            DaysSinceActivity = 45,
            InactiveFlag = 0
        };
    }

    #endregion
}