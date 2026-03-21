namespace ForecastService.Models
{
    public class ForecastResult
    {
        public Guid ForecastId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public List<DailyForecast> Forecasts { get; set; } = new();
        
        public string Algorithm { get; set; } = string.Empty;
        public decimal MAPE { get; set; }
        public decimal RMSE { get; set; }
        public decimal R_Squared { get; set; }
        
        public DateTime GeneratedAt { get; set; }
        public int DaysForecasted { get; set; }
        public DateTime LastHistoricalDate { get; set; }
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public decimal ForecastedUnits { get; set; }
        public decimal ForecastedRevenue { get; set; }
        public ConfidenceInterval? Confidence { get; set; }
        public string Confidence_Level { get; set; } = "95%";
    }

    public class ConfidenceInterval
    {
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
        public decimal PointEstimate { get; set; }
    }
}