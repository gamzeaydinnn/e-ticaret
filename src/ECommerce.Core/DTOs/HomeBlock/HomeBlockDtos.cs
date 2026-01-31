using System;
using System.Collections.Generic;

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
        /// Blok adı - "İndirimli Ürünler", "Süt Ürünleri" vb.
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
        /// Kategori bazlı bloklar için kategori ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Kategori bazlı bloklar için kategori adı (read-only)
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// İlişkili banner ID
        /// </summary>
        public int? BannerId { get; set; }

        /// <summary>
        /// Poster görsel URL'i (Banner veya direkt)
        /// </summary>
        public string? PosterImageUrl { get; set; }

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
    /// Blok oluşturma/güncelleme isteği DTO'su
    /// </summary>
    public class CreateHomeBlockDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BlockType { get; set; } = "manual";
        public int? CategoryId { get; set; }
        public int? BannerId { get; set; }
        public string? PosterImageUrl { get; set; }
        public string? BackgroundColor { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public int MaxProductCount { get; set; } = 6;
        public string? ViewAllUrl { get; set; }
        public string ViewAllText { get; set; } = "Tümünü Gör";
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Blok güncelleme isteği DTO'su
    /// </summary>
    public class UpdateHomeBlockDto : CreateHomeBlockDto
    {
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
