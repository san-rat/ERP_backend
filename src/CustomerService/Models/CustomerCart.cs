namespace CustomerService.Models
{
    public class CustomerCart
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Customer? Customer { get; set; }
        public ICollection<CustomerCartItem> Items { get; set; } = new List<CustomerCartItem>();
    }
}
