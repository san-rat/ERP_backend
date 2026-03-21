namespace ForecastService.Models
{
    public class SalesData
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public DateTime Date { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int OrderCount { get; set; }
    }
}