using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    public class InventoryReservation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        public int Quantity { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "RESERVED"; // RESERVED, RELEASED, FULFILLED

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
