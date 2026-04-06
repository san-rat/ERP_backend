using System;
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

            // Ensure schema alignment
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
            modelBuilder.Entity<Inventory>().HasIndex(i => i.ProductId).IsUnique();

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", CreatedAt = DateTime.UtcNow },
                new Category { Id = 2, Name = "Clothing", CreatedAt = DateTime.UtcNow },
                new Category { Id = 3, Name = "Food & Beverage", CreatedAt = DateTime.UtcNow },
                new Category { Id = 4, Name = "Office Supplies", CreatedAt = DateTime.UtcNow },
                new Category { Id = 5, Name = "Machinery", CreatedAt = DateTime.UtcNow }
            );

            // Seed Products
            var prod1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var prod2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = prod1Id, CategoryId = 1, Sku = "SKU-LAPTOP-01", Name = "High End Laptop", Price = 1200.00m },
                new Product { Id = prod2Id, CategoryId = 4, Sku = "SKU-KEYBOARD-01", Name = "Mechanical Keyboard", Price = 75.50m }
            );

            // Seed Inventory
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory { Id = Guid.NewGuid(), ProductId = prod1Id, QuantityAvailable = 50, LowStockThreshold = 10 },
                new Inventory { Id = Guid.NewGuid(), ProductId = prod2Id, QuantityAvailable = 5, LowStockThreshold = 10 } // Below threshold
            );
        }
    }
}
