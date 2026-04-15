using PredictionService.Models;

namespace PredictionService.ML;

/// <summary>
/// Normalizes customer features to 0-1 range for ML model
/// </summary>
public class FeatureNormalizer
{
    private readonly Dictionary<string, (decimal min, decimal max)> _ranges = new();

    public FeatureNormalizer()
    {
        InitializeRanges();
    }

    /// <summary>
    /// Initialize typical ranges based on business logic
    /// </summary>
    private void InitializeRanges()
    {
        _ranges["Recency"] = (0, 365);
        _ranges["Frequency"] = (0, 100);
        _ranges["MonetaryValue"] = (0, 100000);
        _ranges["AvgOrderValue"] = (0, 10000);
        _ranges["TenureDays"] = (0, 3650);
        _ranges["ProductDiversity"] = (0, 50);
        _ranges["ReturnCount"] = (0, 20);
        _ranges["ReturnRate"] = (0, 1);
        _ranges["CompletedOrders"] = (0, 100);
        _ranges["CancelledOrders"] = (0, 50);
        _ranges["CancellationRate"] = (0, 1);
        _ranges["InactiveFlag"] = (0, 1);
    }

    /// <summary>
    /// Normalize all customer features to 0-1 range
    /// </summary>
    public float[] NormalizeFeatures(CustomerFeatures features)
    {
        var normalized = new[]
        {
            Normalize(features.Recency, "Recency"),
            Normalize(features.Frequency, "Frequency"),
            Normalize((float)features.MonetaryValue, "MonetaryValue"),
            Normalize((float)features.AvgOrderValue, "AvgOrderValue"),
            Normalize(features.TenureDays, "TenureDays"),
            Normalize(features.ProductDiversity, "ProductDiversity"),
            Normalize(features.ReturnCount, "ReturnCount"),
            Normalize((float)features.ReturnRate, "ReturnRate"),
            Normalize((float)features.CancellationRate, "CancellationRate"),
            Normalize(features.CompletedOrders, "CompletedOrders"),
            Normalize(features.CancelledOrders, "CancelledOrders"),
            Normalize(features.InactiveFlag, "InactiveFlag")
        };

        return normalized;
    }

    /// <summary>
    /// Normalize a single value to 0-1 range using min-max normalization
    /// </summary>
    private float Normalize(float value, string featureName)
    {
        if (!_ranges.TryGetValue(featureName, out var range))
            return 0f;

        var (min, max) = range;
        if (max - min == 0) return 0f;

        var normalized = (value - (float)min) / ((float)max - (float)min);
        return Math.Max(0f, Math.Min(1f, normalized));
    }
}

