using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Ürün listeleme ve detay için kullanılan DTO.
    /// Kampanya bilgilerini de içerir.
    /// </summary>
    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Slug { get; set; }
        
        /// <summary>
        /// Ürünün normal fiyatı
        /// </summary>
        public decimal Price { get; set; }
        
        /// <summary>
        /// Kampanyalı/indirimli fiyat (varsa)
        /// null ise indirim yok demektir
        /// </summary>
        public decimal? SpecialPrice { get; set; }
        
        /// <summary>
        /// Orijinal fiyat (kampanya öncesi)
        /// Frontend'de üzeri çizili fiyat gösterimi için kullanılır
        /// </summary>
        public decimal? OriginalPrice { get; set; }
        
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Brand { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        
        #region Kampanya Bilgileri
        
        /// <summary>
        /// Uygulanan kampanya ID'si (varsa)
        /// </summary>
        public int? CampaignId { get; set; }
        
        /// <summary>
        /// Uygulanan kampanya adı (varsa)
        /// Frontend'de "X Kampanyası" gösterimi için
        /// </summary>
        public string? CampaignName { get; set; }
        
        /// <summary>
        /// İndirim yüzdesi (hesaplanmış)
        /// Frontend'de "%20 İndirim" rozeti için
        /// </summary>
        public int? DiscountPercentage { get; set; }
        
        /// <summary>
        /// Kampanya aktif mi?
        /// </summary>
        public bool HasActiveCampaign => CampaignId.HasValue && SpecialPrice.HasValue && SpecialPrice < Price;
        
        #endregion
    }
}
