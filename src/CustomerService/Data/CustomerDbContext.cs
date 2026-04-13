using CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Data
{
    public class CustomerDbContext : DbContext
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
        public DbSet<CustomerOrderReference> CustomerOrderReferences => Set<CustomerOrderReference>();
        public DbSet<CustomerOrderReferenceItem> CustomerOrderReferenceItems => Set<CustomerOrderReferenceItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<CustomerAddress>()
                .HasOne(a => a.Customer)
                .WithMany(c => c.Addresses)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerOrderReference>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.OrderReferences)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerOrderReferenceItem>()
                .HasOne(i => i.CustomerOrderReference)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.CustomerOrderReferenceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}