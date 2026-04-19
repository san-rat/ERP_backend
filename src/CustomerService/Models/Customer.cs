using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerService.Models
{
    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [NotMapped]
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

        public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
        public CustomerCart? Cart { get; set; }
        public ICollection<CustomerOrderReference> OrderReferences { get; set; } = new List<CustomerOrderReference>();
    }
}
