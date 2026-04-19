namespace CustomerService.DTOs.Orders
{
    public class CheckoutRequestDto
    {
        public Guid AddressId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CancelCustomerOrderRequestDto
    {
        public string? Reason { get; set; }
    }

    public class CommerceShippingAddressDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string Country { get; set; } = string.Empty;
    }

    public class CommerceOrderLineRequestDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CommerceCreateOrderRequestDto
    {
        public Guid CustomerId { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
        public IReadOnlyList<CommerceOrderLineRequestDto> Items { get; set; } = Array.Empty<CommerceOrderLineRequestDto>();
        public CommerceShippingAddressDto ShippingAddressSnapshot { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CommerceOrderItemResponseDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CommerceOrderResponseDto
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
        public CommerceShippingAddressDto ShippingAddress { get; set; } = new();
        public IReadOnlyList<CommerceOrderItemResponseDto> Items { get; set; } = Array.Empty<CommerceOrderItemResponseDto>();
    }
}
