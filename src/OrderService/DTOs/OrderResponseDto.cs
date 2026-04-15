namespace OrderService.DTOs
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
