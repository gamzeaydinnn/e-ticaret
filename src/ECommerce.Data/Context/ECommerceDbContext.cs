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
        public virtual DbSet<Banner> Banners { get; set; }
      public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<CampaignRule> CampaignRules { get; set; }
        public virtual DbSet<CampaignReward> CampaignRewards { get; set; }
        
        // SMS Doğrulama Tabloları
        public virtual DbSet<SmsVerification> SmsVerifications { get; set; }
        public virtual DbSet<SmsRateLimit> SmsRateLimits { get; set; }

        // RBAC (Rol Tabanlı Yetkilendirme) Tabloları
        /// <summary>
        /// Sistemdeki tüm izinleri (permissions) tutar.
        /// Her izin bir modül ve aksiyon kombinasyonunu temsil eder.
        /// </summary>
        public virtual DbSet<Permission> Permissions { get; set; }
        
        /// <summary>
        /// Rol-Permission many-to-many ilişkisini yönetir.
        /// Hangi rolün hangi izinlere sahip olduğunu tanımlar.
        /// </summary>
        public virtual DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------
            // GLOBAL COLLATION AYARI: Türkçe karakter desteği için Turkish_CI_AS
            // Bu ayar tüm string alanlarının Türkçe karakterleri (ğ, ü, ş, ö, ç, ı, İ) 
            // doğru şekilde saklamasını ve sıralamasını sağlar.
            // CI = Case Insensitive (büyük/küçük harf duyarsız)
            // AS = Accent Sensitive (aksan duyarlı: ı != i)
            // -------------------
            modelBuilder.UseCollation("Turkish_CI_AS");

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
                // User -> Addresses (1 : N)
                entity.HasMany(u => u.Addresses)
                      .WithOne(a => a.User)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
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
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.SpecialPrice).HasPrecision(18, 2);

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
            // Address Configuration
            // -------------------
            modelBuilder.Entity<Address>(entity =>
            {
                entity.ToTable("Addresses");
                entity.Property(a => a.Title).HasMaxLength(100);
                entity.Property(a => a.FullName).HasMaxLength(200);
                entity.Property(a => a.Phone).HasMaxLength(64);
                entity.Property(a => a.City).HasMaxLength(100);
                entity.Property(a => a.District).HasMaxLength(100);
                entity.Property(a => a.Street).HasMaxLength(500);
                entity.Property(a => a.PostalCode).HasMaxLength(20);

                entity.HasOne(a => a.User)
                      .WithMany(u => u.Addresses)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
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
                entity.Property(e => e.VatAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.FinalPrice).HasPrecision(18, 2);
                entity.Property(e => e.CouponDiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.CampaignDiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.ShippingCost).HasPrecision(18, 2);

                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.ClientOrderId).IsUnique();

                    // Address relationship (optional)
                    entity.HasOne(o => o.Address)
                        .WithMany()
                        .HasForeignKey(o => o.AddressId)
                        .OnDelete(DeleteBehavior.Restrict);
            });

                  // -------------------
                  // OrderStatusHistory Configuration
                  // -------------------
                  modelBuilder.Entity<OrderStatusHistory>(entity =>
                  {
                        entity.ToTable("OrderStatusHistories");
                        entity.Property(e => e.ChangedAt).HasColumnType("datetime2").IsRequired();
                        entity.Property(e => e.ChangedBy).HasMaxLength(200);
                        entity.Property(e => e.Reason).HasMaxLength(2000);

                        entity.HasOne(h => h.Order)
                                .WithMany(o => o.OrderStatusHistories)
                                .HasForeignKey(h => h.OrderId)
                                .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(h => h.OrderId);
                  });

            // -------------------
            // OrderItem Configuration
            // -------------------
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

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
                        .WithMany(p => p.ProductImages)
                        .HasForeignKey(pi => pi.ProductId)
                        .OnDelete(DeleteBehavior.Cascade);
 
            // ProductVariant Configuration
            modelBuilder.Entity<ProductVariant>()
                        .HasOne(v => v.Product)
                        .WithMany(p => p.ProductVariants)
                        .HasForeignKey(v => v.ProductId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                        .Property(v => v.Price)
                        .HasPrecision(18, 2);

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
            // Courier Configuration
            // -------------------
            modelBuilder.Entity<Courier>(entity =>
            {
                entity.ToTable("Couriers");
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Vehicle).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.Rating).HasPrecision(3, 2); // 0.00 - 5.00 için yeterli
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasMany(e => e.AssignedOrders)
                      .WithOne(o => o.Courier)
                      .HasForeignKey(o => o.CourierId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

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
                entity.Property(e => e.Value).HasPrecision(18, 2);
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.Rewards)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.Property(e => e.Token).HasMaxLength(512);
                entity.Property(e => e.HashedToken).HasMaxLength(256).IsRequired();
                entity.Property(e => e.JwtId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedIp).HasMaxLength(64);
                // HashedToken üzerinde unique index (Token artık empty string olarak saklanıyor)
                entity.HasIndex(e => e.HashedToken).IsUnique();

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

            modelBuilder.Entity<Payments>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.Property(c => c.Value).HasPrecision(18, 2);
                entity.Property(c => c.MinOrderAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Discount>(entity =>
            {
                entity.Property(d => d.Value).HasPrecision(18, 2);
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
            // SmsVerification Configuration
            // -------------------
            modelBuilder.Entity<SmsVerification>(entity =>
            {
                entity.ToTable("SmsVerifications");
                
                entity.Property(e => e.PhoneNumber)
                      .HasMaxLength(20)
                      .IsRequired();
                
                entity.Property(e => e.Code)
                      .HasMaxLength(10)
                      .IsRequired();
                
                entity.Property(e => e.CodeHash)
                      .HasMaxLength(256);
                
                entity.Property(e => e.IpAddress)
                      .HasMaxLength(50);
                
                entity.Property(e => e.UserAgent)
                      .HasMaxLength(500);
                
                entity.Property(e => e.JobId)
                      .HasMaxLength(100);
                
                entity.Property(e => e.SmsErrorMessage)
                      .HasMaxLength(500);

                // Index'ler - Performans için kritik
                entity.HasIndex(e => e.PhoneNumber)
                      .HasDatabaseName("IX_SmsVerifications_PhoneNumber");
                
                entity.HasIndex(e => new { e.PhoneNumber, e.Purpose, e.Status })
                      .HasDatabaseName("IX_SmsVerifications_Phone_Purpose_Status");
                
                entity.HasIndex(e => e.ExpiresAt)
                      .HasDatabaseName("IX_SmsVerifications_ExpiresAt");
                
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_SmsVerifications_UserId");

                // User ilişkisi (opsiyonel)
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------
            // SmsRateLimit Configuration
            // -------------------
            modelBuilder.Entity<SmsRateLimit>(entity =>
            {
                entity.ToTable("SmsRateLimits");
                
                entity.Property(e => e.PhoneNumber)
                      .HasMaxLength(20)
                      .IsRequired();
                
                entity.Property(e => e.IpAddress)
                      .HasMaxLength(50);
                
                entity.Property(e => e.BlockReason)
                      .HasMaxLength(200);

                // Index'ler
                entity.HasIndex(e => e.PhoneNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_SmsRateLimits_PhoneNumber");
                
                entity.HasIndex(e => e.IpAddress)
                      .HasDatabaseName("IX_SmsRateLimits_IpAddress");
                
                entity.HasIndex(e => e.DailyResetAt)
                      .HasDatabaseName("IX_SmsRateLimits_DailyResetAt");
            });

            // -------------------
            // RBAC: Permission Configuration
            // -------------------
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
                
                // Name alanı benzersiz olmalı - izin adları tekrarlanamaz
                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                
                entity.Property(e => e.DisplayName)
                      .HasMaxLength(200)
                      .IsRequired();
                
                entity.Property(e => e.Description)
                      .HasMaxLength(500);
                
                entity.Property(e => e.Module)
                      .HasMaxLength(100)
                      .IsRequired();

                // Unique index: Aynı isimde iki izin olamaz
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_Permissions_Name");
                
                // Modül bazlı sorgular için index
                entity.HasIndex(e => e.Module)
                      .HasDatabaseName("IX_Permissions_Module");
                
                // Aktif izinleri hızlı sorgulamak için
                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("IX_Permissions_IsActive");
            });

            // -------------------
            // RBAC: RolePermission Configuration (Join Table)
            // -------------------
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");

                // Primary key
                entity.HasKey(e => e.Id);
                
                // RoleId - AspNetRoles tablosuna referans
                // NOT: IdentityRole<int> kullanıldığından foreign key tanımı manuel
                entity.Property(e => e.RoleId)
                      .IsRequired();
                
                entity.Property(e => e.PermissionId)
                      .IsRequired();
                
                entity.Property(e => e.CreatedAt)
                      .HasColumnType("datetime2");

                // Permission navigation - Cascade delete: Permission silinirse ilişki de silinir
                entity.HasOne(e => e.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(e => e.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // CreatedByUser - Set null: Kullanıcı silinirse audit bilgisi korunur
                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Unique constraint: Aynı role aynı izin birden fazla atanamaz
                entity.HasIndex(e => new { e.RoleId, e.PermissionId })
                      .IsUnique()
                      .HasDatabaseName("IX_RolePermissions_Role_Permission");
                
                // Role bazlı sorgular için index (kullanıcının izinlerini hızlı çekmek için)
                entity.HasIndex(e => e.RoleId)
                      .HasDatabaseName("IX_RolePermissions_RoleId");
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
