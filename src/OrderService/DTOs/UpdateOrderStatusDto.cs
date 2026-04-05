using System.ComponentModel.DataAnnotations;
using OrderService.Models;

namespace OrderService.DTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus NewStatus { get; set; }

        // Required only when cancelling
        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}