using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        [MaxLength(100)]
        public string ExternalOrderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CustomerId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "TotalAmount must be greater than 0")]
        public decimal TotalAmount { get; set; }
    }
}