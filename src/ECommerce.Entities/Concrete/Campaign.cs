using System;
using System.Collections.Generic;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kampanya entity'si - Otomatik uygulanan indirimler ve promosyonlar.
    /// Kupondan farklı olarak kullanıcı kod girmez, koşullar sağlandığında otomatik uygulanır.
    /// Desteklenen türler: Yüzdelik indirim, Sabit tutar, X Al Y Öde, Ücretsiz Kargo
    /// </summary>
    public class Campaign : BaseEntity
    {
        #region Temel Bilgiler

        /// <summary>
        /// Kampanya adı (Admin panelde ve kullanıcıya gösterilir)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kampanya açıklaması (Opsiyonel, detaylı bilgi için)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Kampanya başlangıç tarihi (Bu tarihten önce uygulanmaz)
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Kampanya bitiş tarihi (Bu tarihten sonra uygulanmaz)
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Kampanya aktif mi? (false ise hiç uygulanmaz)
        /// BaseEntity'deki IsActive'i override eder
        /// </summary>
        public new bool IsActive { get; set; } = true;

        #endregion

        #region Kampanya Türü ve Hedefi

        /// <summary>
        /// Kampanya türü: Percentage, FixedAmount, BuyXPayY, FreeShipping
        /// Her tür farklı hesaplama mantığı kullanır
        /// </summary>
        public CampaignType Type { get; set; } = CampaignType.Percentage;

        /// <summary>
        /// Hedef türü: All (tüm sepet), Category (belirli kategoriler), Product (belirli ürünler)
        /// All seçilirse CampaignTargets tablosu kullanılmaz
        /// </summary>
        public CampaignTargetType TargetType { get; set; } = CampaignTargetType.All;

        #endregion

        #region İndirim Değerleri

        /// <summary>
        /// İndirim değeri:
        /// - Percentage için: yüzde (10 = %10)
        /// - FixedAmount için: TL tutarı (50 = 50 TL)
        /// - BuyXPayY ve FreeShipping için kullanılmaz
        /// </summary>
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Maksimum indirim tutarı (Opsiyonel)
        /// Yüzdelik indirimlerde üst limit koymak için kullanılır
        /// Örn: %20 indirim, max 100 TL
        /// </summary>
        public decimal? MaxDiscountAmount { get; set; }

        #endregion

        #region Koşullar

        /// <summary>
        /// Minimum sepet tutarı (Opsiyonel)
        /// Bu tutarın altındaki sepetlere kampanya uygulanmaz
        /// Özellikle FreeShipping kampanyaları için kullanışlı
        /// </summary>
        public decimal? MinCartTotal { get; set; }

        /// <summary>
        /// Minimum ürün adedi (Opsiyonel)
        /// Kampanya kapsamındaki ürünlerden en az bu kadar olmalı
        /// BuyXPayY için zorunlu (BuyQty ile aynı olabilir)
        /// </summary>
        public int? MinQuantity { get; set; }

        #endregion

        #region X Al Y Öde Parametreleri

        /// <summary>
        /// "X Al Y Öde" kampanyasında alınması gereken adet
        /// Örn: "3 Al 2 Öde" için BuyQty = 3
        /// </summary>
        public int? BuyQty { get; set; }

        /// <summary>
        /// "X Al Y Öde" kampanyasında ödenecek adet
        /// Örn: "3 Al 2 Öde" için PayQty = 2
        /// Bedava adet = BuyQty - PayQty
        /// </summary>
        public int? PayQty { get; set; }

        #endregion

        #region Öncelik ve Yığılma

        /// <summary>
        /// Kampanya önceliği (düşük değer = yüksek öncelik)
        /// Aynı ürüne birden fazla kampanya uygunsa, en düşük Priority'li uygulanır
        /// Varsayılan: 100
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Kampanyanın diğer kampanyalarla birlikte uygulanıp uygulanamayacağı
        /// true: Bu kampanya diğerleriyle birlikte çalışabilir
        /// false: Bu kampanya tek başına çalışır, diğerleri devre dışı kalır
        /// Varsayılan: true (stackable)
        /// NOT: Şimdilik kullanılmıyor, ileride genişletme için
        /// </summary>
        public bool IsStackable { get; set; } = true;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Kampanya hedefleri (ürün veya kategori ID'leri)
        /// TargetType = All ise bu collection boş olabilir
        /// </summary>
        public virtual ICollection<CampaignTarget> Targets { get; set; } = new HashSet<CampaignTarget>();

        /// <summary>
        /// Geriye dönük uyumluluk için eski Rules collection'ı
        /// Yeni kampanyalarda kullanılmaz, eski veriler için tutulur
        /// </summary>
        [Obsolete("Yeni kampanyalar için doğrudan Campaign property'lerini kullanın")]
        public virtual ICollection<CampaignRule> Rules { get; set; } = new HashSet<CampaignRule>();

        /// <summary>
        /// Geriye dönük uyumluluk için eski Rewards collection'ı
        /// Yeni kampanyalarda kullanılmaz, eski veriler için tutulur
        /// </summary>
        [Obsolete("Yeni kampanyalar için doğrudan Campaign property'lerini kullanın")]
        public virtual ICollection<CampaignReward> Rewards { get; set; } = new HashSet<CampaignReward>();

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kampanyanın şu anda geçerli olup olmadığını kontrol eder
        /// IsActive && StartDate <= now && EndDate >= now
        /// Tarih karşılaştırması: Önce UTC, sonra yerel zaman denenir
        /// </summary>
        public bool IsCurrentlyValid()
        {
            if (!IsActive) return false;
            
            var nowUtc = DateTime.UtcNow;
            var nowLocal = DateTime.Now;
            
            // UTC ile kontrol
            if (StartDate <= nowUtc && EndDate >= nowUtc)
                return true;
            
            // Yerel zaman ile kontrol (tarihlerin yerel zaman olarak kaydedilmiş olabileceği için)
            if (StartDate <= nowLocal && EndDate >= nowLocal)
                return true;
            
            // Sadece tarih karşılaştırması (saat farkı sorunu için)
            if (StartDate.Date <= nowLocal.Date && EndDate.Date >= nowLocal.Date)
                return true;
                
            return false;
        }

        /// <summary>
        /// Kampanyanın belirtilen sepet tutarı için geçerli olup olmadığını kontrol eder
        /// MinCartTotal koşulunu kontrol eder
        /// </summary>
        public bool IsValidForCartTotal(decimal cartTotal)
        {
            if (!MinCartTotal.HasValue) return true;
            return cartTotal >= MinCartTotal.Value;
        }

        /// <summary>
        /// Kampanyanın belirtilen ürün adedi için geçerli olup olmadığını kontrol eder
        /// MinQuantity koşulunu kontrol eder
        /// </summary>
        public bool IsValidForQuantity(int quantity)
        {
            if (!MinQuantity.HasValue) return true;
            return quantity >= MinQuantity.Value;
        }

        #endregion
    }
}

