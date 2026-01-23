using System;
using System.Collections.Generic;
using ECommerce.Core.DTOs.Promotions;

namespace ECommerce.Core.DTOs.Pricing
{
    /// <summary>
    /// Sepet fiyatlandırma sonucu.
    /// Tüm sepet için hesaplanan fiyat, indirim, kupon ve kampanya bilgilerini içerir.
    /// 
    /// İndirim hiyerarşisi:
    /// 1. Ürün indirimleri (ürün özel fiyatı, miktar indirimi vs.) - satır bazlı
    /// 2. Kampanya indirimleri (Percentage, FixedAmount, BuyXPayY) - satır bazlı
    /// 3. Kupon indirimi - sipariş toplamı bazlı
    /// 4. Kargo ücreti (FreeShipping kampanyası varsa 0)
    /// </summary>
    public class CartPricingResultDto
    {
        /// <summary>
        /// Sepet satırları ve her satır için fiyat/kampanya bilgileri
        /// </summary>
        public List<CartItemPricingDto> Items { get; set; } = new();
        
        /// <summary>
        /// Ara toplam (tüm satırların indirimler öncesi toplamı)
        /// </summary>
        public decimal Subtotal { get; set; }
        
        /// <summary>
        /// Toplam kampanya indirimi
        /// </summary>
        public decimal CampaignDiscountTotal { get; set; }
        
        /// <summary>
        /// Kupon indirimi
        /// </summary>
        public decimal CouponDiscountTotal { get; set; }
        
        /// <summary>
        /// Kargo ücreti
        /// </summary>
        public decimal DeliveryFee { get; set; }
        
        /// <summary>
        /// Genel toplam (Subtotal - CampaignDiscountTotal - CouponDiscountTotal + DeliveryFee)
        /// </summary>
        public decimal GrandTotal { get; set; }
        
        /// <summary>
        /// Uygulanan kupon kodu
        /// </summary>
        public string? AppliedCouponCode { get; set; }
        
        /// <summary>
        /// Uygulanan kampanya adları listesi (geriye dönük uyumluluk)
        /// </summary>
        public List<string> AppliedCampaignNames { get; set; } = new();
        
        #region Yeni Kampanya Bilgileri
        
        /// <summary>
        /// Uygulanan kampanyaların detaylı listesi
        /// Her kampanya için ID, ad, tür, indirim tutarı ve uygulandığı ürünler
        /// </summary>
        public List<AppliedCampaignDto> AppliedCampaigns { get; set; } = new();
        
        /// <summary>
        /// Ücretsiz kargo kampanyası aktif mi?
        /// True ise DeliveryFee = 0 olmalı
        /// </summary>
        public bool IsFreeShipping { get; set; }
        
        /// <summary>
        /// Ücretsiz kargo kampanyası adı (varsa)
        /// </summary>
        public string? FreeShippingCampaignName { get; set; }
        
        #endregion
        
        #region Özet Bilgiler
        
        /// <summary>
        /// Toplam indirim (CampaignDiscountTotal + CouponDiscountTotal)
        /// </summary>
        public decimal TotalDiscount => CampaignDiscountTotal + CouponDiscountTotal;
        
        /// <summary>
        /// Kargo indirimi dahil toplam tasarruf
        /// </summary>
        public decimal TotalSavings => TotalDiscount + (IsFreeShipping ? DeliveryFee : 0);
        
        /// <summary>
        /// Uygulanan toplam kampanya sayısı
        /// </summary>
        public int AppliedCampaignCount => AppliedCampaigns.Count;
        
        #endregion
    }
}

