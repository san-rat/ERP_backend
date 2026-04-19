namespace CustomerService.Models
{
    public class CustomerOrderReferenceItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerOrderReferenceId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public CustomerOrderReference? CustomerOrderReference { get; set; }
    }
}
