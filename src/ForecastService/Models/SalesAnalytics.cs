namespace ForecastService.Models
{
    public class SalesAnalytics
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        
        public decimal AvgDailySales { get; set; }
        public decimal MaxDailySales { get; set; }
        public decimal MinDailySales { get; set; }
        public decimal StandardDeviation { get; set; }
        
        public string Trend { get; set; } = "STABLE";
        public decimal GrowthRate { get; set; }
        
        public string SeasonalPattern { get; set; } = "NON_SEASONAL";
        public List<int> PeakDays { get; set; } = new();
        
        public int DaysWithData { get; set; }
        public DateTime FirstSaleDate { get; set; }
        public DateTime LastSaleDate { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}