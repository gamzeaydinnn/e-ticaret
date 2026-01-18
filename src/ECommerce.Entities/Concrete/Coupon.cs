// =============================================================================
// Coupon Entity - E-Ticaret Kupon Sistemi
// =============================================================================
// Bu entity, sistemdeki tüm kuponları temsil eder.
// Kuponlar farklı türlerde olabilir (yüzde, sabit, ilk sipariş, kargo bedava vb.)
// Kullanım limiti, kategori/ürün bazlı kısıtlamalar desteklenir.
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kupon entity'si - İndirim kuponlarını temsil eder.
    /// Farklı kupon türleri ve kullanım kısıtlamaları destekler.
    /// </summary>
    public class Coupon : BaseEntity
    {
        // =============================================================================
        // Temel Kupon Bilgileri
        // =============================================================================

        /// <summary>
        /// Kupon kodu - Kullanıcının gireceği benzersiz kod
        /// Büyük/küçük harf duyarsız olarak kontrol edilmeli
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Kupon başlığı/açıklaması - Admin panelinde görüntüleme için
        /// </summary>
        [StringLength(200)]
        public string? Title { get; set; }

        /// <summary>
        /// Kupon açıklaması - Detaylı bilgi
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        // =============================================================================
        // Kupon Türü ve Değeri
        // =============================================================================

        /// <summary>
        /// Kupon türü - İndirim hesaplama mantığını belirler
        /// </summary>
        public CouponType Type { get; set; } = CouponType.Percentage;

        /// <summary>
        /// Geriye dönük uyumluluk için - Yüzde mi sabit mi?
        /// Type == Percentage ise true, FixedAmount ise false
        /// NOT: Yeni kodlarda Type alanını kullanın
        /// </summary>
        [Obsolete("Type alanını kullanın. Bu alan geriye dönük uyumluluk için korunuyor.")]
        public bool IsPercentage 
        { 
            get => Type == CouponType.Percentage;
            set => Type = value ? CouponType.Percentage : CouponType.FixedAmount;
        }

        /// <summary>
        /// İndirim değeri
        /// Percentage için: yüzde değeri (ör: 10 = %10)
        /// FixedAmount için: TL değeri (ör: 50 = 50₺)
        /// BuyXGetY için: X değeri (kaç adet al)
        /// </summary>
        [Range(0, 100000)]
        public decimal Value { get; set; }

        /// <summary>
        /// BuyXGetY kuponu için: Y değeri (kaç adet öde)
        /// Örnek: BuyXPayY = 2 ve Value = 3 ise "3 al 2 öde"
        /// </summary>
        public int? BuyXPayY { get; set; }

        /// <summary>
        /// Maksimum indirim tutarı (yüzde indirimlerde tavan)
        /// Örnek: %50 indirim ama max 100₺
        /// null ise sınır yok
        /// </summary>
        public decimal? MaxDiscountAmount { get; set; }

        // =============================================================================
        // Kullanım Kısıtlamaları
        // =============================================================================

        /// <summary>
        /// Minimum sipariş tutarı - Bu tutarın altındaki siparişlerde geçersiz
        /// </summary>
        [Range(0, 1000000)]
        public decimal? MinOrderAmount { get; set; }

        /// <summary>
        /// Kupon son kullanma tarihi
        /// Bu tarihten sonra kupon geçersiz olur
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Kupon başlangıç tarihi (opsiyonel)
        /// Bu tarihten önce kupon kullanılamaz
        /// null ise hemen geçerli
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Toplam kullanım limiti - Tüm kullanıcılar için toplam kaç kez kullanılabilir
        /// 0 veya null ise sınırsız
        /// </summary>
        [Range(0, int.MaxValue)]
        public int UsageLimit { get; set; } = 0;

        /// <summary>
        /// Mevcut kullanım sayısı - Kupon şu ana kadar kaç kez kullanıldı
        /// Her başarılı sipariş sonrası artırılır
        /// </summary>
        public int UsageCount { get; set; } = 0;

        /// <summary>
        /// Kullanıcı başına maksimum kullanım sayısı
        /// null ise sınırsız, 1 ise tek kullanımlık (kullanıcı bazında)
        /// </summary>
        public int? MaxUsagePerUser { get; set; }

        /// <summary>
        /// Tek kullanımlık kupon mu? (Sadece bir kez kullanılabilir, tüm sistem genelinde)
        /// </summary>
        public bool IsSingleUse { get; set; } = false;

        // =============================================================================
        // Kategori ve Ürün Bazlı Kısıtlamalar
        // =============================================================================

        /// <summary>
        /// Kategori bazlı kupon - Sadece bu kategorideki ürünlerde geçerli
        /// null ise tüm kategorilerde geçerli
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Kategori navigation property
        /// </summary>
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        /// <summary>
        /// Alt kategorilerde de geçerli mi?
        /// true ise seçilen kategorinin alt kategorilerinde de kupon geçerli
        /// </summary>
        public bool IncludeSubCategories { get; set; } = true;

        /// <summary>
        /// Ürün bazlı kupon - Sadece bu ürünlerde geçerli (many-to-many)
        /// Boş ise tüm ürünlerde geçerli
        /// </summary>
        public virtual ICollection<CouponProduct> CouponProducts { get; set; } = new HashSet<CouponProduct>();

        // =============================================================================
        // Kullanıcı Kısıtlamaları
        // =============================================================================

        /// <summary>
        /// Sadece belirli kullanıcılar için mi? (private/VIP kupon)
        /// true ise CouponUsers tablosundaki kullanıcılar için geçerli
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        // =============================================================================
        // Ek Koşullar (JSON formatında esnek koşullar)
        // =============================================================================

        /// <summary>
        /// Ek koşullar JSON formatında
        /// Örnek: {"minItemCount": 3, "excludedBrands": [1,2,3]}
        /// </summary>
        public string? ConditionsJson { get; set; }

        // =============================================================================
        // İlişkili Tablolar
        // =============================================================================

        /// <summary>
        /// Kupon kullanım geçmişi - Hangi siparişlerde kullanıldı
        /// </summary>
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new HashSet<CouponUsage>();

        // IsActive already provided by BaseEntity - Kupon aktif/pasif durumu
    }
}
