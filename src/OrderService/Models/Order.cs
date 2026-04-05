using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public class Order
    {
        public int Id { get; set; }

        // External order reference from e-commerce app
        [Required]
        [MaxLength(100)]
        public string ExternalOrderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CustomerId { get; set; } = string.Empty;

        // Simple total amount for beginner-friendly version
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Created;

        // Used only when order is cancelled
        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}