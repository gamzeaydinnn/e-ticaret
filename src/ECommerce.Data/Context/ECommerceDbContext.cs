using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Context
{
    public class ECommerceDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)
        {
        }

        // DbSets
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
            });

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.SubCategories)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product Configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Brand).HasMaxLength(100);

                entity.HasOne(e => e.Category)
                    .WithMany(e => e.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.SKU).IsUnique();
            });

            // Order Configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ShippingCity).HasMaxLength(100).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Orders)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderNumber).IsUnique();
            });

            // OrderItem Configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");

                entity.HasOne(e => e.Order)
                    .WithMany(e => e.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(e => e.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CartItem Configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Use fixed timestamp to avoid migration diffs
            var seededAt = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Elektronik",
                    Description = "Elektronik ürünler",
                    SortOrder = 1,
                    CreatedDate = seededAt
                },
                new Category
                {
                    Id = 2,
                    Name = "Giyim",
                    Description = "Giyim ürünleri",
                    SortOrder = 2,
                    CreatedDate = seededAt
                }
            );
        }
    }
    
}