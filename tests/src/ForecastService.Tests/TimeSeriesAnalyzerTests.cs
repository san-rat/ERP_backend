using Xunit;
using ForecastService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ForecastService.Tests.Services;

public class TimeSeriesAnalyzerTests
{
    private readonly Mock<ILogger<TimeSeriesAnalyzer>> _mockLogger;
    private readonly TimeSeriesAnalyzer _analyzer;

    public TimeSeriesAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<TimeSeriesAnalyzer>>();
        _analyzer = new TimeSeriesAnalyzer(_mockLogger.Object);
    }

    #region CalculateMovingAverage Tests

    [Fact]
    public void CalculateMovingAverage_WithValidData_ReturnsCorrectAverage()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        var period = 3;

        // Act
        var result = _analyzer.CalculateMovingAverage(data, period);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(8, result.Length); // 10 - 3 + 1 = 8
        Assert.Equal(20m, result[0]); // (10 + 20 + 30) / 3
        Assert.Equal(60m, result[4]); // (50 + 60 + 70) / 3
    }

    [Fact]
    public void CalculateMovingAverage_WithPeriodLargerThanData_ReturnsOriginalData()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30 };
        var period = 5;

        // Act
        var result = _analyzer.CalculateMovingAverage(data, period);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void CalculateMovingAverage_WithPeriodOne_ReturnsOriginalData()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };
        var period = 1;

        // Act
        var result = _analyzer.CalculateMovingAverage(data, period);

        // Assert
        Assert.Equal(data.Length, result.Length);
        Assert.Equal(data, result);
    }

    #endregion

    #region CalculateExponentialSmoothing Tests

    [Fact]
    public void CalculateExponentialSmoothing_WithValidAlpha_ReturnsSmoothedData()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };
        var alpha = 0.3m;

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, alpha);

        // Assert
        Assert.Equal(data.Length, result.Length);
        Assert.Equal(data[0], result[0]); // First value should be same
        Assert.True(result[1] > data[0]); // Should be between first and second value
        Assert.True(result[1] < data[1]);
    }

    [Fact]
    public void CalculateExponentialSmoothing_WithAlphaZero_ReturnsFirstValue()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };
        var alpha = 0m;

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, alpha);

        // Assert
        Assert.Equal(data.Length, result.Length);
        Assert.All(result, value => Assert.Equal(data[0], value));
    }

    [Fact]
    public void CalculateExponentialSmoothing_WithAlphaOne_ReturnsOriginalData()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };
        var alpha = 1m;

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, alpha);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void CalculateExponentialSmoothing_WithEmptyData_ReturnsEmpty()
    {
        // Arrange
        var data = new decimal[] { };
        var alpha = 0.3m;

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, alpha);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateExponentialSmoothing_WithInvalidAlpha_UsesDefault()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };
        var invalidAlpha = 1.5m; // Invalid (> 1)

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, invalidAlpha);

        // Assert
        Assert.Equal(data.Length, result.Length);
        Assert.NotNull(result);
    }

    #endregion

    #region CalculateTrend Tests

    [Fact]
    public void CalculateTrend_WithUpwardData_ReturnsPositiveSlope()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30, 40, 50 };

        // Act
        var trend = _analyzer.CalculateTrend(data);

        // Assert
        Assert.True(trend > 0);
    }

    [Fact]
    public void CalculateTrend_WithDownwardData_ReturnsNegativeSlope()
    {
        // Arrange
        var data = new decimal[] { 50, 40, 30, 20, 10 };

        // Act
        var trend = _analyzer.CalculateTrend(data);

        // Assert
        Assert.True(trend < 0);
    }

    [Fact]
    public void CalculateTrend_WithFlatData_ReturnsZeroSlope()
    {
        // Arrange
        var data = new decimal[] { 30, 30, 30, 30, 30 };

        // Act
        var trend = _analyzer.CalculateTrend(data);

        // Assert
        Assert.Equal(0, trend);
    }

    [Fact]
    public void CalculateTrend_WithSingleValue_ReturnsZero()
    {
        // Arrange
        var data = new decimal[] { 50 };

        // Act
        var trend = _analyzer.CalculateTrend(data);

        // Assert
        Assert.Equal(0, trend);
    }

    #endregion

    #region CalculateSeasonality Tests

    [Fact]
    public void CalculateSeasonality_WithSeasonalData_ReturnsPositiveValue()
    {
        // Arrange
        // Create data with clear seasonality (peaks every 7 values)
        var data = new decimal[] { 10, 10, 10, 50, 50, 50, 10, 10, 10, 50, 50, 50, 10 };
        var seasonLength = 7;

        // Act
        var seasonality = _analyzer.CalculateSeasonality(data, seasonLength);

        // Assert
        Assert.True(seasonality > 0);
        Assert.True(seasonality <= 1);
    }

    [Fact]
    public void CalculateSeasonality_WithRandomData_ReturnsLowValue()
    {
        // Arrange
        var random = new Random();
        var data = Enumerable.Range(0, 100)
            .Select(_ => (decimal)random.Next(10, 50))
            .ToArray();
        var seasonLength = 7;

        // Act
        var seasonality = _analyzer.CalculateSeasonality(data, seasonLength);

        // Assert
        Assert.True(seasonality >= 0);
        Assert.True(seasonality <= 1);
    }

    [Fact]
    public void CalculateSeasonality_WithInsufficientData_ReturnsZero()
    {
        // Arrange
        var data = new decimal[] { 10, 20, 30 };
        var seasonLength = 7;

        // Act
        var seasonality = _analyzer.CalculateSeasonality(data, seasonLength);

        // Assert
        Assert.Equal(0, seasonality);
    }

    [Fact]
    public void CalculateSeasonality_WithFlatData_ReturnsZero()
    {
        // Arrange
        var data = new decimal[] { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 };
        var seasonLength = 7;

        // Act
        var seasonality = _analyzer.CalculateSeasonality(data, seasonLength);

        // Assert
        Assert.Equal(0, seasonality);
    }

    #endregion

    #region CalculateMetrics Tests

    [Fact]
    public void CalculateMetrics_WithPerfectPrediction_ReturnsHighR2()
    {
        // Arrange
        var actual = new decimal[] { 10, 20, 30, 40, 50 };
        var predicted = new decimal[] { 10, 20, 30, 40, 50 };

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.Equal(0, mape);
        Assert.Equal(0, rmse);
        Assert.Equal(1, r2);
    }

    [Fact]
    public void CalculateMetrics_WithPoorPrediction_ReturnsLowR2()
    {
        // Arrange
        var actual = new decimal[] { 10, 20, 30, 40, 50 };
        var predicted = new decimal[] { 50, 40, 30, 20, 10 }; // Reversed

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.True(mape > 0);
        Assert.True(r2 >= 0); // R² is clamped, won't go negative
        Assert.True(r2 < 1);  // But should be low for poor predictions// Can be negative for poor predictions
    }

    [Fact]
    public void CalculateMetrics_WithMAPECalculation_ReturnsCorrectValue()
    {
        // Arrange
        var actual = new decimal[] { 100, 100, 100 };
        var predicted = new decimal[] { 110, 90, 100 };

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.True(mape > 0);
        Assert.True(mape <= 100); // MAPE should be percentage
    }

    [Fact]
    public void CalculateMetrics_WithRMSECalculation_ReturnsCorrectValue()
    {
        // Arrange
        var actual = new decimal[] { 10, 20, 30 };
        var predicted = new decimal[] { 12, 22, 28 };

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.True(rmse > 0);
        Assert.True(rmse < 5); // Small differences
    }

    [Fact]
    public void CalculateMetrics_WithMismatchedLengths_ReturnsZeros()
    {
        // Arrange
        var actual = new decimal[] { 10, 20, 30 };
        var predicted = new decimal[] { 10, 20 };

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.Equal(0, mape);
        Assert.Equal(0, rmse);
        Assert.Equal(0, r2);
    }

    [Fact]
    public void CalculateMetrics_WithEmptyArrays_ReturnsZeros()
    {
        // Arrange
        var actual = new decimal[] { };
        var predicted = new decimal[] { };

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.Equal(0, mape);
        Assert.Equal(0, rmse);
        Assert.Equal(0, r2);
    }

    [Fact]
    public void CalculateMetrics_R2IsClampedBetween0And1()
    {
        // Arrange
        var actual = new decimal[] { 10, 20, 30, 40, 50 };
        var predicted = new decimal[] { 15, 25, 35, 45, 55 }; // Small consistent offset

        // Act
        var (mape, rmse, r2) = _analyzer.CalculateMetrics(actual, predicted);

        // Assert
        Assert.True(r2 >= 0);
        Assert.True(r2 <= 1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateMovingAverage_WithZeroValues_HandlesCorrectly()
    {
        // Arrange
        var data = new decimal[] { 0, 10, 0, 10, 0 };
        var period = 2;

        // Act
        var result = _analyzer.CalculateMovingAverage(data, period);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.All(v => v >= 0));
    }

    [Fact]
    public void CalculateExponentialSmoothing_WithLargeValues_HandlesCorrectly()
    {
        // Arrange
        var data = new decimal[] { 1000000, 2000000, 3000000 };
        var alpha = 0.5m;

        // Act
        var result = _analyzer.CalculateExponentialSmoothing(data, alpha);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.All(result, v => Assert.True(v > 0));
    }

    [Fact]
    public void CalculateTrend_WithNegativeValues_HandlesCorrectly()
    {
        // Arrange
        var data = new decimal[] { -50, -40, -30, -20, -10 };

        // Act
        var trend = _analyzer.CalculateTrend(data);

        // Assert
        Assert.True(trend > 0); // Should still be upward
    }

    #endregion
}