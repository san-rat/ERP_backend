using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class EcommerceCreateOrderDto
    {
        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExternalOrderId { get; set; } = string.Empty;

        [Required]
        public IReadOnlyList<EcommerceOrderItemRequestDto> Items { get; set; } = Array.Empty<EcommerceOrderItemRequestDto>();

        [Required]
        public ShippingAddressSnapshotDto ShippingAddressSnapshot { get; set; } = new();

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class EcommerceOrderItemRequestDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    public class ShippingAddressSnapshotDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        [Required]
        public string City { get; set; } = string.Empty;

        public string? State { get; set; }

        public string? PostalCode { get; set; }

        [Required]
        public string Country { get; set; } = string.Empty;
    }

    public class EcommerceOrderItemResponseDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class EcommerceOrderResponseDto
    {
        public Guid Id { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ShippingAddressSnapshotDto ShippingAddress { get; set; } = new();
        public IReadOnlyList<EcommerceOrderItemResponseDto> Items { get; set; } = Array.Empty<EcommerceOrderItemResponseDto>();
    }

    public class CancelEcommerceOrderDto
    {
        public string? Reason { get; set; }
    }
}
