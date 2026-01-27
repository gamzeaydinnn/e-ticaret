using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Promotions
{
    /// <summary>
    /// Kampanya önizleme sonucu DTO'su.
    /// Kampanyanın etkileyeceği ürünleri ve fiyat değişikliklerini içerir.
    /// </summary>
    public class CampaignPreviewResult
    {
        /// <summary>
        /// Önizleme mesajı (kaç ürün etkilenecek vb.)
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Etkilenecek ürünlerin listesi
        /// </summary>
        public List<CampaignPreviewProduct> AffectedProducts { get; set; } = new();
        
        /// <summary>
        /// Toplam indirim tutarı (tüm ürünlerin indirimlerinin toplamı)
        /// </summary>
        public decimal TotalDiscount { get; set; }
        
        /// <summary>
        /// Etkilenen toplam ürün sayısı
        /// </summary>
        public int TotalProductCount { get; set; }
        
        /// <summary>
        /// Ortalama indirim yüzdesi
        /// </summary>
        public decimal AverageDiscountPercentage { get; set; }
    }

    /// <summary>
    /// Kampanya önizlemesinde tek bir ürünün bilgisi.
    /// Eski ve yeni fiyat karşılaştırması için kullanılır.
    /// </summary>
    public class CampaignPreviewProduct
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Ürün adı
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// Kategori adı
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;
        
        /// <summary>
        /// Orijinal fiyat (kampanya öncesi)
        /// </summary>
        public decimal OriginalPrice { get; set; }
        
        /// <summary>
        /// Yeni fiyat (kampanya sonrası)
        /// </summary>
        public decimal NewPrice { get; set; }
        
        /// <summary>
        /// İndirim tutarı (TL)
        /// </summary>
        public decimal DiscountAmount { get; set; }
        
        /// <summary>
        /// İndirim yüzdesi
        /// </summary>
        public decimal DiscountPercentage { get; set; }
    }
}
