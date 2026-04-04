using Microsoft.AspNetCore.Mvc;
using Moq;
using PredictionService.Controllers;
using PredictionService.Models;
using PredictionService.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace PredictionService.Tests.Controllers;

public class ModelManagementControllerTests
{
    private readonly Mock<IModelRetrainingService> _mockRetrainingService;
    private readonly Mock<ILogger<ModelManagementController>> _mockLogger;
    private readonly ModelManagementController _controller;

    public ModelManagementControllerTests()
    {
        _mockRetrainingService = new Mock<IModelRetrainingService>();
        _mockLogger = new Mock<ILogger<ModelManagementController>>();
        _controller = new ModelManagementController(_mockRetrainingService.Object, _mockLogger.Object);
    }

    #region RetrainModel Tests

    [Fact]
    public async Task RetrainModel_WithSuccessfulTraining_ReturnsOkResult()
    {
        // Arrange
        var response = new ModelRetrainingResponse
        {
            Success = true,
            Message = "✅ Model trained successfully with 1000 records",
            Status = "COMPLETED",
            TrainingHistoryId = Guid.NewGuid(),
            RecordsUsed = 1000,
            Accuracy = 0.85m,
            DurationSeconds = 300
        };

        _mockRetrainingService
            .Setup(s => s.RetrainModelAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RetrainModel();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedResponse = Assert.IsType<ModelRetrainingResponse>(okResult.Value);
        Assert.True(returnedResponse.Success);
        Assert.Equal("COMPLETED", returnedResponse.Status);
    }

    [Fact]
    public async Task RetrainModel_CallsRetrainingService()
    {
        // Arrange
        var response = new ModelRetrainingResponse
        {
            Success = true,
            Status = "COMPLETED",
            RecordsUsed = 1000
        };

        _mockRetrainingService
            .Setup(s => s.RetrainModelAsync())
            .ReturnsAsync(response);

        // Act
        await _controller.RetrainModel();

        // Assert
        _mockRetrainingService.Verify(s => s.RetrainModelAsync(), Times.Once);
    }

    [Fact]
    public async Task RetrainModel_WithFailedTraining_ReturnsFalseResponse()
    {
        // Arrange
        var response = new ModelRetrainingResponse
        {
            Success = false,
            Message = "Insufficient training data",
            Status = "FAILED"
        };

        _mockRetrainingService
            .Setup(s => s.RetrainModelAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RetrainModel();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ModelRetrainingResponse>(okResult.Value);
        Assert.False(returnedResponse.Success);
        Assert.Equal("FAILED", returnedResponse.Status);
    }

    [Fact]
    public async Task RetrainModel_IncludesRecordsUsedInResponse()
    {
        // Arrange
        var recordsUsed = 1500;
        var response = new ModelRetrainingResponse
        {
            Success = true,
            Status = "COMPLETED",
            RecordsUsed = recordsUsed,
            Accuracy = 0.87m
        };

        _mockRetrainingService
            .Setup(s => s.RetrainModelAsync())
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RetrainModel();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ModelRetrainingResponse>(okResult.Value);
        Assert.Equal(recordsUsed, returnedResponse.RecordsUsed);
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_ReturnsOkResult()
    {
        // Arrange
        var status = new TrainingStatusResponse
        {
            CurrentStatus = "IDLE",
            CurrentModel = "v20240101_120000",
            LastTrainingDate = DateTime.UtcNow.AddDays(-1),
            LastTrainingRecordCount = 1000,
            IsTrainingInProgress = false
        };

        _mockRetrainingService
            .Setup(s => s.GetTrainingStatusAsync())
            .ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetStatus_IncludesCurrentModel()
    {
        // Arrange
        var modelVersion = "v20240108_090000";
        var status = new TrainingStatusResponse
        {
            CurrentStatus = "IDLE",
            CurrentModel = modelVersion,
            IsTrainingInProgress = false
        };

        _mockRetrainingService
            .Setup(s => s.GetTrainingStatusAsync())
            .ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStatus = Assert.IsType<TrainingStatusResponse>(okResult.Value);
        Assert.Equal(modelVersion, returnedStatus.CurrentModel);
    }

    [Fact]
    public async Task GetStatus_IndicatesIfTrainingInProgress()
    {
        // Arrange
        var status = new TrainingStatusResponse
        {
            CurrentStatus = "TRAINING_IN_PROGRESS",
            CurrentModel = "v20240101_120000",
            IsTrainingInProgress = true
        };

        _mockRetrainingService
            .Setup(s => s.GetTrainingStatusAsync())
            .ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStatus = Assert.IsType<TrainingStatusResponse>(okResult.Value);
        Assert.True(returnedStatus.IsTrainingInProgress);
    }

    [Fact]
    public async Task GetStatus_CallsRetrainingService()
    {
        // Arrange
        var status = new TrainingStatusResponse { CurrentStatus = "IDLE" };

        _mockRetrainingService
            .Setup(s => s.GetTrainingStatusAsync())
            .ReturnsAsync(status);

        // Act
        await _controller.GetStatus();

        // Assert
        _mockRetrainingService.Verify(s => s.GetTrainingStatusAsync(), Times.Once);
    }

    #endregion

    #region GetMetrics Tests

    [Fact]
    public async Task GetMetrics_ReturnsOkResult()
    {
        // Arrange
        var metrics = new ModelMetricsResponse
        {
            CurrentModelVersion = "v20240101_120000",
            TrainingDate = DateTime.UtcNow.AddDays(-1),
            TrainingDataCount = 1000,
            Accuracy = 0.85m,
            Precision = 0.82m,
            Recall = 0.88m,
            AucRoc = 0.90m
        };

        _mockRetrainingService
            .Setup(s => s.GetModelMetricsAsync())
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_IncludesAccuracy()
    {
        // Arrange
        var accuracy = 0.87m;
        var metrics = new ModelMetricsResponse
        {
            CurrentModelVersion = "v20240101_120000",
            Accuracy = accuracy,
            Precision = 0.85m,
            Recall = 0.89m,
            AucRoc = 0.92m
        };

        _mockRetrainingService
            .Setup(s => s.GetModelMetricsAsync())
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMetrics = Assert.IsType<ModelMetricsResponse>(okResult.Value);
        Assert.Equal(accuracy, returnedMetrics.Accuracy);
    }

    [Fact]
    public async Task GetMetrics_IncludesAllMetrics()
    {
        // Arrange
        var metrics = new ModelMetricsResponse
        {
            CurrentModelVersion = "v20240101_120000",
            TrainingDate = DateTime.UtcNow.AddDays(-1),
            TrainingDataCount = 1000,
            Accuracy = 0.85m,
            Precision = 0.82m,
            Recall = 0.88m,
            AucRoc = 0.90m
        };

        _mockRetrainingService
            .Setup(s => s.GetModelMetricsAsync())
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMetrics = Assert.IsType<ModelMetricsResponse>(okResult.Value);
        Assert.NotNull(returnedMetrics.Accuracy);
        Assert.NotNull(returnedMetrics.Precision);
        Assert.NotNull(returnedMetrics.Recall);
        Assert.NotNull(returnedMetrics.AucRoc);
    }

    #endregion

    #region GetHistory Tests

    [Fact]
    public async Task GetHistory_ReturnsOkResult()
    {
        // Arrange
        var history = new List<TrainingHistoryResponse>
        {
            new TrainingHistoryResponse
            {
                Id = Guid.NewGuid(),
                TrainingDate = DateTime.UtcNow.AddDays(-1),
                Status = "COMPLETED",
                RecordCount = 1000,
                ChurnedCount = 250,
                ActiveCount = 750,
                TriggeredBy = "MANUAL",
                DurationSeconds = 300
            }
        };

        _mockRetrainingService
            .Setup(s => s.GetTrainingHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.GetHistory(30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WithCustomDayParameter_PassesParameterToService()
    {
        // Arrange
        _mockRetrainingService
            .Setup(s => s.GetTrainingHistoryAsync(60))
            .ReturnsAsync(new List<TrainingHistoryResponse>());

        // Act
        await _controller.GetHistory(60);

        // Assert
        _mockRetrainingService.Verify(s => s.GetTrainingHistoryAsync(60), Times.Once);
    }

    [Fact]
    public async Task GetHistory_WithDefaultParameter_UsesThirty()
    {
        // Arrange
        _mockRetrainingService
            .Setup(s => s.GetTrainingHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<TrainingHistoryResponse>());

        // Act
        await _controller.GetHistory();

        // Assert
        _mockRetrainingService.Verify(s => s.GetTrainingHistoryAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetHistory_ReturnsMultipleHistoryEntries()
    {
        // Arrange
        var history = new List<TrainingHistoryResponse>
        {
            new TrainingHistoryResponse
            {
                Id = Guid.NewGuid(),
                TrainingDate = DateTime.UtcNow.AddDays(-1),
                Status = "COMPLETED",
                RecordCount = 1000
            },
            new TrainingHistoryResponse
            {
                Id = Guid.NewGuid(),
                TrainingDate = DateTime.UtcNow.AddDays(-7),
                Status = "COMPLETED",
                RecordCount = 950
            }
        };

        _mockRetrainingService
            .Setup(s => s.GetTrainingHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.GetHistory(30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedHistory = Assert.IsType<List<TrainingHistoryResponse>>(okResult.Value);
        Assert.Equal(2, returnedHistory.Count);
    }

    #endregion

    #region GetVersions Tests

    [Fact]
    public async Task GetVersions_ReturnsOkResult()
    {
        // Arrange
        var versions = new List<ModelVersionResponse>
        {
            new ModelVersionResponse
            {
                Id = Guid.NewGuid(),
                Version = "v20240101_120000",
                TrainingDate = DateTime.UtcNow.AddDays(-1),
                RecordCount = 1000,
                Accuracy = 0.85m,
                IsActive = true
            }
        };

        _mockRetrainingService
            .Setup(s => s.GetAllModelVersionsAsync())
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetVersions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetVersions_ReturnsMultipleVersions()
    {
        // Arrange
        var versions = new List<ModelVersionResponse>
        {
            new ModelVersionResponse
            {
                Id = Guid.NewGuid(),
                Version = "v20240101_120000",
                TrainingDate = DateTime.UtcNow.AddDays(-7),
                RecordCount = 1000,
                Accuracy = 0.85m,
                IsActive = false
            },
            new ModelVersionResponse
            {
                Id = Guid.NewGuid(),
                Version = "v20240108_090000",
                TrainingDate = DateTime.UtcNow.AddDays(-1),
                RecordCount = 1050,
                Accuracy = 0.87m,
                IsActive = true
            }
        };

        _mockRetrainingService
            .Setup(s => s.GetAllModelVersionsAsync())
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetVersions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedVersions = Assert.IsType<List<ModelVersionResponse>>(okResult.Value);
        Assert.Equal(2, returnedVersions.Count);
    }

    [Fact]
    public async Task GetVersions_IdentifiesActiveVersion()
    {
        // Arrange
        var versions = new List<ModelVersionResponse>
        {
            new ModelVersionResponse
            {
                Id = Guid.NewGuid(),
                Version = "v20240108_090000",
                IsActive = true
            }
        };

        _mockRetrainingService
            .Setup(s => s.GetAllModelVersionsAsync())
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.GetVersions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedVersions = Assert.IsType<List<ModelVersionResponse>>(okResult.Value);
        Assert.Single(returnedVersions);
        Assert.True(returnedVersions[0].IsActive);
    }

    [Fact]
    public async Task GetVersions_CallsRetrainingService()
    {
        // Arrange
        _mockRetrainingService
            .Setup(s => s.GetAllModelVersionsAsync())
            .ReturnsAsync(new List<ModelVersionResponse>());

        // Act
        await _controller.GetVersions();

        // Assert
        _mockRetrainingService.Verify(s => s.GetAllModelVersionsAsync(), Times.Once);
    }

    #endregion
}