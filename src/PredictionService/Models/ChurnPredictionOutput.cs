namespace PredictionService.Models;

public class ChurnPredictionOutput
{
    public Guid PredictionId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal ChurnProbability { get; set; }
    public string ChurnRiskLabel { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public List<ChurnFactor> TopFactors { get; set; } = new();
    public DateTime PredictedAt { get; set; }
}