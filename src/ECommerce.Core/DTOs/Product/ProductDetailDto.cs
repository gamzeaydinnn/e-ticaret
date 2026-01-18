// ProductDetailDto: Ürün detay bilgilerini döndüren DTO.
// Kategori, varyant, stok, resim listesi bilgilerini içerir.
// API response'larında ve ürün detay sayfasında kullanılır.

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Ürün detay bilgilerini içeren DTO.
    /// API response ve ürün detay sayfası için kullanılır.
    /// </summary>
    public class ProductDetailDto
    {
        #region Temel Bilgiler

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        
        public string? Brand { get; set; }
        public int? BrandId { get; set; }
        public string? ImageUrl { get; set; }

        #endregion

        #region Kategori Bilgisi

        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        #endregion

        #region Varyant Bilgileri

        /// <summary>
        /// Ürünün varyantları
        /// Market ürünleri için: 330ml, 500ml, 1L gibi hacim varyantları
        /// </summary>
        public List<ProductVariantDto> Variants { get; set; } = new();

        /// <summary>
        /// Varyant sayısı
        /// </summary>
        public int VariantCount => Variants.Count;

        /// <summary>
        /// Ürünün varyantları var mı?
        /// </summary>
        public bool HasVariants => Variants.Count > 0;

        /// <summary>
        /// Toplam stok (tüm varyantların toplamı)
        /// Varyant yoksa ana ürün stoğu döner
        /// </summary>
        public int TotalStock => HasVariants 
            ? Variants.FindAll(v => v.IsActive).ConvertAll(v => v.Stock).DefaultIfEmpty(0).Sum() 
            : StockQuantity;

        /// <summary>
        /// En düşük varyant fiyatı
        /// "XX TL'den başlayan fiyatlarla" için
        /// </summary>
        public decimal? MinVariantPrice => HasVariants 
            ? Variants.FindAll(v => v.IsActive && v.Stock > 0).ConvertAll(v => v.Price).DefaultIfEmpty(Price).Min()
            : null;

        #endregion

        #region Seçenek Bilgileri (Varyant Seçimi İçin)

        /// <summary>
        /// Mevcut seçenek türleri ve değerleri
        /// UI'da varyant seçim dropdown'ları için
        /// Örn: { "Hacim": ["330ml", "500ml", "1L"], "Paket": ["Tekli", "6'lı"] }
        /// </summary>
        public Dictionary<string, List<ProductOptionSelectionDto>>? AvailableOptions { get; set; }

        #endregion

        #region Görsel Bilgileri

        /// <summary>
        /// Ek ürün görselleri URL'leri
        /// </summary>
        public List<string> ImageUrls { get; set; } = new();

        #endregion
    }

    /// <summary>
    /// Varyant bilgisi için DTO.
    /// Liste görünümü ve detay sayfasında kullanılır.
    /// </summary>
    public class ProductVariantDto
    {
        #region Kimlik

        public int Id { get; set; }

        /// <summary>
        /// SKU - Stok Tutma Birimi (benzersiz)
        /// </summary>
        public string SKU { get; set; } = string.Empty;

        #endregion

        #region Temel Bilgiler

        /// <summary>
        /// Varyant başlığı
        /// Örn: "Coca Cola 330ml Kutu"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Alternatif başlık alanı (Title)
        /// Bazı yerlerde Name yerine Title kullanılır
        /// </summary>
        public string Title 
        { 
            get => Name; 
            set => Name = value; 
        }

        /// <summary>
        /// Satış fiyatı
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stok adedi
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Alternatif stok alanı
        /// </summary>
        public int Stock 
        { 
            get => StockQuantity; 
            set => StockQuantity = value; 
        }

        /// <summary>
        /// Para birimi
        /// </summary>
        public string Currency { get; set; } = "TRY";

        #endregion

        #region Fiziksel Özellikler

        /// <summary>
        /// Barkod
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// Ağırlık (gram)
        /// </summary>
        public int? WeightGrams { get; set; }

        /// <summary>
        /// Hacim (ml)
        /// </summary>
        public int? VolumeML { get; set; }

        /// <summary>
        /// Formatlanmış hacim (330ml, 1L)
        /// </summary>
        public string? FormattedVolume => VolumeML switch
        {
            null => null,
            >= 1000 => $"{VolumeML.Value / 1000.0:N1}L",
            _ => $"{VolumeML}ml"
        };

        #endregion

        #region Seçenek Değerleri

        /// <summary>
        /// Bu varyanta atanmış seçenek değerleri
        /// Örn: [{ OptionName: "Hacim", Value: "330ml" }]
        /// </summary>
        public List<VariantOptionValueDto>? OptionValues { get; set; }

        /// <summary>
        /// Seçenek özeti
        /// Örn: "Hacim: 330ml, Paket: Tekli"
        /// </summary>
        public string OptionsSummary => OptionValues?.Count > 0
            ? string.Join(", ", OptionValues.ConvertAll(o => $"{o.OptionName}: {o.Value}"))
            : string.Empty;

        #endregion

        #region Durum

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Stokta var mı?
        /// </summary>
        public bool InStock => StockQuantity > 0 && IsActive;

        /// <summary>
        /// Stok durumu metni
        /// </summary>
        public string StockStatus => StockQuantity switch
        {
            0 => "Tükendi",
            <= 5 => "Son Stoklar",
            _ => "Stokta"
        };

        #endregion
    }

    /// <summary>
    /// Varyant seçenek değerini temsil eder.
    /// Varyant detayında ve seçim listelerinde kullanılır.
    /// </summary>
    public class VariantOptionValueDto
    {
        public int OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public int OptionValueId { get; set; }
        public string Value { get; set; } = string.Empty;
        public string? ColorCode { get; set; }
    }

    /// <summary>
    /// Ürün detayında seçenek seçimi için kullanılan DTO.
    /// Dropdown/buton grubu oluşturmak için.
    /// </summary>
    public class ProductOptionSelectionDto
    {
        /// <summary>
        /// Seçenek değer ID'si
        /// </summary>
        public int OptionValueId { get; set; }

        /// <summary>
        /// Değer metni
        /// Örn: "330ml", "Kırmızı"
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Bu değere sahip varyant sayısı
        /// </summary>
        public int VariantCount { get; set; }

        /// <summary>
        /// Bu değer stokta mı?
        /// En az bir varyant stokta ise true
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Renk kodu (renk seçenekleri için)
        /// </summary>
        public string? ColorCode { get; set; }

        /// <summary>
        /// En düşük fiyat (bu değere sahip varyantlar arasında)
        /// </summary>
        public decimal? MinPrice { get; set; }
    }
}

