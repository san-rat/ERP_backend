namespace ForecastService.Models
{
    public class ForecastRequest
    {
        public Guid ProductId { get; set; }
        public int ForecastDays { get; set; }
        public string Algorithm { get; set; } = "AUTO";
        public bool IncludeConfidenceInterval { get; set; } = true;
        public int ConfidenceLevel { get; set; } = 95;
    }
}