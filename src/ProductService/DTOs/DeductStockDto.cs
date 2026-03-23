using System;
using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    /// <summary>
    /// Request body for deducting stock when an order is placed.
    /// This is used internally by the ProductService to simulate the order-placed event.
    /// </summary>
    public class DeductStockDto
    {
        /// <summary>The product whose stock should be deducted.</summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>A unique identifier for the order that caused this deduction.</summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>Number of units to deduct from QuantityAvailable.</summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
