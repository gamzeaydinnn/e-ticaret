using System;

namespace ECommerce.Core.DTOs
{
    /// <summary>
    /// Banner Data Transfer Object
    /// Frontend ve API arasında banner verisi taşımak için kullanılır
    /// </summary>
    public class BannerDto
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Banner başlığı
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Alt başlık veya açıklama metni
        /// </summary>
        public string? SubTitle { get; set; }
        
        /// <summary>
        /// Detaylı açıklama (admin paneli için)
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Görsel URL'i
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Tıklandığında yönlendirilecek URL
        /// </summary>
        public string LinkUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Buton metni (CTA)
        /// </summary>
        public string? ButtonText { get; set; }
        
        /// <summary>
        /// Banner tipi: slider, promo, banner
        /// </summary>
        public string Type { get; set; } = "slider";
        
        /// <summary>
        /// Gösterim pozisyonu
        /// </summary>
        public string? Position { get; set; }
        
        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Gösterim sırası
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        
        /// <summary>
        /// Gösterime başlangıç tarihi
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Gösterim bitiş tarihi
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Tıklanma sayısı
        /// </summary>
        public int ClickCount { get; set; } = 0;
        
        /// <summary>
        /// Görüntülenme sayısı
        /// </summary>
        public int ViewCount { get; set; } = 0;
        
        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}