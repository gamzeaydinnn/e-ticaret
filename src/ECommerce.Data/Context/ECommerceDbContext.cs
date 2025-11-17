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
        public virtual DbSet<ReconciliationLog> ReconciliationLogs { get; set; }
        public virtual DbSet<ProductImage> ProductImages { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<Stocks> Stocks { get; set; }
        public virtual DbSet<StockReservation> StockReservations { get; set; }
        public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

        public virtual DbSet<Brand> Brands { get; set; }
        public virtual DbSet<Discount> Discounts { get; set; }
        public virtual DbSet<ProductReview> ProductReviews { get; set; }
        public virtual DbSet<Address> Addresses { get; set; }
        public virtual DbSet<DeliverySlot> DeliverySlots { get; set; }
        public virtual DbSet<Coupon> Coupons { get; set; }
        public virtual DbSet<InventoryLog> InventoryLogs { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<WeightReport> WeightReports { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<CampaignRule> CampaignRules { get; set; }
        public virtual DbSet<CampaignReward> CampaignRewards { get; set; }

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

            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.ToTable("StockReservations");
                entity.Property(e => e.ClientOrderId)
                      .IsRequired();
                entity.Property(e => e.Quantity)
                      .IsRequired();
                entity.Property(e => e.CreatedAt)
                      .HasColumnType("datetime2");
                entity.Property(e => e.ExpiresAt)
                      .HasColumnType("datetime2");
                entity.HasIndex(e => e.ClientOrderId);
                entity.HasIndex(e => new { e.ProductId, e.IsReleased, e.ExpiresAt });

                entity.HasOne(e => e.Product)
                      .WithMany(p => p.StockReservations)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.StockReservations)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------
            // Order Configuration
            // -------------------
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ShippingCity).HasMaxLength(100).IsRequired();
                entity.Property(e => e.AppliedCouponCode).HasMaxLength(50);

                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.ClientOrderId).IsUnique();
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

            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.ToTable("Campaigns");
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
            });

            modelBuilder.Entity<CampaignRule>(entity =>
            {
                entity.ToTable("CampaignRules");
                entity.Property(e => e.ConditionJson).IsRequired();
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.Rules)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CampaignReward>(entity =>
            {
                entity.ToTable("CampaignRewards");
                entity.Property(e => e.RewardType).HasMaxLength(50).IsRequired();
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.Rewards)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.Property(e => e.Token).HasMaxLength(512).IsRequired();
                entity.Property(e => e.JwtId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedIp).HasMaxLength(64);
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReconciliationLog>(entity =>
            {
                entity.ToTable("ReconciliationLogs");
                entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ProviderPaymentId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CheckedAt).HasColumnType("datetime2");
                entity.Property(e => e.Issue).HasMaxLength(1000);
                entity.Property(e => e.Details).HasMaxLength(4000);
            });

            // -------------------
            // WeightReport Configuration
            // -------------------
            modelBuilder.Entity<WeightReport>(entity =>
            {
                entity.ToTable("WeightReports");
                
                entity.Property(e => e.ExternalReportId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(100).IsRequired();
                entity.Property(e => e.OverageAmount).HasColumnType("decimal(18,2)");
                
                // Unique index for idempotency
                entity.HasIndex(e => e.ExternalReportId).IsUnique();
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.Status);

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.WeightReports)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.OrderItem)
                      .WithMany(oi => oi.WeightReports)
                      .HasForeignKey(e => e.OrderItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

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
