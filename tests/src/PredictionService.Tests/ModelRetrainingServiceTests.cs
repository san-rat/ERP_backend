using Moq;
using PredictionService.Models;
using PredictionService.Repositories;
using PredictionService.Services;
using PredictionService.ML;
using Xunit;
using Microsoft.Extensions.Logging;

namespace PredictionService.Tests.Services;

public class ModelRetrainingServiceTests
{
    private readonly Mock<ChurnModelManager> _mockModelManager;
    private readonly Mock<ITrainingDataRepository> _mockTrainingDataRepository;
    private readonly Mock<ILogger<ModelRetrainingService>> _mockLogger;
    private readonly ModelRetrainingService _service;

    public ModelRetrainingServiceTests()
    {
        _mockModelManager = new Mock<ChurnModelManager>(
            new Mock<ILogger<ChurnModelManager>>().Object,
            new Mock<ITrainingDataRepository>().Object
        );
        _mockTrainingDataRepository = new Mock<ITrainingDataRepository>();
        _mockLogger = new Mock<ILogger<ModelRetrainingService>>();
        _service = new ModelRetrainingService(
            _mockModelManager.Object,
            _mockTrainingDataRepository.Object,
            _mockLogger.Object
        );
    }

    #region RetrainModelAsync Tests

    [Fact]
    public async Task RetrainModelAsync_WithSuccessfulTraining_ReturnsSuccessResponse()
    {
        // Arrange
        var trainingResult = new ModelTrainingResult
        {
            Status = "COMPLETED",
            TrainingStartTime = DateTime.UtcNow.AddMinutes(-5),
            TrainingEndTime = DateTime.UtcNow,
            TotalRecordsUsed = 1000,
            ChurnedCount = 250,
            NonChurnedCount = 750,
            Accuracy = 0.85m,
            Precision = 0.82m,
            Recall = 0.88m,
            AucRoc = 0.90m
        };

        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .ReturnsAsync(trainingResult);

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        _mockTrainingDataRepository
            .Setup(r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()))
            .ReturnsAsync(Guid.NewGuid());

        _mockTrainingDataRepository
            .Setup(r => r.SetActiveModelAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RetrainModelAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("COMPLETED", result.Status);
        Assert.Equal(1000, result.RecordsUsed);
        Assert.Equal(0.85m, result.Accuracy);
    }

    [Fact]
    public async Task RetrainModelAsync_WhenAlreadyTraining_ReturnsInProgressResponse()
    {
        // Arrange
        var trainingResult = new ModelTrainingResult
        {
            Status = "COMPLETED",
            TrainingStartTime = DateTime.UtcNow.AddMinutes(-5),
            TrainingEndTime = DateTime.UtcNow,
            TotalRecordsUsed = 1000,
            Accuracy = 0.85m
        };

        // Setup the mock to return a delayed result to simulate ongoing training
        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .Returns(async () =>
            {
                // Simulate a training operation that takes time
                await Task.Delay(1000);
                return trainingResult;
            });

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        _mockTrainingDataRepository
            .Setup(r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()))
            .ReturnsAsync(Guid.NewGuid());

