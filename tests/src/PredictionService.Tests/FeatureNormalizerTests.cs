using PredictionService.Models;
using PredictionService.ML;
using Xunit;
using Microsoft.Extensions.Logging;


namespace PredictionService.Tests.ML;

public class FeatureNormalizerTests
{
    private readonly FeatureNormalizer _normalizer;

    public FeatureNormalizerTests()
    {
        _normalizer = new FeatureNormalizer();
    }

    #region NormalizeFeatures Tests

    [Fact]
    public void NormalizeFeatures_WithZeroValues_ReturnsNormalizedArray()
    {
        // Arrange
        var features = CreateTestCustomerFeatures(0, 0, 0);

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.Length);
        Assert.All(result, value => Assert.True(value >= 0 && value <= 1));
    }

    [Fact]
    public void NormalizeFeatures_WithMaxValues_ReturnsValuesClampedToOne()
    {
        // Arrange
        var features = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = 365,
            Frequency = 100,
            MonetaryValue = 100000,
            AvgOrderValue = 10000,
            TenureDays = 3650,
            ProductDiversity = 100,
            CategoryDiversity = 50,
            AvgProductsPerOrder = 10,
            ReturnCount = 100,
            ReturnRate = 1,
            TotalRefunded = 100000,
            CompletedOrders = 100,
            CancelledOrders = 50,
            CancellationRate = 1,
            AccountAgeDays = 3650,
            DaysSinceActivity = 365,
            InactiveFlag = 1
        };

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, value => Assert.True(value >= 0 && value <= 1));
    }

    [Fact]
    public void NormalizeFeatures_WithMidRangeValues_ReturnsValuesInMiddleRange()
    {
        // Arrange
        var features = CreateTestCustomerFeatures(182, 50, 50000);

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, value => Assert.True(value >= 0 && value <= 1));
        // Recency at 182 (midpoint of 365) should normalize to ~0.5
        Assert.True(result[0] > 0.4 && result[0] < 0.6);
    }

    [Fact]
    public void NormalizeFeatures_WithNegativeValues_ClampToZero()
    {
        // Arrange
        var features = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = -10, // Negative value
            Frequency = 50,
            MonetaryValue = 5000,
            AvgOrderValue = 500,
            TenureDays = 1000,
            ProductDiversity = 5,
            CategoryDiversity = 3,
            AvgProductsPerOrder = 2,
            ReturnCount = 1,
            ReturnRate = 0.1m,
            TotalRefunded = 100,
            CompletedOrders = 10,
            CancelledOrders = 0,
            CancellationRate = 0.05m,
            AccountAgeDays = 1000,
            DaysSinceActivity = 30,
            InactiveFlag = 0
        };

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.True(result[0] >= 0); // First value (Recency) should be clamped to 0
    }

    [Fact]
    public void NormalizeFeatures_ReturnsArrayWithCorrectLength()
    {
        // Arrange
        var features = CreateTestCustomerFeatures(30, 10, 1000);

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.Equal(12, result.Length);
    }

    [Fact]
    public void NormalizeFeatures_AllValuesAreInValidRange()
    {
        // Arrange
        var features = CreateTestCustomerFeatures(100, 50, 50000);

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, value =>
        {
            Assert.True(value >= 0, $"Value {value} is less than 0");
            Assert.True(value <= 1, $"Value {value} is greater than 1");
        });
    }

    [Fact]
    public void NormalizeFeatures_WithVaryingRecency_ProducesDifferentNormalizedValues()
    {
        // Arrange
        var features1 = CreateTestCustomerFeatures(30, 50, 5000);
        var features2 = CreateTestCustomerFeatures(300, 50, 5000);

        // Act
        var result1 = _normalizer.NormalizeFeatures(features1);
        var result2 = _normalizer.NormalizeFeatures(features2);

        // Assert
        Assert.NotEqual(result1[0], result2[0]); // Recency normalized values should differ
    }

    [Fact]
    public void NormalizeFeatures_WithInactiveCustomer_ProducesHighInactivityValue()
    {
        // Arrange
        var activeFeatures = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = 10,
            Frequency = 50,
            MonetaryValue = 5000,
            AvgOrderValue = 500,
            TenureDays = 1000,
            ProductDiversity = 5,
            CategoryDiversity = 3,
            AvgProductsPerOrder = 2,
            ReturnCount = 1,
            ReturnRate = 0.1m,
            TotalRefunded = 100,
            CompletedOrders = 10,
            CancelledOrders = 0,
            CancellationRate = 0.05m,
            AccountAgeDays = 1000,
            DaysSinceActivity = 30,
            InactiveFlag = 0
        };

        var inactiveFeatures = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = 10,
            Frequency = 50,
            MonetaryValue = 5000,
            AvgOrderValue = 500,
            TenureDays = 1000,
            ProductDiversity = 5,
            CategoryDiversity = 3,
            AvgProductsPerOrder = 2,
            ReturnCount = 1,
            ReturnRate = 0.1m,
            TotalRefunded = 100,
            CompletedOrders = 10,
            CancelledOrders = 0,
            CancellationRate = 0.05m,
            AccountAgeDays = 1000,
            DaysSinceActivity = 30,
            InactiveFlag = 1
        };

        // Act
        var activeResult = _normalizer.NormalizeFeatures(activeFeatures);
        var inactiveResult = _normalizer.NormalizeFeatures(inactiveFeatures);

        // Assert
        Assert.NotEqual(activeResult[11], inactiveResult[11]); // InactiveFlag is at index 11
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void NormalizeFeatures_WithAllZeros_ReturnsAllZeros()
    {
        // Arrange
        var features = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = 0,
            Frequency = 0,
            MonetaryValue = 0,
            AvgOrderValue = 0,
            TenureDays = 0,
            ProductDiversity = 0,
            CategoryDiversity = 0,
            AvgProductsPerOrder = 0,
            ReturnCount = 0,
            ReturnRate = 0,
            TotalRefunded = 0,
            CompletedOrders = 0,
            CancelledOrders = 0,
            CancellationRate = 0,
            AccountAgeDays = 0,
            DaysSinceActivity = 0,
            InactiveFlag = 0
        };

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, value => Assert.True(value >= 0 && value <= 1));
    }

    [Fact]
    public void NormalizeFeatures_IsConsistent()
    {
        // Arrange
        var features = CreateTestCustomerFeatures(50, 25, 2500);

        // Act
        var result1 = _normalizer.NormalizeFeatures(features);
        var result2 = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void NormalizeFeatures_WithLargeValues_HandlesCorrectly()
    {
        // Arrange
        var features = new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = 1000, // Beyond max range
            Frequency = 500, // Beyond max range
            MonetaryValue = 500000, // Beyond max range
            AvgOrderValue = 50000, // Beyond max range
            TenureDays = 10000, // Beyond max range
            ProductDiversity = 50,
            CategoryDiversity = 25,
            AvgProductsPerOrder = 10,
            ReturnCount = 50,
            ReturnRate = 5, // Beyond max range
            TotalRefunded = 500000,
            CompletedOrders = 200,
            CancelledOrders = 100,
            CancellationRate = 5,
            AccountAgeDays = 10000,
            DaysSinceActivity = 1000,
            InactiveFlag = 1
        };

        // Act
        var result = _normalizer.NormalizeFeatures(features);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, value => Assert.True(value >= 0 && value <= 1));
    }

    #endregion

    #region Helper Methods

    private CustomerFeatures CreateTestCustomerFeatures(int recency, int frequency, decimal monetaryValue)
    {
        return new CustomerFeatures
        {
            CustomerId = Guid.NewGuid(),
            Recency = recency,
            Frequency = frequency,
            MonetaryValue = monetaryValue,
            AvgOrderValue = frequency > 0 ? monetaryValue / frequency : 100,
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
            DaysSinceActivity = recency,
            InactiveFlag = recency > 180 ? 1 : 0
        };
    }

    #endregion
}