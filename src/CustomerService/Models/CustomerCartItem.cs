namespace CustomerService.Models
{
    public class CustomerCartItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerCartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public CustomerCart? CustomerCart { get; set; }
    }
}
