using Microsoft.ML.Data;

namespace PredictionService.ML;

/// <summary>
/// ML.NET output class for churn model
/// Contains prediction results
/// </summary>
public class ChurnModelOutput
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }
}
