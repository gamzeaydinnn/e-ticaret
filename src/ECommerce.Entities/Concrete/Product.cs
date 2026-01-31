using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }

        // Brand artık string değil, entity ile ilişki olacak
        public int? BrandId { get; set; }         // Foreign key
        public virtual Brand? Brand { get; set; } // Navigation property

        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0m;
        public decimal? SpecialPrice { get; set; }
        public int StockQuantity { get; set; } = 0;
        public string ImageUrl { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Ürün birim ağırlığı (gram)
        /// Tartı entegrasyonu için gerekli
        /// </summary>
        public int UnitWeightGrams { get; set; } = 0;

        #region Ağırlık Bazlı Satış Alanları
        
        /// <summary>
        /// Ürün ağırlık bazlı mı satılıyor?
        /// true: kg/gram bazlı (domates, peynir vb.)
        /// false: adet bazlı (şişe su, paket makarna vb.)
        /// </summary>
        public bool IsWeightBased { get; set; } = false;

        /// <summary>
        /// Ağırlık birimi (kg, gram, litre vb.)
        /// Sadece IsWeightBased=true olan ürünlerde kullanılır
        /// </summary>
        public WeightUnit WeightUnit { get; set; } = WeightUnit.Piece;

        /// <summary>
        /// Birim fiyatı (örn: 1 kg = 50₺)
        /// IsWeightBased=true ise bu fiyat kullanılır
        /// IsWeightBased=false ise normal Price kullanılır
        /// </summary>
        public decimal PricePerUnit { get; set; } = 0m;

        /// <summary>
        /// Minimum sipariş ağırlığı (gram cinsinden)
        /// Örn: En az 100 gram sipariş edilebilir
        /// 0 = limit yok
        /// </summary>
        public decimal MinOrderWeight { get; set; } = 0m;

        /// <summary>
        /// Maksimum sipariş ağırlığı (gram cinsinden)
        /// Örn: En fazla 5000 gram (5 kg) sipariş edilebilir
        /// 0 = limit yok
        /// </summary>
        public decimal MaxOrderWeight { get; set; } = 0m;

        /// <summary>
        /// Ağırlık bazlı ürünlerde tahmini tolerans yüzdesi
        /// Müşteriye bilgi amaçlı gösterilir (örn: "±%10 fark olabilir")
        /// </summary>
        public decimal WeightTolerancePercent { get; set; } = 10m;

        #endregion

        // Navigation
        public virtual Category Category { get; set; } = null!;
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new HashSet<ProductVariant>();
        public virtual ICollection<Discount> Discounts { get; set; } = new HashSet<Discount>();
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new HashSet<ProductReview>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new HashSet<Favorite>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new HashSet<ProductImage>();
        public virtual ICollection<StockReservation> StockReservations { get; set; } = new HashSet<StockReservation>();
        
        /// <summary>
        /// Ürüne özel kuponlar - Many-to-many ilişki
        /// Bu ürünün geçerli olduğu kuponları listeler
        /// </summary>
        public virtual ICollection<CouponProduct> CouponProducts { get; set; } = new HashSet<CouponProduct>();

        /// <summary>
        /// Ana sayfa blok ilişkileri - Many-to-many ilişki
        /// Bu ürünün hangi ana sayfa bloklarında gösterildiğini belirtir
        /// Sadece manuel seçimli bloklar (BlockType = "manual") için kullanılır
        /// </summary>
        public virtual ICollection<HomeBlockProduct> HomeBlockProducts { get; set; } = new HashSet<HomeBlockProduct>();
    }

}
