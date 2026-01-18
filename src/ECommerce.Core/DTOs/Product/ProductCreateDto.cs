// ProductCreateDto: Yeni ürün oluşturmak için kullanılan DTO.
// Varyantlar ile birlikte ürün oluşturma desteği eklenmiştir.
// Validation kuralları ile güvenli veri girişi sağlanır.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Yeni ürün oluşturmak için kullanılan DTO.
    /// Varyantlar ile birlikte toplu oluşturma desteklenir.
    /// </summary>
    public class ProductCreateDto
    {
        #region Temel Bilgiler

        /// <summary>
        /// Ürün adı
        /// </summary>
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Ürün adı 2-200 karakter arasında olmalıdır")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ürün açıklaması
        /// </summary>
        [StringLength(4000, ErrorMessage = "Açıklama en fazla 4000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// SKU - Opsiyonel, boşsa otomatik oluşturulur
        /// Ana ürün SKU'su (varyant değil)
        /// </summary>
        [StringLength(50, ErrorMessage = "SKU en fazla 50 karakter olabilir")]
        public string? SKU { get; set; }

        #endregion

        #region Fiyat ve Stok

        /// <summary>
        /// Ana ürün fiyatı
        /// Varyant yoksa bu fiyat kullanılır
        /// </summary>
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 9999999.99, ErrorMessage = "Fiyat 0.01 ile 9,999,999.99 arasında olmalıdır")]
        public decimal Price { get; set; }

        /// <summary>
        /// İndirimli fiyat (opsiyonel)
        /// </summary>
        [Range(0.01, 9999999.99, ErrorMessage = "İndirimli fiyat geçerli aralıkta olmalıdır")]
        public decimal? SpecialPrice { get; set; }

        /// <summary>
        /// Ana ürün stok adedi
        /// Varyant yoksa bu stok kullanılır
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Stok negatif olamaz")]
        public int StockQuantity { get; set; }

        /// <summary>
        /// Stock property - StockQuantity ile senkronize
        /// Geriye dönük uyumluluk için
        /// </summary>
        public int Stock 
        { 
            get => StockQuantity; 
            set => StockQuantity = value; 
        }

        #endregion

        #region Kategori ve Marka

        /// <summary>
        /// Kategori ID (zorunlu)
        /// </summary>
        [Required(ErrorMessage = "Kategori zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kategori seçiniz")]
        public int CategoryId { get; set; }

        /// <summary>
        /// Marka ID (opsiyonel)
        /// </summary>
        public int? BrandId { get; set; }

        #endregion

        #region Görsel

        /// <summary>
        /// Ana görsel URL'si
        /// </summary>
        [StringLength(1000, ErrorMessage = "Görsel URL en fazla 1000 karakter olabilir")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Ek görsel URL'leri
        /// </summary>
        public List<string>? AdditionalImageUrls { get; set; }

        #endregion

        #region Varyantlar

        /// <summary>
        /// Ürün ile birlikte oluşturulacak varyantlar
        /// Null veya boş ise varyantsız ürün oluşturulur
        /// </summary>
        public List<ProductVariantCreateDto>? Variants { get; set; }

        /// <summary>
        /// Varyant var mı?
        /// </summary>
        public bool HasVariants => Variants != null && Variants.Count > 0;

        #endregion

        #region Durum

        /// <summary>
        /// Ürün aktif mi?
        /// Varsayılan: aktif
        /// </summary>
        public bool IsActive { get; set; } = true;

        #endregion
    }
}
