using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        
        public bool IsActive { get; set; } = true;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int QuantityAvailable { get; set; }
        
        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 10;
    }
}
