// XmlImportRequestDto: XML import işlemi başlatmak için kullanılan DTO.
// URL'den veya dosyadan import desteklenir.
// Mapping konfigürasyonu ve import seçenekleri içerir.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// XML import işlemi başlatmak için kullanılan DTO.
    /// URL veya dosya kaynağından import yapılabilir.
    /// </summary>
    public class XmlImportRequestDto
    {
        #region Kaynak Seçenekleri

        /// <summary>
        /// Kaynak tipi: "url" veya "file"
        /// </summary>
        [Required(ErrorMessage = "Kaynak tipi zorunludur")]
        [RegularExpression(@"^(url|file)$", ErrorMessage = "Kaynak tipi 'url' veya 'file' olmalıdır")]
        public string SourceType { get; set; } = "url";

        /// <summary>
        /// XML feed URL'si (SourceType = "url" ise zorunlu)
        /// </summary>
        [Url(ErrorMessage = "Geçerli bir URL giriniz")]
        public string? Url { get; set; }

        /// <summary>
        /// Tanımlı feed kaynağı ID'si (opsiyonel)
        /// Belirtilirse o kaynağın URL ve mapping ayarları kullanılır
        /// </summary>
        public int? FeedSourceId { get; set; }

        #endregion

        #region Mapping Konfigürasyonu

        /// <summary>
        /// XML alan eşleştirme konfigürasyonu
        /// FeedSourceId belirtilmişse bu alan göz ardı edilir
        /// </summary>
        public XmlMappingConfigDto? MappingConfig { get; set; }

        #endregion

        #region Import Seçenekleri

        /// <summary>
        /// Sadece önizleme mi yapılsın?
        /// true: Değişiklik yapılmaz, sadece analiz sonucu döner
        /// false: Gerçek import yapılır
        /// </summary>
        public bool PreviewOnly { get; set; } = false;

        /// <summary>
        /// Mevcut ürünler güncellensin mi?
        /// true: SKU eşleşirse güncelle
        /// false: Sadece yeni ürünleri ekle
        /// </summary>
        public bool UpdateExisting { get; set; } = true;

        /// <summary>
        /// Stok sıfır olanları pasifleştir mi?
        /// true: Stock=0 olan varyantları IsActive=false yap
        /// </summary>
        public bool DeactivateZeroStock { get; set; } = false;

        /// <summary>
        /// Feed'de görünmeyen ürünleri pasifleştir mi?
        /// true: Bu import'ta görünmeyen SKU'ları pasifleştir
        /// DİKKAT: Büyük veri kaybına yol açabilir
        /// </summary>
        public bool DeactivateMissing { get; set; } = false;

        /// <summary>
        /// Varsayılan kategori ID'si
        /// XML'de kategori bilgisi yoksa kullanılır
        /// </summary>
        public int? DefaultCategoryId { get; set; }

        /// <summary>
        /// Varsayılan marka ID'si
        /// XML'de marka bilgisi yoksa kullanılır
        /// </summary>
        public int? DefaultBrandId { get; set; }

        #endregion

        #region Filtre Seçenekleri

        /// <summary>
        /// Sadece belirli SKU'ları import et
        /// Boş ise tümünü import et
        /// </summary>
        public List<string>? SkuFilter { get; set; }

        /// <summary>
        /// Minimum stok filtresi
        /// Bu değerin altındaki ürünler atlanır
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum stok negatif olamaz")]
        public int? MinStock { get; set; }

        /// <summary>
        /// Minimum fiyat filtresi
        /// Bu değerin altındaki ürünler atlanır
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Minimum fiyat negatif olamaz")]
        public decimal? MinPrice { get; set; }

        #endregion

        #region Validation

        /// <summary>
        /// DTO validasyonunu yapar.
        /// URL veya FeedSourceId'den biri zorunludur.
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (SourceType == "url" && string.IsNullOrEmpty(Url) && !FeedSourceId.HasValue)
            {
                errors.Add("URL veya Feed Source ID belirtilmelidir");
            }

            if (SourceType == "url" && !string.IsNullOrEmpty(Url) && !Uri.TryCreate(Url, UriKind.Absolute, out _))
            {
                errors.Add("Geçersiz URL formatı");
            }

            if (MappingConfig == null && !FeedSourceId.HasValue && SourceType == "url")
            {
                errors.Add("Mapping konfigürasyonu veya Feed Source ID gereklidir");
            }

            return errors.Count == 0;
        }

        #endregion
    }

    /// <summary>
    /// XML dosyası ile import için kullanılan DTO.
    /// Multipart form-data ile dosya gönderimi için.
    /// </summary>
    public class XmlImportFileRequestDto
    {
        /// <summary>
        /// Mapping konfigürasyonu (JSON string olarak)
        /// </summary>
        public string? MappingConfigJson { get; set; }

        /// <summary>
        /// Sadece önizleme mi?
        /// </summary>
        public bool PreviewOnly { get; set; } = false;

        /// <summary>
        /// Mevcut ürünleri güncelle
        /// </summary>
        public bool UpdateExisting { get; set; } = true;

        /// <summary>
        /// Stok 0 olanları pasifleştir
        /// </summary>
        public bool DeactivateZeroStock { get; set; } = false;

        /// <summary>
        /// Varsayılan kategori ID
        /// </summary>
        public int? DefaultCategoryId { get; set; }

        /// <summary>
        /// Varsayılan marka ID
        /// </summary>
        public int? DefaultBrandId { get; set; }
    }
}
