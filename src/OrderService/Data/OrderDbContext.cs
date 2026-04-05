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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.HasIndex(o => o.ExternalOrderId).IsUnique();

                entity.Property(o => o.TotalAmount)
                    .HasPrecision(18, 2);

                // Store enum as string for readability in DB
                entity.Property(o => o.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(o => o.ExternalOrderId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(o => o.CustomerId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(o => o.CancellationReason)
                    .HasMaxLength(500);
            });
        }
    }
}