using System;
using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class ReleaseStockDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
