using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders", "dbo");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(o => o.TotalAmount)
                    .HasColumnName("total_amount")
                    .HasPrecision(12, 2);

                entity.Property(o => o.Status)
                    .HasColumnName("status")
                    .HasMaxLength(50);

                entity.Property(o => o.CustomerId)
                    .HasColumnName("customer_id")
                    .IsRequired();

                entity.Property(o => o.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(o => o.Notes)
                    .HasColumnName("notes");

                entity.Property(o => o.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(o => o.UpdatedAt)
                    .HasColumnName("updated_at");

                entity.HasMany(o => o.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items", "dbo");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(i => i.OrderId)
                    .HasColumnName("order_id")
                    .IsRequired();

                entity.Property(i => i.ProductId)
                    .HasColumnName("product_id")
                    .IsRequired();

                entity.Property(i => i.ProductName)
                    .HasColumnName("product_name")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(i => i.Quantity)
                    .HasColumnName("quantity");

                entity.Property(i => i.UnitPrice)
                    .HasColumnName("unit_price")
                    .HasPrecision(12, 2);

                entity.Property(i => i.TotalPrice)
                    .HasColumnName("total_price")
                    .HasPrecision(12, 2);

                entity.Property(i => i.CreatedAt)
                    .HasColumnName("created_at");
            });
        }
    }
}
