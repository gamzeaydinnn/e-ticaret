using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ana sayfa banner/poster entity'si
    /// Slider görselleri, promosyon kartları ve genel banner'lar için kullanılır
    /// Admin panelinden tam kontrol edilebilir
    /// </summary>
    public class Banner
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Banner başlığı (görüntülenebilir)
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Alt başlık veya açıklama metni
        /// Slider'da gösterilecek ek metin
        /// </summary>
        public string? SubTitle { get; set; }
        
        /// <summary>
        /// Detaylı açıklama (opsiyonel)
        /// Admin panelinde kullanılır
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Görsel URL'i (/uploads/banners/xxx.jpg veya /images/xxx.png)
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Tıklandığında yönlendirilecek URL
        /// </summary>
        public string LinkUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Buton metni (Slider üzerinde gösterilecek CTA butonu)
        /// Örnek: "Hemen İncele", "Alışverişe Başla"
        /// </summary>
        public string? ButtonText { get; set; }
        
        /// <summary>
        /// Banner tipi: slider, promo, banner
        /// slider: Ana sayfa büyük slider (önerilen: 1200x400px)
        /// promo: Küçük promosyon kartları (önerilen: 300x200px)
        /// banner: Genel banner (önerilen: 800x200px)
        /// </summary>
        public string Type { get; set; } = "slider";
        
        /// <summary>
        /// Banner'ın gösterim pozisyonu/konumu
        /// Örnek: homepage-top, homepage-middle, sidebar
        /// </summary>
        public string? Position { get; set; }
        
        /// <summary>
        /// Aktif/Pasif durumu
        /// Pasif banner'lar public API'de gösterilmez
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Gösterim sırası (küçükten büyüğe sıralanır)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        
        /// <summary>
        /// Gösterime başlangıç tarihi (opsiyonel)
        /// Belirtilmişse bu tarihten önce gösterilmez
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Gösterim bitiş tarihi (opsiyonel)
        /// Belirtilmişse bu tarihten sonra gösterilmez
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Tıklanma sayısı (analytics için)
        /// </summary>
        public int ClickCount { get; set; } = 0;
        
        /// <summary>
        /// Görüntülenme sayısı (analytics için)
        /// </summary>
        public int ViewCount { get; set; } = 0;
        
        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}