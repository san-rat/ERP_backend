using CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Data
{
    public class CustomerDbContext : DbContext
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<CustomerCart> CustomerCarts { get; set; }
        public DbSet<CustomerCartItem> CustomerCartItems { get; set; }
        public DbSet<CustomerOrderReference> CustomerOrderReferences { get; set; }
        public DbSet<CustomerOrderReferenceItem> CustomerOrderReferenceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("customers", "dbo");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Email).IsUnique();

                entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(c => c.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
                entity.Property(c => c.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
                entity.Property(c => c.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
                entity.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(c => c.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(c => c.CreatedAt).HasColumnName("created_at");
                entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<CustomerAddress>(entity =>
            {
                entity.ToTable("customer_addresses", "dbo");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(a => a.CustomerId).HasColumnName("customer_id");
                entity.Property(a => a.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(150);
                entity.Property(a => a.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(a => a.AddressLine1).HasColumnName("address_line_1").IsRequired().HasMaxLength(255);
                entity.Property(a => a.AddressLine2).HasColumnName("address_line_2").HasMaxLength(255);
                entity.Property(a => a.City).HasColumnName("city").IsRequired().HasMaxLength(100);
                entity.Property(a => a.State).HasColumnName("state").HasMaxLength(100);
                entity.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(30);
                entity.Property(a => a.Country).HasColumnName("country").IsRequired().HasMaxLength(100);
                entity.Property(a => a.IsDefault).HasColumnName("is_default");
                entity.Property(a => a.CreatedAt).HasColumnName("created_at");
                entity.Property(a => a.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(a => a.Customer)
                      .WithMany(c => c.Addresses)
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerCart>(entity =>
            {
                entity.ToTable("customer_carts", "dbo");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.CustomerId).IsUnique();

                entity.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(c => c.CustomerId).HasColumnName("customer_id");
                entity.Property(c => c.CreatedAt).HasColumnName("created_at");
                entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(c => c.Customer)
                      .WithOne(c => c.Cart)
                      .HasForeignKey<CustomerCart>(c => c.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerCartItem>(entity =>
            {
                entity.ToTable("customer_cart_items", "dbo");
                entity.HasKey(i => i.Id);
                entity.HasIndex(i => new { i.CustomerCartId, i.ProductId }).IsUnique();

                entity.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(i => i.CustomerCartId).HasColumnName("customer_cart_id");
                entity.Property(i => i.ProductId).HasColumnName("product_id");
                entity.Property(i => i.Quantity).HasColumnName("quantity");
                entity.Property(i => i.CreatedAt).HasColumnName("created_at");
                entity.Property(i => i.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(i => i.CustomerCart)
                      .WithMany(c => c.Items)
                      .HasForeignKey(i => i.CustomerCartId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerOrderReference>(entity =>
            {
                entity.ToTable("customer_order_references", "dbo");
                entity.HasKey(o => o.Id);
                entity.HasIndex(o => o.ErpOrderId).IsUnique();
                entity.HasIndex(o => new { o.CustomerId, o.CreatedAt });

                entity.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(o => o.CustomerId).HasColumnName("customer_id");
                entity.Property(o => o.ErpOrderId).HasColumnName("erp_order_id");
                entity.Property(o => o.ExternalOrderId).HasColumnName("external_order_id").IsRequired().HasMaxLength(100);
                entity.Property(o => o.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
                entity.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3);
                entity.Property(o => o.PaymentMethod).HasColumnName("payment_method").IsRequired().HasMaxLength(50);
                entity.Property(o => o.Status).HasColumnName("status").IsRequired().HasMaxLength(30);
                entity.Property(o => o.ShippingFullName).HasColumnName("shipping_full_name").IsRequired().HasMaxLength(150);
                entity.Property(o => o.ShippingPhone).HasColumnName("shipping_phone").HasMaxLength(20);
                entity.Property(o => o.ShippingAddressLine1).HasColumnName("shipping_address_line_1").IsRequired().HasMaxLength(255);
                entity.Property(o => o.ShippingAddressLine2).HasColumnName("shipping_address_line_2").HasMaxLength(255);
                entity.Property(o => o.ShippingCity).HasColumnName("shipping_city").IsRequired().HasMaxLength(100);
                entity.Property(o => o.ShippingState).HasColumnName("shipping_state").HasMaxLength(100);
                entity.Property(o => o.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(30);
                entity.Property(o => o.ShippingCountry).HasColumnName("shipping_country").IsRequired().HasMaxLength(100);
                entity.Property(o => o.Notes).HasColumnName("notes");
                entity.Property(o => o.CreatedAt).HasColumnName("created_at");
                entity.Property(o => o.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(o => o.Customer)
                      .WithMany(c => c.OrderReferences)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustomerOrderReferenceItem>(entity =>
            {
                entity.ToTable("customer_order_reference_items", "dbo");
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
                entity.Property(i => i.CustomerOrderReferenceId).HasColumnName("customer_order_reference_id");
                entity.Property(i => i.ProductId).HasColumnName("product_id");
                entity.Property(i => i.ProductName).HasColumnName("product_name").IsRequired().HasMaxLength(255);
                entity.Property(i => i.Quantity).HasColumnName("quantity");
                entity.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
                entity.Property(i => i.TotalPrice).HasColumnName("total_price").HasPrecision(12, 2);

                entity.HasOne(i => i.CustomerOrderReference)
                      .WithMany(o => o.Items)
                      .HasForeignKey(i => i.CustomerOrderReferenceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
