namespace CustomerService.Models
{
    public class CustomerOrderReference
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public Guid ErpOrderId { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public string ShippingFullName { get; set; } = string.Empty;
        public string? ShippingPhone { get; set; }
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Customer? Customer { get; set; }
        public ICollection<CustomerOrderReferenceItem> Items { get; set; } = new List<CustomerOrderReferenceItem>();
    }
}
