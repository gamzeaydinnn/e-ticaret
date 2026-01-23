using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Pricing
{
    /// <summary>
    /// Sepet satırı fiyatlandırma sonucu.
    /// Her bir ürün satırı için fiyat, indirim ve kampanya bilgilerini içerir.
    /// </summary>
    public class CartItemPricingDto
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Kategori ID (kampanya hesaplaması için)
        /// </summary>
        public int CategoryId { get; set; }
        
        /// <summary>
        /// Ürün adı
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Ürün adedi
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// Birim fiyat
        /// </summary>
        public decimal UnitPrice { get; set; }
        
        /// <summary>
        /// Satır toplam tutarı (UnitPrice * Quantity)
        /// </summary>
        public decimal LineBaseTotal { get; set; }
        
        /// <summary>
        /// Ürün bazlı indirim (ürün özel fiyatı vs.)
        /// </summary>
        public decimal LineDiscountTotal { get; set; }
        
        /// <summary>
        /// Kampanya indirimi (ayrı gösterilir)
        /// </summary>
        public decimal LineCampaignDiscount { get; set; }
        
        /// <summary>
        /// Satır son tutar (LineBaseTotal - LineDiscountTotal - LineCampaignDiscount)
        /// </summary>
        public decimal LineFinalTotal { get; set; }
        
        #region Kampanya Bilgileri
        
        /// <summary>
        /// Uygulanan kampanya ID (varsa)
        /// </summary>
        public int? AppliedCampaignId { get; set; }
        
        /// <summary>
        /// Uygulanan kampanya adı
        /// </summary>
        public string? AppliedCampaignName { get; set; }
        
        /// <summary>
        /// Uygulanan kampanya türü
        /// </summary>
        public CampaignType? AppliedCampaignType { get; set; }
        
        /// <summary>
        /// Kampanya gösterim metni (örn: "%10 İndirim", "3 Al 2 Öde")
        /// </summary>
        public string? CampaignDisplayText { get; set; }
        
        #endregion
    }
}

