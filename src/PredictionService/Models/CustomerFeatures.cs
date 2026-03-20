namespace PredictionService.Models;

public class CustomerFeatures
{
    public Guid CustomerId { get; set; }
    
    // RFM Features
    public int Recency { get; set; }
    public int Frequency { get; set; }
    public decimal MonetaryValue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int TenureDays { get; set; }
    
    // Product Diversity
    public int ProductDiversity { get; set; }
    public int CategoryDiversity { get; set; }
    public decimal AvgProductsPerOrder { get; set; }
    
    // Return Behavior
    public int ReturnCount { get; set; }
    public decimal ReturnRate { get; set; }
    public decimal TotalRefunded { get; set; }
    
    // Order Status
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CancellationRate { get; set; }
    
    // Engagement
    public int AccountAgeDays { get; set; }
    public int DaysSinceActivity { get; set; }
    public int InactiveFlag { get; set; }  // 0 or 1 (int, not bool)
}