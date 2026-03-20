using Microsoft.ML.Data;
using PredictionService.Models;

namespace PredictionService.ML;

/// <summary>
/// ML.NET input class for churn model
/// Maps features to be fed into the machine learning model
/// </summary>
public class ChurnModelInput
{
    [LoadColumn(0)] public float Recency { get; set; }
    [LoadColumn(1)] public float Frequency { get; set; }
    [LoadColumn(2)] public float MonetaryValue { get; set; }
    [LoadColumn(3)] public float AvgOrderValue { get; set; }
    [LoadColumn(4)] public float TenureDays { get; set; }
    [LoadColumn(5)] public float ProductDiversity { get; set; }
    [LoadColumn(6)] public float ReturnCount { get; set; }
    [LoadColumn(7)] public float ReturnRate { get; set; }
    [LoadColumn(8)] public float CancellationRate { get; set; }
    [LoadColumn(9)] public float CompletedOrders { get; set; }
    [LoadColumn(10)] public float CancelledOrders { get; set; }
    [LoadColumn(11)] public float InactiveFlag { get; set; }
}
