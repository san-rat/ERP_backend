namespace ForecastService.Models
{
    public class ProductMetrics
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal CurrentPrice { get; set; }
        
        public int TotalUnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgUnitPrice { get; set; }
        public int OrderCount { get; set; }
        
        public decimal TrendDirection { get; set; }
        public decimal Volatility { get; set; }
        public decimal SeasonalityIndex { get; set; }
    }
}