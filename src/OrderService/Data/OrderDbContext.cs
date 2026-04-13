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
            });
        }
    }
}
