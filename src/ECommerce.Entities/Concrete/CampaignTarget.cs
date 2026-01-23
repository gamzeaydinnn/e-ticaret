using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kampanya hedeflerini tutan entity.
    /// Bir kampanya birden fazla ürün veya kategoriye hedeflenebilir.
    /// Campaign.TargetType = All ise bu tablo kullanılmaz.
    /// 
    /// Örnek:
    /// - Campaign "Meyve Festivali" -> TargetType = Category -> CampaignTarget'ta [Meyve, Sebze] kategori ID'leri
    /// - Campaign "iPhone İndirimi" -> TargetType = Product -> CampaignTarget'ta [iPhone 14, iPhone 15] ürün ID'leri
    /// </summary>
    public class CampaignTarget : BaseEntity
    {
        /// <summary>
        /// İlişkili kampanya ID'si
        /// </summary>
        public int CampaignId { get; set; }

        /// <summary>
        /// Hedef ID (ProductId veya CategoryId)
        /// TargetKind'a göre yorumlanır
        /// </summary>
        public int TargetId { get; set; }

        /// <summary>
        /// Hedef türü: Product veya Category
        /// Campaign.TargetType ile tutarlı olmalı
        /// </summary>
        public CampaignTargetKind TargetKind { get; set; }

        #region Navigation Properties

        /// <summary>
        /// İlişkili kampanya
        /// </summary>
        public virtual Campaign Campaign { get; set; } = null!;

        // Not: Product ve Category navigation property'leri kaldırıldı
        // Çünkü TargetId tek bir FK olarak hem Product hem Category'ye referans veremez
        // Bunun yerine servis katmanında manuel olarak join yapılacak

        #endregion
    }
}
