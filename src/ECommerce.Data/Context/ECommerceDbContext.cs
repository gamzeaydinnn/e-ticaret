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
        public virtual DbSet<MikroSyncState> MikroSyncStates { get; set; }
        public virtual DbSet<MikroCategoryMapping> MikroCategoryMappings { get; set; }
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
        
        // Kampanya Hedefleri Tablosu
        /// <summary>
        /// Kampanya hedeflerini tutar (ürün veya kategori bazlı).
        /// Campaign.TargetType = All ise bu tablo kullanılmaz.
        /// </summary>
        public virtual DbSet<CampaignTarget> CampaignTargets { get; set; }
        
        // Kupon Sistemi Tabloları
        /// <summary>
        /// Kupon kullanım geçmişini tutar.
        /// Her sipariş için hangi kuponun kullanıldığını ve indirim miktarını takip eder.
        /// </summary>
        public virtual DbSet<CouponUsage> CouponUsages { get; set; }
        
        /// <summary>
        /// Kupon-Ürün many-to-many ilişkisini yönetir.
        /// Belirli ürünlere özel kuponlar tanımlamak için kullanılır.
        /// </summary>
        public virtual DbSet<CouponProduct> CouponProducts { get; set; }

        // Ağırlık Bazlı Ödeme Sistemi
        /// <summary>
        /// Ağırlık fark kayıtlarını tutar.
        /// Tartı sonrası fiyat değişikliklerini ve admin müdahalelerini takip eder.
        /// </summary>
        public virtual DbSet<WeightAdjustment> WeightAdjustments { get; set; }
        
        // SMS Doğrulama Tabloları
        public virtual DbSet<SmsVerification> SmsVerifications { get; set; }
        public virtual DbSet<SmsRateLimit> SmsRateLimits { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // WEBHOOK VE ÖDEME TAKİP TABLOLARI
        // Idempotency ve audit için webhook event kayıtları
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ödeme sağlayıcılarından gelen webhook event'lerini saklar
        /// Idempotency için kritik: Aynı event'in birden fazla işlenmesini önler
        /// Replay attack koruması ve audit amaçlı loglar
        /// </summary>
        public virtual DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }

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

        // ═══════════════════════════════════════════════════════════════════════════════
        // POSNET ÖDEME SİSTEMİ TABLOLARI
        // Yapı Kredi POSNET entegrasyonu için eklenen tablolar
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET işlem logları - Detaylı audit trail
        /// Her XML isteği ve yanıtı saklanır
        /// </summary>
        public virtual DbSet<PosnetTransactionLog> PosnetTransactionLogs { get; set; }

        // -------------------
        // XML/Varyant Sistemi Tabloları
        // -------------------
        
        /// <summary>
        /// Ürün seçenek türlerini tutar.
        /// Örn: "Hacim", "Renk", "Beden", "Paket"
        /// </summary>
        public virtual DbSet<ProductOption> ProductOptions { get; set; }
        
        /// <summary>
        /// Seçenek türlerine ait değerleri tutar.
        /// Örn: Hacim için "330ml", "1L", "2L"
        /// </summary>
        public virtual DbSet<ProductOptionValue> ProductOptionValues { get; set; }
        
        /// <summary>
        /// Varyant-Seçenek değeri ilişkisi (Many-to-Many).
        /// Hangi varyantın hangi özelliklere sahip olduğunu tanımlar.
        /// </summary>
        public virtual DbSet<VariantOptionValue> VariantOptionValues { get; set; }
        
        /// <summary>
        /// XML feed kaynaklarını tutar.
        /// Tedarikçi XML URL'leri ve mapping konfigürasyonları.
        /// </summary>
        public virtual DbSet<XmlFeedSource> XmlFeedSources { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // NEWSLETTER (BÜLTEN) SİSTEMİ
        // Kullanıcıların e-posta bülteni aboneliklerini yönetir
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Newsletter abonelerini tutar.
        /// GDPR uyumlu abonelik ve token bazlı iptal mekanizması içerir.
        /// </summary>
        public virtual DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // KARGO ÜCRETİ YÖNETİM SİSTEMİ
        // Araç tipi bazlı (motorcycle/car) dinamik kargo fiyatlandırması
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kargo ücreti ayarlarını tutar.
        /// Admin panelinden araç tipine göre (motosiklet/araba) fiyat güncellenebilir.
        /// Kurye bazlı DEĞİL, sistemdeki araç tipleri bazlı fiyatlandırma.
        /// </summary>
        public virtual DbSet<ShippingSetting> ShippingSettings { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ANA SAYFA ÜRÜN BLOK SİSTEMİ
        // Admin panelinden yönetilebilir ürün blokları (İndirimli, Süt Ürünleri vb.)
        // Her blok poster/banner + ürün listesi şeklinde görüntülenir
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ana sayfa ürün bloklarını tutar.
        /// Her blok bir poster ve ürün listesi içerir.
        /// Blok tipleri: manual, category, discounted, newest, bestseller
        /// </summary>
        public virtual DbSet<HomeProductBlock> HomeProductBlocks { get; set; }

        /// <summary>
        /// Blok-Ürün many-to-many ilişkisini yönetir.
        /// Sadece BlockType = "manual" olan bloklar için kullanılır.
        /// Admin'in elle seçtiği ürünleri ve sıralamasını saklar.
        /// </summary>
        public virtual DbSet<HomeBlockProduct> HomeBlockProducts { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // İADE TALEBİ SİSTEMİ
        // Müşteri iade taleplerini ve admin onay sürecini yönetir
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Müşteri iade taleplerini tutar.
        /// Kargo durumuna göre otomatik iptal veya admin onaylı iade akışı yönetir.
        /// </summary>
        public virtual DbSet<RefundRequest> RefundRequests { get; set; }

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
                
                // Varyant snapshot alanları (sipariş anındaki değerler)
                entity.Property(oi => oi.VariantTitle).HasMaxLength(200);
                entity.Property(oi => oi.VariantSku).HasMaxLength(50);

                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Varyant ilişkisi (nullable - eski siparişler için)
                entity.HasOne(oi => oi.ProductVariant)
                      .WithMany()
                      .HasForeignKey(oi => oi.ProductVariantId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------
            // CartItem Configuration
            // -------------------
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Product)
                      .WithMany(p => p.CartItems)
                      .HasForeignKey(c => c.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Varyant ilişkisi (nullable - varyantsız ürünler için)
                entity.HasOne(c => c.ProductVariant)
                      .WithMany()
                      .HasForeignKey(c => c.ProductVariantId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Aynı kullanıcı, aynı ürün ve aynı varyant tek satır olabilir
                entity.HasIndex(c => new { c.UserId, c.ProductId, c.ProductVariantId }).IsUnique();
            });

            // ProductImage Configuration
            modelBuilder.Entity<ProductImage>()
                        .HasOne(pi => pi.Product)
                        .WithMany(p => p.ProductImages)
                        .HasForeignKey(pi => pi.ProductId)
                        .OnDelete(DeleteBehavior.Cascade);
 
            // ProductVariant Configuration
            // SKU benzersiz olmalı - XML entegrasyonunun ana anahtarı
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.ToTable("ProductVariants");
                
                // Temel alanlar
                entity.Property(v => v.Title).HasMaxLength(200).IsRequired();
                entity.Property(v => v.SKU).HasMaxLength(50).IsRequired();
                entity.Property(v => v.Price).HasPrecision(18, 2);
                entity.Property(v => v.Currency).HasMaxLength(10).HasDefaultValue("TRY");
                
                // XML entegrasyon alanları
                entity.Property(v => v.Barcode).HasMaxLength(50);
                entity.Property(v => v.SupplierCode).HasMaxLength(100);
                entity.Property(v => v.ParentSku).HasMaxLength(50);
                
                // SKU benzersiz index - entegrasyonun kritik noktası
                entity.HasIndex(v => v.SKU)
                      .IsUnique()
                      .HasDatabaseName("IX_ProductVariants_SKU");
                
                // Barcode index (opsiyonel aramalar için)
                entity.HasIndex(v => v.Barcode)
                      .HasDatabaseName("IX_ProductVariants_Barcode");
                
                // ParentSku index (gruplama için)
                entity.HasIndex(v => v.ParentSku)
                      .HasDatabaseName("IX_ProductVariants_ParentSku");
                
                // Tedarikçi kodu index
                entity.HasIndex(v => v.SupplierCode)
                      .HasDatabaseName("IX_ProductVariants_SupplierCode");
                
                // Son görülme tarihi index (pasifleştirme için)
                entity.HasIndex(v => v.LastSeenAt)
                      .HasDatabaseName("IX_ProductVariants_LastSeenAt");
                
                // Product ilişkisi
                entity.HasOne(v => v.Product)
                      .WithMany(p => p.ProductVariants)
                      .HasForeignKey(v => v.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------
            // ProductOption Configuration
            // Seçenek tipleri: Hacim, Renk, Beden vb.
            // -------------------
            modelBuilder.Entity<ProductOption>(entity =>
            {
                entity.ToTable("ProductOptions");
                
                entity.Property(o => o.Name).HasMaxLength(100).IsRequired();
                entity.Property(o => o.Description).HasMaxLength(500);
                
                // Her seçenek adı benzersiz olmalı
                entity.HasIndex(o => o.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_ProductOptions_Name");
            });

            // -------------------
            // ProductOptionValue Configuration
            // Seçenek değerleri: 330ml, 1L, Kırmızı, XL vb.
            // -------------------
            modelBuilder.Entity<ProductOptionValue>(entity =>
            {
                entity.ToTable("ProductOptionValues");
                
                entity.Property(ov => ov.Value).HasMaxLength(100).IsRequired();
                entity.Property(ov => ov.ColorCode).HasMaxLength(20);
                
                // Aynı seçenek altında aynı değer olamaz
                entity.HasIndex(ov => new { ov.OptionId, ov.Value })
                      .IsUnique()
                      .HasDatabaseName("IX_ProductOptionValues_OptionId_Value");
                
                // ProductOption ilişkisi
                entity.HasOne(ov => ov.Option)
                      .WithMany(o => o.OptionValues)
                      .HasForeignKey(ov => ov.OptionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------
            // VariantOptionValue Configuration
            // Many-to-Many: Hangi varyant hangi seçenek değerlerine sahip
            // -------------------
            modelBuilder.Entity<VariantOptionValue>(entity =>
            {
                entity.ToTable("VariantOptionValues");
                
                // Composite Primary Key
                entity.HasKey(vov => new { vov.VariantId, vov.OptionValueId });
                
                // ProductVariant ilişkisi
                entity.HasOne(vov => vov.Variant)
                      .WithMany(v => v.VariantOptionValues)
                      .HasForeignKey(vov => vov.VariantId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // ProductOptionValue ilişkisi
                entity.HasOne(vov => vov.OptionValue)
                      .WithMany()
                      .HasForeignKey(vov => vov.OptionValueId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------
            // XmlFeedSource Configuration
            // XML kaynak tanımları
            // -------------------
            modelBuilder.Entity<XmlFeedSource>(entity =>
            {
                entity.ToTable("XmlFeedSources");
                
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Url).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.MappingConfig).HasColumnType("nvarchar(max)");
                entity.Property(x => x.SupplierName).HasMaxLength(200);
                entity.Property(x => x.AuthType).HasMaxLength(50);
                entity.Property(x => x.AuthUsername).HasMaxLength(200);
                entity.Property(x => x.AuthPassword).HasMaxLength(200);
                entity.Property(x => x.LastSyncError).HasMaxLength(2000);
                entity.Property(x => x.Notes).HasMaxLength(1000);
                
                // Feed adı benzersiz olmalı
                entity.HasIndex(x => x.Name)
                      .IsUnique()
                      .HasDatabaseName("IX_XmlFeedSources_Name");
            });

            // -------------------
            // Stocks Configuration
            // -------------------
            modelBuilder.Entity<Stocks>()
                        .HasOne(s => s.ProductVariant)
                        .WithMany(pv => pv.Stocks)
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
                        .WithMany(u => u.ProductReviews)
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

            // ═══════════════════════════════════════════════════════════════════════════════
            // KAMPANYA SİSTEMİ KONFİGÜRASYONU
            // Otomatik uygulanan indirimler: %, TL, 3Al2Öde, Kargo Bedava
            // ═══════════════════════════════════════════════════════════════════════════════

            modelBuilder.Entity<Campaign>(entity =>
            {
                entity.ToTable("Campaigns");

                // Temel alanlar
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);

                // Kampanya türü ve hedef türü (enum olarak saklanır)
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.TargetType).HasConversion<int>();
                
                // İndirim değerleri (decimal precision)
                entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
                entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.MinCartTotal).HasPrecision(18, 2);
                
                // Varsayılan değerler
                entity.Property(e => e.Priority).HasDefaultValue(100);
                entity.Property(e => e.IsStackable).HasDefaultValue(true);
                
                // Performans için index: aktif kampanyaları hızlı bulmak için
                entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate })
                      .HasDatabaseName("IX_Campaigns_ActiveDateRange");
            });

            // Kampanya Hedefleri (ürün veya kategori bazlı hedefleme)
            modelBuilder.Entity<CampaignTarget>(entity =>
            {
                entity.ToTable("CampaignTargets");
                
                entity.Property(e => e.TargetKind).HasConversion<int>();
                
                // Campaign ile ilişki
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.Targets)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Not: TargetId hem Product hem Category ID'si olabilir
                // EF Core'da polimorfik FK desteklenmediği için navigation property kullanmıyoruz
                // Servis katmanında TargetKind'a göre manuel join yapılacak
                
                // Unique constraint: Aynı kampanyada aynı hedef iki kez olamaz
                entity.HasIndex(e => new { e.CampaignId, e.TargetId, e.TargetKind })
                      .IsUnique()
                      .HasDatabaseName("IX_CampaignTargets_Unique");
            });

            // Eski kampanya kuralları (geriye dönük uyumluluk için)
            modelBuilder.Entity<CampaignRule>(entity =>
            {
                entity.ToTable("CampaignRules");
                entity.Property(e => e.ConditionJson).IsRequired();
                entity.HasOne(e => e.Campaign)
                      .WithMany(c => c.Rules)
                      .HasForeignKey(e => e.CampaignId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Eski kampanya ödülleri (geriye dönük uyumluluk için)
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
                // ═══════════════════════════════════════════════════════════════════════════════
                // PAYMENTS ENTITY KONFİGÜRASYONU
                // Tüm ödeme sağlayıcıları için ortak yapı + POSNET özel alanları
                // ═══════════════════════════════════════════════════════════════════════════════

                // Tutar alanları
                entity.Property(p => p.Amount).HasPrecision(18, 2);
                entity.Property(p => p.RefundedAmount).HasPrecision(18, 2);

                // Authorize/Capture alanları (v2.1)
                entity.Property(p => p.AuthorizedAmount).HasPrecision(18, 2);
                entity.Property(p => p.CapturedAmount).HasPrecision(18, 2);
                entity.Property(p => p.TolerancePercentage).HasPrecision(5, 2).HasDefaultValue(0.10m);
                entity.Property(p => p.AuthorizationReference).HasMaxLength(100);
                entity.Property(p => p.CaptureFailureReason).HasMaxLength(500);

                // String alanları - Maksimum uzunluklar
                entity.Property(p => p.Provider).HasMaxLength(50).IsRequired();
                entity.Property(p => p.ProviderPaymentId).HasMaxLength(100);
                entity.Property(p => p.Status).HasMaxLength(50).IsRequired();
                entity.Property(p => p.Currency).HasMaxLength(10).HasDefaultValue("TRY");

                // POSNET özel alanları
                entity.Property(p => p.HostLogKey).HasMaxLength(20);
                entity.Property(p => p.AuthCode).HasMaxLength(10);
                entity.Property(p => p.MdStatus).HasMaxLength(5);
                entity.Property(p => p.Eci).HasMaxLength(5);
                entity.Property(p => p.Cavv).HasMaxLength(100);
                entity.Property(p => p.CardBin).HasMaxLength(10);
                entity.Property(p => p.CardLastFour).HasMaxLength(10);
                entity.Property(p => p.CardType).HasMaxLength(30);
                entity.Property(p => p.TransactionType).HasMaxLength(30);
                entity.Property(p => p.TransactionId).HasMaxLength(50);
                entity.Property(p => p.IpAddress).HasMaxLength(50);

                // RawResponse - Large text
                entity.Property(p => p.RawResponse).HasColumnType("nvarchar(max)");

                // İndeksler - Performans optimizasyonu
                entity.HasIndex(p => p.OrderId).HasDatabaseName("IX_Payments_OrderId");
                entity.HasIndex(p => p.Provider).HasDatabaseName("IX_Payments_Provider");
                entity.HasIndex(p => p.HostLogKey).HasDatabaseName("IX_Payments_HostLogKey");
                entity.HasIndex(p => p.TransactionId).HasDatabaseName("IX_Payments_TransactionId");
                entity.HasIndex(p => new { p.Provider, p.Status }).HasDatabaseName("IX_Payments_Provider_Status");
                entity.HasIndex(p => p.CreatedAt).HasDatabaseName("IX_Payments_CreatedAt");

                // Self-referencing relationship (iade işlemleri için)
                entity.HasOne(p => p.OriginalPayment)
                      .WithMany(p => p.RefundPayments)
                      .HasForeignKey(p => p.OriginalPaymentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Order ilişkisi
                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // PAYMENT WEBHOOK EVENT KONFİGÜRASYONU
            // Idempotency ve audit için webhook event kayıtları
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<PaymentWebhookEvent>(entity =>
            {
                entity.ToTable("PaymentWebhookEvents");

                // Primary Key (BaseEntity'den geliyor)
                entity.HasKey(e => e.Id);

                // Provider bilgileri
                entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProviderEventId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PaymentIntentId).HasMaxLength(255);
                entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();

                // İşleme durumu
                entity.Property(e => e.ProcessingStatus).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                // Güvenlik alanları
                entity.Property(e => e.Signature).HasMaxLength(512);
                entity.Property(e => e.SourceIpAddress).HasMaxLength(45);

                // Large text alanları
                entity.Property(e => e.RawPayload).HasColumnType("nvarchar(max)");
                entity.Property(e => e.HttpHeaders).HasColumnType("nvarchar(max)");

                // ╔═══════════════════════════════════════════════════════════════════════════════╗
                // ║ UNIQUE CONSTRAINT - IDEMPOTENCY İÇİN KRİTİK!                                   ║
                // ║ Aynı event'in birden fazla kez işlenmesini engeller                           ║
                // ║ Provider + ProviderEventId kombinasyonu benzersiz olmalı                       ║
                // ╚═══════════════════════════════════════════════════════════════════════════════╝
                entity.HasIndex(e => new { e.Provider, e.ProviderEventId })
                      .IsUnique()
                      .HasDatabaseName("UQ_PaymentWebhookEvents_Provider_EventId");

                // İndeksler - Performans optimizasyonu
                entity.HasIndex(e => e.PaymentIntentId).HasDatabaseName("IX_PaymentWebhookEvents_PaymentIntentId");
                entity.HasIndex(e => e.ReceivedAt).HasDatabaseName("IX_PaymentWebhookEvents_ReceivedAt");
                entity.HasIndex(e => e.ProcessingStatus).HasDatabaseName("IX_PaymentWebhookEvents_ProcessingStatus");
                entity.HasIndex(e => new { e.Provider, e.ProcessingStatus })
                      .HasDatabaseName("IX_PaymentWebhookEvents_Provider_Status");

                // Order ilişkisi (opsiyonel)
                entity.HasOne(e => e.Order)
                      .WithMany()
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Payment ilişkisi (opsiyonel)
                entity.HasOne(e => e.Payment)
                      .WithMany()
                      .HasForeignKey(e => e.PaymentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // POSNET TRANSACTION LOG KONFİGÜRASYONU
            // Detaylı işlem logları - Audit trail
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<PosnetTransactionLog>(entity =>
            {
                entity.ToTable("PosnetTransactionLogs");

                // Primary Key
                entity.HasKey(e => e.Id);

                // Tutar alanları
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                // Large text alanları
                entity.Property(e => e.RequestXml).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ResponseXml).HasColumnType("nvarchar(max)");
                entity.Property(e => e.RequestHeaders).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");

                // İndeksler - Performans ve arama optimizasyonu
                entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_PosnetLog_CorrelationId");
                entity.HasIndex(e => e.PaymentId).HasDatabaseName("IX_PosnetLog_PaymentId");
                entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_PosnetLog_OrderId");
                entity.HasIndex(e => e.TransactionType).HasDatabaseName("IX_PosnetLog_TransactionType");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_PosnetLog_CreatedAt");
                entity.HasIndex(e => e.HostLogKey).HasDatabaseName("IX_PosnetLog_HostLogKey");
                entity.HasIndex(e => e.IsSuccess).HasDatabaseName("IX_PosnetLog_IsSuccess");

                // Composite index - Sık kullanılan sorgular için
                entity.HasIndex(e => new { e.TransactionType, e.IsSuccess, e.CreatedAt })
                      .HasDatabaseName("IX_PosnetLog_Type_Success_Date");

                // İlişkiler
                entity.HasOne(e => e.Payment)
                      .WithMany()
                      .HasForeignKey(e => e.PaymentId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Order)
                      .WithMany()
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.Property(c => c.Value).HasPrecision(18, 2);
                entity.Property(c => c.MinOrderAmount).HasPrecision(18, 2);
                entity.Property(c => c.MaxDiscountAmount).HasPrecision(18, 2);
                
                entity.Property(c => c.Code).HasMaxLength(50).IsRequired();
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.ConditionsJson).HasColumnType("nvarchar(max)");
                
                // Benzersiz kupon kodu
                entity.HasIndex(c => c.Code).IsUnique();
                
                // Kategori ilişkisi (opsiyonel)
                entity.HasOne(c => c.Category)
                      .WithMany()
                      .HasForeignKey(c => c.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
            
            // -------------------
            // CouponProduct Configuration (Many-to-Many)
            // -------------------
            modelBuilder.Entity<CouponProduct>(entity =>
            {
                entity.ToTable("CouponProducts");
                
                entity.HasKey(cp => new { cp.CouponId, cp.ProductId });
                
                entity.Property(cp => cp.CustomDiscountValue).HasPrecision(18, 2);
                
                entity.HasOne(cp => cp.Coupon)
                      .WithMany(c => c.CouponProducts)
                      .HasForeignKey(cp => cp.CouponId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(cp => cp.Product)
                      .WithMany(p => p.CouponProducts)
                      .HasForeignKey(cp => cp.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // -------------------
            // CouponUsage Configuration
            // -------------------
            modelBuilder.Entity<CouponUsage>(entity =>
            {
                entity.ToTable("CouponUsages");
                
                entity.Property(cu => cu.DiscountApplied).HasPrecision(18, 2);
                entity.Property(cu => cu.CouponCode).HasMaxLength(50);
                entity.Property(cu => cu.IpAddress).HasMaxLength(50);
                entity.Property(cu => cu.UserAgent).HasMaxLength(500);
                
                // İndeksler
                entity.HasIndex(cu => cu.CouponId);
                entity.HasIndex(cu => cu.UserId);
                entity.HasIndex(cu => cu.OrderId);
                entity.HasIndex(cu => new { cu.CouponId, cu.UserId });
                
                // Kupon ilişkisi
                entity.HasOne(cu => cu.Coupon)
                      .WithMany(c => c.CouponUsages)
                      .HasForeignKey(cu => cu.CouponId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Kullanıcı ilişkisi
                entity.HasOne(cu => cu.User)
                      .WithMany()
                      .HasForeignKey(cu => cu.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                // Sipariş ilişkisi
                entity.HasOne(cu => cu.Order)
                      .WithMany(o => o.CouponUsages)
                      .HasForeignKey(cu => cu.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Discount>(entity =>
            {
                entity.Property(d => d.Value).HasPrecision(18, 2);
            });

            // -------------------
            // MikroSyncState Configuration
            // Mikro ERP senkronizasyon durumlarını takip eder
            // -------------------
            modelBuilder.Entity<MikroSyncState>(entity =>
            {
                entity.ToTable("MikroSyncStates");
                
                entity.Property(e => e.SyncType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Direction).HasMaxLength(20).IsRequired();
                entity.Property(e => e.LastError).HasMaxLength(1000);
                
                // Unique index: Her sync tipi + yön kombinasyonu için tek kayıt
                entity.HasIndex(e => new { e.SyncType, e.Direction }).IsUnique();
            });

            // -------------------
            // MikroCategoryMapping Configuration
            // Mikro ERP kategorileri ile e-ticaret kategorileri arasındaki eşleme
            // -------------------
            modelBuilder.Entity<MikroCategoryMapping>(entity =>
            {
                entity.ToTable("MikroCategoryMappings");
                
                entity.Property(e => e.MikroAnagrupKod)
                    .HasMaxLength(50)
                    .IsRequired();
                    
                entity.Property(e => e.MikroAltgrupKod)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.MikroMarkaKod)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.MikroGrupAciklama)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Notes)
                    .HasMaxLength(500);
                
                // Kategori ilişkisi
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // Marka ilişkisi (opsiyonel)
                entity.HasOne(e => e.Brand)
                    .WithMany()
                    .HasForeignKey(e => e.BrandId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Composite unique index: Aynı Mikro grup+altgrup+marka kombinasyonu tekrar edemez
                entity.HasIndex(e => new { e.MikroAnagrupKod, e.MikroAltgrupKod, e.MikroMarkaKod })
                    .IsUnique()
                    .HasDatabaseName("IX_MikroCategoryMappings_Unique");
                    
                // Performans için index
                entity.HasIndex(e => e.CategoryId);
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
            // WeightAdjustment Configuration (Ağırlık Fark Yönetimi)
            // -------------------
            modelBuilder.Entity<WeightAdjustment>(entity =>
            {
                entity.ToTable("WeightAdjustments");
                
                // Decimal precision ayarları (para birimi için 18,2)
                entity.Property(e => e.EstimatedWeight).HasPrecision(18, 4);
                entity.Property(e => e.ActualWeight).HasPrecision(18, 4);
                entity.Property(e => e.WeightDifference).HasPrecision(18, 4);
                entity.Property(e => e.DifferencePercent).HasPrecision(18, 4);
                entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedPrice).HasPrecision(18, 2);
                entity.Property(e => e.ActualPrice).HasPrecision(18, 2);
                entity.Property(e => e.PriceDifference).HasPrecision(18, 2);
                entity.Property(e => e.AdminAdjustedPrice).HasPrecision(18, 2);
                
                // String uzunlukları
                entity.Property(e => e.ProductName).HasMaxLength(200);
                entity.Property(e => e.WeighedByCourierName).HasMaxLength(200);
                entity.Property(e => e.AdminUserName).HasMaxLength(200);
                entity.Property(e => e.AdminNote).HasMaxLength(1000);
                entity.Property(e => e.PaymentTransactionId).HasMaxLength(100);
                entity.Property(e => e.NotificationType).HasMaxLength(50);

                // Order ilişkisi - NO ACTION (cascade döngüsü önleme)
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.WeightAdjustments)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.NoAction);

                // OrderItem ilişkisi - NO ACTION (cascade döngüsü önleme)
                entity.HasOne(e => e.OrderItem)
                      .WithMany()
                      .HasForeignKey(e => e.OrderItemId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Product ilişkisi - NO ACTION (ürün silinse bile fark kaydı korunur)
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Courier ilişkisi - SET NULL (kurye silinirse audit bilgisi korunur)
                entity.HasOne(e => e.WeighedByCourier)
                      .WithMany()
                      .HasForeignKey(e => e.WeighedByCourierId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Admin User ilişkisi - SET NULL
                entity.HasOne(e => e.AdminUser)
                      .WithMany()
                      .HasForeignKey(e => e.AdminUserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Index'ler - Performans için kritik sorgular
                entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_WeightAdjustments_OrderId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_WeightAdjustments_Status");
                entity.HasIndex(e => new { e.Status, e.RequiresAdminApproval })
                      .HasDatabaseName("IX_WeightAdjustments_StatusAdmin");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_WeightAdjustments_CreatedAt");
            });

            // -------------------
            // Order için ek decimal precision ayarları (Ağırlık Sistemi)
            // -------------------
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.PreAuthAmount).HasPrecision(18, 2);
                entity.Property(e => e.FinalAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalWeightDifference).HasPrecision(18, 4);
                entity.Property(e => e.TotalPriceDifference).HasPrecision(18, 2);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.PosnetTransactionId).HasMaxLength(100);
            });

            // -------------------
            // OrderItem için ek decimal precision ayarları (Ağırlık Sistemi)
            // -------------------
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.EstimatedWeight).HasPrecision(18, 4);
                entity.Property(e => e.ActualWeight).HasPrecision(18, 4);
                entity.Property(e => e.EstimatedPrice).HasPrecision(18, 2);
                entity.Property(e => e.ActualPrice).HasPrecision(18, 2);
                entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.WeightDifference).HasPrecision(18, 4);
                entity.Property(e => e.PriceDifference).HasPrecision(18, 2);
            });

            // -------------------
            // Product için ek decimal precision ayarları (Ağırlık Sistemi)
            // -------------------
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
                entity.Property(e => e.MinOrderWeight).HasPrecision(18, 4);
                entity.Property(e => e.MaxOrderWeight).HasPrecision(18, 4);
                entity.Property(e => e.WeightTolerancePercent).HasPrecision(5, 2);
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // NEWSLETTER (BÜLTEN) SİSTEMİ KONFIGÜRASYONU
            // Email ve UnsubscribeToken için index, unique constraint ve ilişki tanımları
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<NewsletterSubscriber>(entity =>
            {
                entity.ToTable("NewsletterSubscribers");

                // Email alanı - benzersiz olmalı, case-insensitive arama için index
                entity.Property(e => e.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                // Email için unique index
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_NewsletterSubscribers_Email");

                // UnsubscribeToken için unique index - hızlı token araması için
                entity.Property(e => e.UnsubscribeToken)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.HasIndex(e => e.UnsubscribeToken)
                    .IsUnique()
                    .HasDatabaseName("IX_NewsletterSubscribers_UnsubscribeToken");

                // Kaynak alanı
                entity.Property(e => e.Source)
                    .HasMaxLength(50)
                    .HasDefaultValue("web_footer");

                // FullName alanı
                entity.Property(e => e.FullName)
                    .HasMaxLength(100);

                // IP adresi
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45); // IPv6 desteği

                // ConfirmationToken
                entity.Property(e => e.ConfirmationToken)
                    .HasMaxLength(64);

                // Aktif aboneler için composite index (toplu mail gönderiminde performans)
                entity.HasIndex(e => new { e.IsActive, e.IsConfirmed })
                    .HasDatabaseName("IX_NewsletterSubscribers_Active_Confirmed");

                // User ilişkisi (opsiyonel)
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silinirse ilişki null olur
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // KARGO ÜCRETİ AYARLARI (ShippingSetting) KONFİGÜRASYONU
            // Araç tipine göre dinamik fiyatlandırma
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<ShippingSetting>(entity =>
            {
                entity.ToTable("ShippingSettings");

                // VehicleType - Benzersiz olmalı (motorcycle, car vb.)
                entity.Property(e => e.VehicleType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.VehicleType)
                    .IsUnique()
                    .HasDatabaseName("IX_ShippingSettings_VehicleType");

                // DisplayName - Türkçe görüntüleme adı
                entity.Property(e => e.DisplayName)
                    .HasMaxLength(100)
                    .IsRequired();

                // Price - Kargo ücreti (TL)
                entity.Property(e => e.Price)
                    .HasPrecision(18, 2)
                    .IsRequired();

                // EstimatedDeliveryTime
                entity.Property(e => e.EstimatedDeliveryTime)
                    .HasMaxLength(100);

                // Description
                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // MaxWeight ve MaxVolume
                entity.Property(e => e.MaxWeight)
                    .HasPrecision(18, 2);

                entity.Property(e => e.MaxVolume)
                    .HasPrecision(18, 2);

                // Audit alanları
                entity.Property(e => e.UpdatedByUserName)
                    .HasMaxLength(200);

                // Aktif kayıtları hızlı sorgulamak için index
                entity.HasIndex(e => new { e.IsActive, e.SortOrder })
                    .HasDatabaseName("IX_ShippingSettings_Active_SortOrder");
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // ANA SAYFA ÜRÜN BLOK SİSTEMİ
            // HomeProductBlock ve HomeBlockProduct entity konfigürasyonları
            // ═══════════════════════════════════════════════════════════════════════════════

            // -------------------
            // HomeProductBlock Configuration
            // Ana sayfa ürün bloklarını tutar (İndirimli Ürünler, Süt Ürünleri vb.)
            // -------------------
            modelBuilder.Entity<HomeProductBlock>(entity =>
            {
                entity.ToTable("HomeProductBlocks");
                
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Slug)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.BlockType)
                    .HasMaxLength(50)
                    .IsRequired()
                    .HasDefaultValue("manual");

                entity.Property(e => e.PosterImageUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.BackgroundColor)
                    .HasMaxLength(20);

                entity.Property(e => e.ViewAllUrl)
                    .HasMaxLength(300);

                entity.Property(e => e.ViewAllText)
                    .HasMaxLength(50)
                    .HasDefaultValue("Tümünü Gör");

                entity.Property(e => e.MaxProductCount)
                    .HasDefaultValue(6);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Slug benzersiz olmalı
                entity.HasIndex(e => e.Slug)
                    .IsUnique()
                    .HasDatabaseName("IX_HomeProductBlocks_Slug");

                // Aktif blokları sıralı getirmek için index
                entity.HasIndex(e => new { e.IsActive, e.DisplayOrder })
                    .HasDatabaseName("IX_HomeProductBlocks_Active_Order");

                // Banner ilişkisi (1 : 0..1)
                entity.HasOne(e => e.Banner)
                    .WithMany()
                    .HasForeignKey(e => e.BannerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Category ilişkisi (1 : 0..1)
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // -------------------
            // HomeBlockProduct Configuration
            // Blok-Ürün many-to-many ilişkisi (manuel seçim için)
            // -------------------
            modelBuilder.Entity<HomeBlockProduct>(entity =>
            {
                entity.ToTable("HomeBlockProducts");

                // Composite Primary Key (BlockId + ProductId)
                entity.HasKey(e => new { e.BlockId, e.ProductId });

                entity.Property(e => e.DisplayOrder)
                    .HasDefaultValue(0);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // HomeProductBlock ilişkisi
                entity.HasOne(e => e.Block)
                    .WithMany(b => b.BlockProducts)
                    .HasForeignKey(e => e.BlockId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Product ilişkisi
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.HomeBlockProducts)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Sıralama için index
                entity.HasIndex(e => new { e.BlockId, e.DisplayOrder })
                    .HasDatabaseName("IX_HomeBlockProducts_Block_Order");
            });

            // ═══════════════════════════════════════════════════════════════════════════════
            // İADE TALEBİ (RefundRequest) KONFİGÜRASYONU
            // Müşteri iade talepleri ve admin onay süreci
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<RefundRequest>(entity =>
            {
                entity.ToTable("RefundRequests");

                // İade tutarı - para birimi hassasiyeti
                entity.Property(e => e.RefundAmount).HasPrecision(18, 2);

                // String alanları - maksimum uzunluklar
                entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.RefundType).HasMaxLength(20).HasDefaultValue("full");
                entity.Property(e => e.OrderStatusAtRequest).HasMaxLength(50);
                entity.Property(e => e.AdminNote).HasMaxLength(2000);
                entity.Property(e => e.PosnetHostLogKey).HasMaxLength(20);
                entity.Property(e => e.TransactionType).HasMaxLength(20);
                entity.Property(e => e.RefundFailureReason).HasMaxLength(1000);

                // Tarih alanları
                entity.Property(e => e.RequestedAt).HasColumnType("datetime2");
                entity.Property(e => e.ProcessedAt).HasColumnType("datetime2");
                entity.Property(e => e.RefundedAt).HasColumnType("datetime2");

                // Order ilişkisi - Restrict: Sipariş silinirse iade talebi korunur
                entity.HasOne(e => e.Order)
                      .WithMany(o => o.RefundRequests)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Talep eden kullanıcı ilişkisi
                // Restrict: Kullanıcı silinemesin eğer iade talebi varsa
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // İşleyen admin ilişkisi
                // Restrict: Admin kullanıcı silinemesin eğer işlediği iade talebi varsa
                entity.HasOne(e => e.ProcessedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ProcessedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // İndeksler - Performans optimizasyonu
                entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_RefundRequests_OrderId");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_RefundRequests_UserId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_RefundRequests_Status");
                entity.HasIndex(e => e.RequestedAt).HasDatabaseName("IX_RefundRequests_RequestedAt");

                // Admin paneli: Bekleyen iade talepleri hızlı sorgusu
                entity.HasIndex(e => new { e.Status, e.RequestedAt })
                      .HasDatabaseName("IX_RefundRequests_Status_RequestedAt");
            });

            // -------------------
            // Seed Data
            // -------------------
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // ═══════════════════════════════════════════════════════════════════════════════
            // KARGO ÜCRETİ VARSAYILAN SEED DATA
            // Motosiklet: 40 TL, Araba: 60 TL (Admin panelden değiştirilebilir)
            // ═══════════════════════════════════════════════════════════════════════════════
            modelBuilder.Entity<ShippingSetting>().HasData(
                new ShippingSetting
                {
                    Id = 1,
                    VehicleType = "motorcycle",
                    DisplayName = "Motosiklet ile Teslimat",
                    Price = 40.00m,
                    EstimatedDeliveryTime = "30-45 dakika",
                    Description = "Hızlı teslimat, küçük ve orta boy paketler için ideal",
                    SortOrder = 1,
                    MaxWeight = 15.0m,  // Motosiklet max 15 kg
                    MaxVolume = null,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = null,
                    UpdatedByUserId = null,
                    UpdatedByUserName = null
                },
                new ShippingSetting
                {
                    Id = 2,
                    VehicleType = "car",
                    DisplayName = "Araç ile Teslimat",
                    Price = 60.00m,
                    EstimatedDeliveryTime = "1-2 saat",
                    Description = "Büyük paketler ve ağır ürünler için uygun",
                    SortOrder = 2,
                    MaxWeight = 100.0m, // Araba max 100 kg
                    MaxVolume = null,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = null,
                    UpdatedByUserId = null,
                    UpdatedByUserName = null
                }
            );
        }
    }
}