        _mockTrainingDataRepository
            .Setup(r => r.SetActiveModelAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Start first training (don't await yet)
        var task1 = _service.RetrainModelAsync();

        // Give it a moment to start
        await Task.Delay(100);

        // Try to start second training immediately (while first is still running)
        var result = await _service.RetrainModelAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("IN_PROGRESS", result.Status);
        Assert.Contains("already in progress", result.Message);

        // Clean up - wait for the first task to complete
        await task1;
    }

    [Fact]
    public async Task RetrainModelAsync_WithFailedTraining_ReturnsFailureResponse()
    {
        // Arrange
        var trainingResult = new ModelTrainingResult
        {
            Status = "FAILED",
            ErrorMessage = "Insufficient training data",
            TrainingStartTime = DateTime.UtcNow.AddMinutes(-1),
            TrainingEndTime = DateTime.UtcNow
        };

        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .ReturnsAsync(trainingResult);

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RetrainModelAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("FAILED", result.Status);
        Assert.Contains("Insufficient training data", result.Message);
    }

    [Fact]
    public async Task RetrainModelAsync_SavesTrainingHistory()
    {
        // Arrange
        var trainingResult = new ModelTrainingResult
        {
            Status = "COMPLETED",
            TrainingStartTime = DateTime.UtcNow.AddMinutes(-5),
            TrainingEndTime = DateTime.UtcNow,
            TotalRecordsUsed = 1000,
            ChurnedCount = 250,
            NonChurnedCount = 750,
            Accuracy = 0.85m
        };

        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .ReturnsAsync(trainingResult);

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        _mockTrainingDataRepository
            .Setup(r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()))
            .ReturnsAsync(Guid.NewGuid());

        _mockTrainingDataRepository
            .Setup(r => r.SetActiveModelAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        await _service.RetrainModelAsync();

        // Assert
        _mockTrainingDataRepository.Verify(
            r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task RetrainModelAsync_SavesModelVersion()
    {
        // Arrange
        var trainingResult = new ModelTrainingResult
        {
            Status = "COMPLETED",
            TrainingStartTime = DateTime.UtcNow.AddMinutes(-5),
            TrainingEndTime = DateTime.UtcNow,
            TotalRecordsUsed = 1000,
            Accuracy = 0.85m,
            Precision = 0.82m,
            Recall = 0.88m,
            AucRoc = 0.90m
        };

        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .ReturnsAsync(trainingResult);

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        _mockTrainingDataRepository
            .Setup(r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()))
            .ReturnsAsync(Guid.NewGuid());

        _mockTrainingDataRepository
            .Setup(r => r.SetActiveModelAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        await _service.RetrainModelAsync();

        // Assert
        _mockTrainingDataRepository.Verify(
            r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RetrainModelAsync_CalculatesDurationCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddSeconds(-120);
        var endTime = DateTime.UtcNow;

        var trainingResult = new ModelTrainingResult
        {
            Status = "COMPLETED",
            TrainingStartTime = startTime,
            TrainingEndTime = endTime,
            TotalRecordsUsed = 1000,
            Accuracy = 0.85m
        };

        _mockModelManager
            .Setup(m => m.TrainModelWithRealDataAsync())
            .ReturnsAsync(trainingResult);

        _mockTrainingDataRepository
            .Setup(r => r.SaveTrainingHistoryAsync(It.IsAny<TrainingHistory>()))
            .Returns(Task.CompletedTask);

        _mockTrainingDataRepository
            .Setup(r => r.SaveModelVersionAsync(It.IsAny<ModelVersionInfo>()))
            .ReturnsAsync(Guid.NewGuid());

        _mockTrainingDataRepository
            .Setup(r => r.SetActiveModelAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RetrainModelAsync();

        // Assert
        Assert.True(result.DurationSeconds >= 120);
    }

    #endregion

    #region GetTrainingStatusAsync Tests

    [Fact]
    public async Task GetTrainingStatusAsync_WithActiveModel_ReturnsModelInfo()
    {
        // Arrange
        var activeModel = new ModelVersionInfo
        {
            ModelVersion = "v20240101_120000",
            TrainingDate = DateTime.UtcNow.AddDays(-1),
            TrainingDataCount = 1000
        };

        _mockTrainingDataRepository
            .Setup(r => r.GetActiveModelAsync())
            .ReturnsAsync(activeModel);

        // Act
        var result = await _service.GetTrainingStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("v20240101_120000", result.CurrentModel);
        Assert.Equal(1000, result.LastTrainingRecordCount);
    }

    [Fact]
    public async Task GetTrainingStatusAsync_WithoutActiveModel_ReturnsNullModel()
    {
        // Arrange
        _mockTrainingDataRepository
            .Setup(r => r.GetActiveModelAsync())
            .ReturnsAsync((ModelVersionInfo?)null);

        // Act
        var result = await _service.GetTrainingStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CurrentModel);
    }

    #endregion

    #region GetTrainingHistoryAsync Tests

    [Fact]
    public async Task GetTrainingHistoryAsync_ReturnsHistoryForPeriod()
    {
        // Arrange
        var history = new List<TrainingHistory>
        {
            new TrainingHistory
            {
                Id = Guid.NewGuid(),
                TrainingStartTime = DateTime.UtcNow.AddDays(-1),
                TrainingEndTime = DateTime.UtcNow.AddDays(-1).AddHours(1),
                TrainingStatus = "COMPLETED",
                TotalRecordsUsed = 1000,
                ChurnedCount = 250,
                TriggeredBy = "MANUAL"
            },
            new TrainingHistory
            {
                Id = Guid.NewGuid(),
                TrainingStartTime = DateTime.UtcNow.AddDays(-7),
                TrainingEndTime = DateTime.UtcNow.AddDays(-7).AddHours(1),
                TrainingStatus = "COMPLETED",
                TotalRecordsUsed = 950,
                ChurnedCount = 240,
                TriggeredBy = "AUTOMATIC"
            }
        };

        _mockTrainingDataRepository
            .Setup(r => r.GetTrainingHistoryAsync(30))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetTrainingHistoryAsync(30);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, h => Assert.NotEqual(Guid.Empty, h.Id));
    }

    [Fact]
    public async Task GetTrainingHistoryAsync_CalculatesDurationForEachEntry()
    {
        // Arrange
        var history = new List<TrainingHistory>
        {
            new TrainingHistory
            {
                Id = Guid.NewGuid(),
                TrainingStartTime = DateTime.UtcNow.AddHours(-2),
                TrainingEndTime = DateTime.UtcNow.AddHours(-1),
                TrainingStatus = "COMPLETED",
                TotalRecordsUsed = 1000,
                ChurnedCount = 250,
                TriggeredBy = "MANUAL"
            }
        };

        _mockTrainingDataRepository
            .Setup(r => r.GetTrainingHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(history);

        // Act
        var result = await _service.GetTrainingHistoryAsync(30);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result[0].DurationSeconds > 3500); // ~1 hour
    }

    #endregion

    #region GetModelMetricsAsync Tests

    [Fact]
    public async Task GetModelMetricsAsync_WithActiveModel_ReturnsMetrics()
    {
        // Arrange
        var activeModel = new ModelVersionInfo
        {
            ModelVersion = "v20240101_120000",
            TrainingDate = DateTime.UtcNow.AddDays(-1),
            TrainingDataCount = 1000,
            Accuracy = 0.85m,
            Precision = 0.82m,
            Recall = 0.88m,
            AucRoc = 0.90m
        };

        _mockTrainingDataRepository
            .Setup(r => r.GetActiveModelAsync())
            .ReturnsAsync(activeModel);

        // Act
        var result = await _service.GetModelMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("v20240101_120000", result.CurrentModelVersion);
        Assert.Equal(0.85m, result.Accuracy);
        Assert.Equal(0.82m, result.Precision);
        Assert.Equal(0.88m, result.Recall);
        Assert.Equal(0.90m, result.AucRoc);
    }

    [Fact]
    public async Task GetModelMetricsAsync_WithoutActiveModel_ReturnsMessageNoActiveModel()
    {
        // Arrange
        _mockTrainingDataRepository
            .Setup(r => r.GetActiveModelAsync())
            .ReturnsAsync((ModelVersionInfo?)null);

        // Act
        var result = await _service.GetModelMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("No active model", result.CurrentModelVersion);
        Assert.Null(result.Accuracy);
    }

    #endregion

    #region GetAllModelVersionsAsync Tests

    [Fact]
    public async Task GetAllModelVersionsAsync_ReturnsAllVersions()
    {
        // Arrange
        var versions = new List<ModelVersionInfo>
        {
            new ModelVersionInfo
            {
                Id = Guid.NewGuid(),
                ModelVersion = "v20240101_120000",
                TrainingDate = DateTime.UtcNow.AddDays(-1),
                TrainingDataCount = 1000,
                Accuracy = 0.85m
            },
            new ModelVersionInfo
            {
                Id = Guid.NewGuid(),
                ModelVersion = "v20240108_090000",
                TrainingDate = DateTime.UtcNow,
                TrainingDataCount = 1050,
                Accuracy = 0.87m
            }
        };

        _mockTrainingDataRepository
            .Setup(r => r.GetAllModelVersionsAsync())
            .ReturnsAsync(versions);

        // Act
        var result = await _service.GetAllModelVersionsAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.NotEqual(Guid.Empty, v.Id));
    }

    #endregion
}