using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Core.DTOs.Category;

namespace ECommerce.Core.DTOs.HomeBlock
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu DTO
    /// ------------------------------------------------
    /// Ana sayfadaki ürün bloklarını frontend'e taşımak için kullanılır.
    /// Her blok poster + ürün listesi şeklinde görüntülenir.
    /// </summary>
    public class HomeProductBlockDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Blok adı - Admin panelde görünür (internal name)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Müşterilere gösterilen başlık - Ana sayfada görünür
        /// Örnek: "Bu Fırsatları Kaçırmayın", "Haftalık İndirimler"
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// URL dostu slug - "indirimli-urunler"
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Blok açıklaması (opsiyonel)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Blok tipi: manual, category, discounted, newest, bestseller
        /// </summary>
        public string BlockType { get; set; } = "manual";

        /// <summary>
        /// Kategori bazlı bloklar için eski kategori ID (Geriye dönük uyumluluk için)
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Kategori bazlı bloklar için hedef kategori ID'si (Üst ve alt kategorileri destekler)
        /// </summary>
        public int? TargetCategoryId { get; set; }

        /// <summary>
        /// Zenginleştirilmiş hedef kategori bilgisi ve hiyerarşi breadcrumb'ı
        /// Örnek: "Gıda > Süt Ürünleri"
        /// </summary>
        public CategoryBreadcrumbDto? TargetCategoryInfo { get; set; }

        /// <summary>
        /// Kategori bazlı bloklar için kategori adı (read-only)
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// Kategori bazlı bloklar için kategori slug'ı (read-only)
        /// </summary>
        public string? CategorySlug { get; set; }

        /// <summary>
        /// İlişkili banner ID
        /// </summary>
        public int? BannerId { get; set; }

        /// <summary>
        /// İlişkili banner detayları (opsiyonel zengin veri)
        /// </summary>
        public BannerDto? Banner { get; set; }

        /// <summary>
        /// Poster görsel URL'i (Banner veya direkt)
        /// </summary>
        public string? PosterImageUrl { get; set; }

        /// <summary>
        /// Doğrudan poster tıklama linki (Banner kullanılmadığında veya alternatif olarak)
        /// </summary>
        public string? PosterLinkUrl { get; set; }

        /// <summary>
        /// Çözümlenmiş nihai poster tıklama hedefi (Computed)
        /// Öncelik sırası: Banner.LinkUrl > PosterLinkUrl > null
        /// </summary>
        public string? PosterClickTarget { get; set; }

        /// <summary>
        /// Arka plan rengi (hex)
        /// </summary>
        public string? BackgroundColor { get; set; }

        /// <summary>
        /// Gösterim sırası
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Maksimum ürün sayısı
        /// </summary>
        public int MaxProductCount { get; set; } = 6;

        /// <summary>
        /// "Tümünü Gör" hedef URL
        /// </summary>
        public string? ViewAllUrl { get; set; }

        /// <summary>
        /// "Tümünü Gör" buton metni
        /// </summary>
        public string ViewAllText { get; set; } = "Tümünü Gör";

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gösterime başlangıç tarihi
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gösterim bitiş tarihi
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Poster tıklanma sayısı (analitik)
        /// </summary>
        public int ClickCount { get; set; } = 0;

        /// <summary>
        /// Blok görüntülenme sayısı (analitik - Intersection Observer)
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Tıklama oranı (CTR - Click-Through Rate) %: (ClickCount / ViewCount) * 100
        /// </summary>
        public decimal ClickThroughRate { get; set; } = 0m;

        /// <summary>
        /// Bloktaki ürünler - Public API için doldurulur
        /// </summary>
        public List<HomeBlockProductItemDto> Products { get; set; } = new();
    }

    /// <summary>
    /// Blok içindeki ürün DTO'su
    /// Sadece blok gösterimi için gerekli alanları içerir (lightweight)
    /// </summary>
    public class HomeBlockProductItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SpecialPrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        
        /// <summary>
        /// İndirim yüzdesi (hesaplanmış)
        /// </summary>
        public int? DiscountPercent { get; set; }

        /// <summary>
        /// Blok içindeki sıralama (manuel seçim için)
        /// </summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Blok oluşturma isteği DTO'su
    /// </summary>
    public class CreateHomeBlockDto
    {
        [Required(ErrorMessage = "Blok adı zorunludur")]
        [MaxLength(100, ErrorMessage = "Blok adı en fazla 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Müşteri başlığı en fazla 200 karakter olabilir")]
        public string? Title { get; set; }

        [MaxLength(150, ErrorMessage = "Slug en fazla 150 karakter olabilir")]
        public string? Slug { get; set; }

        [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Blok tipi zorunludur")]
        public string BlockType { get; set; } = "manual";

        /// <summary>
        /// Geriye dönük uyumluluk için eski kategori alanı
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Hedef kategori ID'si (Üst ve alt kategorileri destekler)
        /// </summary>
        public int? TargetCategoryId { get; set; }

        public int? BannerId { get; set; }

        [MaxLength(500, ErrorMessage = "Görsel URL en fazla 500 karakter olabilir")]
        public string? PosterImageUrl { get; set; }

        /// <summary>
        /// Direkt poster tıklama URL'i (opsiyonel)
        /// http://, https:// veya / ile başlamalıdır
        /// </summary>
        [MaxLength(500, ErrorMessage = "Poster hedef URL en fazla 500 karakter olabilir")]
        [RegularExpression(@"^(https?://|/)", ErrorMessage = "URL http://, https:// veya / ile başlamalıdır")]
        public string? PosterLinkUrl { get; set; }

        [MaxLength(7, ErrorMessage = "Renk kodu en fazla 7 karakter olabilir")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz (#RRGGBB)")]
        public string? BackgroundColor { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [Range(1, 50, ErrorMessage = "Maksimum ürün sayısı 1 ile 50 arasında olmalıdır")]
        public int MaxProductCount { get; set; } = 6;

        [MaxLength(300, ErrorMessage = "Tümünü Gör URL en fazla 300 karakter olabilir")]
        public string? ViewAllUrl { get; set; }

        [MaxLength(50, ErrorMessage = "Tümünü Gör metni en fazla 50 karakter olabilir")]
        public string ViewAllText { get; set; } = "Tümünü Gör";

        public bool IsActive { get; set; } = true;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Manuel bloklar için başlangıçta atanacak ürün ID listesi
        /// </summary>
        public List<int>? ProductIds { get; set; }
    }

    /// <summary>
    /// Blok güncelleme isteği DTO'su
    /// </summary>
    public class UpdateHomeBlockDto : CreateHomeBlockDto
    {
        [Required(ErrorMessage = "Blok ID zorunludur")]
        public int Id { get; set; }
    }

    /// <summary>
    /// Bloğa ürün ekleme isteği
    /// </summary>
    public class AddProductToBlockDto
    {
        public int BlockId { get; set; }
        public int ProductId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Bloktaki ürünleri toplu güncelleme isteği (sıralama vb.)
    /// </summary>
    public class UpdateBlockProductsDto
    {
        public int BlockId { get; set; }
        
        /// <summary>
        /// Yeni ürün sıralaması - ProductId ve DisplayOrder listesi
        /// </summary>
        public List<BlockProductOrderDto> Products { get; set; } = new();
    }

    /// <summary>
    /// Ürün sıralama bilgisi
    /// </summary>
    public class BlockProductOrderDto
    {
        public int ProductId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
