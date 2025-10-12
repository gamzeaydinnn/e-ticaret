using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Concrete;
using ECommerce.Data.Context;


using System;

namespace ECommerce.Data.Context
{
    public class ECommerceDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options) { }

        // DbSets
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }

        public virtual DbSet<AuditLogs> AuditLogs { get; set; }
        public virtual DbSet<Courier> Couriers { get; set; }
        public virtual DbSet<Favorite> Favorites { get; set; }
        public virtual DbSet<MicroSyncLog> MicroSyncLogs { get; set; }
        public virtual DbSet<Payments> Payments { get; set; }
        public virtual DbSet<ProductImage> ProductImages { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<Stocks> Stocks { get; set; }

        public virtual DbSet<Brand> Brands { get; set; }
        public virtual DbSet<Discount> Discounts { get; set; }
        public virtual DbSet<ProductReview> ProductReviews { get; set; }
        public virtual DbSet<Address> Addresses { get; set; }
        public virtual DbSet<DeliverySlot> DeliverySlots { get; set; }
        public virtual DbSet<Coupon> Coupons { get; set; }
        public virtual DbSet<InventoryLog> InventoryLogs { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------
            // User Configuration
            // -------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
            });

            // -------------------
            // Category Configuration
            // -------------------
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

            // -------------------
            // Product Configuration
            // -------------------
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Brand)
                      .WithMany(b => b.Products)
                      .HasForeignKey(p => p.BrandId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.SKU).IsUnique();
            });

            // Product-Discount Many-to-Many
            modelBuilder.Entity<Product>()
                        .HasMany(p => p.Discounts)
                        .WithMany(d => d.Products)
                        .UsingEntity(j => j.ToTable("ProductDiscounts"));

            // -------------------
            // Order Configuration
            // -------------------
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ShippingCity).HasMaxLength(100).IsRequired();

                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(o => o.OrderNumber).IsUnique();
            });

            // -------------------
            // OrderItem Configuration
            // -------------------
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");

                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------
            // CartItem Configuration
            // -------------------
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Product)
                      .WithMany()
                      .HasForeignKey(c => c.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(c => new { c.UserId, c.ProductId }).IsUnique();
            });

            // ProductImage Configuration
modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.ProductImages) // ✅ Product'ta 'ProductImages' varsa bunu kullan
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
 
// ProductVariant Configuration
modelBuilder.Entity<ProductVariant>()
            .HasOne(v => v.Product)
            .WithMany(p => p.ProductVariants) 
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            // -------------------
            // Stocks Configuration
            // -------------------
            modelBuilder.Entity<Stocks>()
                        .HasOne(s => s.ProductVariant)
                        .WithMany()
                        .HasForeignKey(s => s.ProductVariantId)
                        .OnDelete(DeleteBehavior.Restrict);

            // -------------------
            // ProductReview Configuration
            // -------------------
            modelBuilder.Entity<ProductReview>()
                        .HasOne(r => r.Product)
                        .WithMany(p => p.ProductReviews)
                        .HasForeignKey(r => r.ProductId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductReview>()
                        .HasOne(r => r.User)
                        .WithMany()
                        .HasForeignKey(r => r.UserId)
                        .OnDelete(DeleteBehavior.Restrict);

            // -------------------
            // Seed Data
            // -------------------
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var fixedDate = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Elektronik",
                    Description = "Elektronik ürünler",
                    SortOrder = 1,
                    Slug = "elektronik",
                    CreatedAt = fixedDate,
                    IsActive = true
                },
                new Category
                {
                    Id = 2,
                    Name = "Giyim",
                    Description = "Giyim ürünleri",
                    SortOrder = 2,
                    Slug = "giyim",
                    CreatedAt = fixedDate,
                    IsActive = true
                }
            );
        }
    }
}
