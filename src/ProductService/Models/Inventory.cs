using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    public class Inventory
    {
        [Required]
        [Key]
        public Guid ProductId { get; set; }

        public int QuantityAvailable { get; set; } = 0;

        public int QuantityReserved { get; set; } = 0;

        public int LowStockThreshold { get; set; } = 10;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [NotMapped]
        public bool IsLowStock => QuantityAvailable <= LowStockThreshold;
    }
}
