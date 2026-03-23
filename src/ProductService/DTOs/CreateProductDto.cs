using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new product.
    /// </summary>
    public class CreateProductDto
    {
        /// <summary>Stock Keeping Unit ΓÇö must be unique across all products.</summary>
        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        /// <summary>Display name of the product.</summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional long-form description of the product.</summary>
        public string? Description { get; set; }

        /// <summary>Optional category the product belongs to (FK ΓåÆ Categories.Id).</summary>
        public int? CategoryId { get; set; }

        /// <summary>Unit selling price. Must be greater than 0.</summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        /// <summary>Whether the product is available for ordering. Defaults to true.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Initial stock quantity loaded into Inventory. Defaults to 0.</summary>
        [Range(0, int.MaxValue, ErrorMessage = "Initial stock cannot be negative")]
        public int InitialStock { get; set; } = 0;

        /// <summary>Quantity at which a low-stock alert is raised. Defaults to 10.</summary>
        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 10;
    }
}
