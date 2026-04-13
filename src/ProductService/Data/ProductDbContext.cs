using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<InventoryReservation> InventoryReservations { get; set; } = null!;
        public DbSet<LowStockAlert> LowStockAlerts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(c => c.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100);

                entity.Property(c => c.Description)
                    .HasColumnName("description")
                    .HasMaxLength(500);

                entity.Property(c => c.CreatedAt)
                    .HasColumnName("created_at");

                entity.HasIndex(c => c.Name).IsUnique();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.CategoryId).HasColumnName("category_id");
                entity.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(100);
                entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(255);
                entity.Property(p => p.Description).HasColumnName("description");
                entity.Property(p => p.Price).HasColumnName("price").HasColumnType("decimal(12,2)");
                entity.Property(p => p.IsActive).HasColumnName("is_active");
                entity.Property(p => p.CreatedAt).HasColumnName("created_at");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                entity.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
                entity.Property(p => p.QuantityAvailable).HasColumnName("quantity_available");

                entity.HasIndex(p => p.Sku).IsUnique();

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId);

                entity.HasOne(p => p.Inventory)
                    .WithOne(i => i.Product)
                    .HasForeignKey<Inventory>(i => i.ProductId);
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("inventory");
                entity.HasKey(i => i.ProductId);

                entity.Property(i => i.ProductId)
                    .HasColumnName("product_id")
                    .ValueGeneratedNever();

                entity.Property(i => i.QuantityAvailable).HasColumnName("quantity_available");
                entity.Property(i => i.QuantityReserved).HasColumnName("quantity_reserved");
                entity.Property(i => i.LowStockThreshold).HasColumnName("low_stock_threshold");
                entity.Property(i => i.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<InventoryReservation>(entity =>
            {
                entity.ToTable("inventory_reservations");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.ProductId).HasColumnName("product_id");
                entity.Property(r => r.OrderId).HasColumnName("order_id");
                entity.Property(r => r.Quantity).HasColumnName("quantity");
                entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(30);
                entity.Property(r => r.ReservedAt).HasColumnName("reserved_at");

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductId);
            });

            modelBuilder.Entity<LowStockAlert>(entity =>
            {
                entity.ToTable("low_stock_alerts");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id).HasColumnName("id");
                entity.Property(a => a.ProductId).HasColumnName("product_id");
                entity.Property(a => a.QuantityAtAlert).HasColumnName("quantity_at_alert");
                entity.Property(a => a.IsResolved).HasColumnName("is_resolved");
                entity.Property(a => a.AlertedAt).HasColumnName("created_at");
                entity.Property(a => a.ResolvedAt).HasColumnName("resolved_at");

                entity.HasOne(a => a.Product)
                    .WithMany()
                    .HasForeignKey(a => a.ProductId);
            });
        }
    }
}
