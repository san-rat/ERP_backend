namespace PredictionService.Models;

public class ChurnFactor
{
    public string FactorName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string FeatureValue { get; set; } = string.Empty;
    public decimal ContributionPercentage { get; set; }
}