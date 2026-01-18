// ProductOptionCreateDto: Yeni seçenek türü oluşturmak için kullanılan DTO.
// Validation kuralları ile güvenli veri girişi sağlanır.
// Seçenek adı benzersiz olmalıdır (business katmanında kontrol edilir).

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.ProductOption
{
    /// <summary>
    /// Yeni ürün seçenek türü oluşturmak için kullanılan DTO.
    /// Örn: "Hacim", "Renk", "Beden" gibi yeni seçenek kategorisi ekleme.
    /// </summary>
    public class ProductOptionCreateDto
    {
        /// <summary>
        /// Seçenek adı (benzersiz olmalı)
        /// Örn: "Hacim", "Renk", "Beden"
        /// </summary>
        [Required(ErrorMessage = "Seçenek adı zorunludur")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Seçenek adı 1-100 karakter arasında olmalıdır")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// 0 = en üstte, yüksek değerler altta
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Görüntüleme sırası 0-1000 arasında olmalıdır")]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Açıklama (opsiyonel)
        /// Admin panelinde yardımcı bilgi olarak gösterilebilir
        /// </summary>
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Varsayılan: aktif
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Oluşturulurken eklenecek başlangıç değerleri (opsiyonel)
        /// Örn: Hacim için: ["330ml", "500ml", "1L"]
        /// </summary>
        public List<ProductOptionValueCreateDto>? InitialValues { get; set; }
    }

    /// <summary>
    /// Mevcut seçenek türünü güncellemek için kullanılan DTO.
    /// Partial update desteklenir.
    /// </summary>
    public class ProductOptionUpdateDto
    {
        /// <summary>
        /// Seçenek adı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Seçenek adı 1-100 karakter arasında olmalıdır")]
        public string? Name { get; set; }

        /// <summary>
        /// Görüntüleme sırası
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Görüntüleme sırası 0-1000 arasında olmalıdır")]
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// Açıklama
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// DTO'nun herhangi bir güncelleme içerip içermediğini kontrol eder.
        /// </summary>
        public bool HasAnyUpdate()
        {
            return Name != null ||
                   DisplayOrder.HasValue ||
                   Description != null ||
                   IsActive.HasValue;
        }
    }

    /// <summary>
    /// Yeni seçenek değeri oluşturmak için kullanılan DTO.
    /// Örn: Hacim seçeneğine "330ml" değeri ekleme.
    /// </summary>
    public class ProductOptionValueCreateDto
    {
        /// <summary>
        /// Değer metni
        /// Örn: "330ml", "Kırmızı", "XL"
        /// </summary>
        [Required(ErrorMessage = "Değer zorunludur")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Değer 1-100 karakter arasında olmalıdır")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// 0 = en üstte
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Görüntüleme sırası 0-1000 arasında olmalıdır")]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Renk kodu (sadece renk seçenekleri için)
        /// Hex formatında: "#FF0000"
        /// </summary>
        [StringLength(20, ErrorMessage = "Renk kodu en fazla 20 karakter olabilir")]
        [RegularExpression(@"^#?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Geçersiz renk kodu formatı")]
        public string? ColorCode { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Varsayılan: aktif
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Mevcut seçenek değerini güncellemek için kullanılan DTO.
    /// </summary>
    public class ProductOptionValueUpdateDto
    {
        /// <summary>
        /// Değer metni
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Değer 1-100 karakter arasında olmalıdır")]
        public string? Value { get; set; }

        /// <summary>
        /// Görüntüleme sırası
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Görüntüleme sırası 0-1000 arasında olmalıdır")]
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// Renk kodu
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(20, ErrorMessage = "Renk kodu en fazla 20 karakter olabilir")]
        public string? ColorCode { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// DTO'nun herhangi bir güncelleme içerip içermediğini kontrol eder.
        /// </summary>
        public bool HasAnyUpdate()
        {
            return Value != null ||
                   DisplayOrder.HasValue ||
                   ColorCode != null ||
                   IsActive.HasValue;
        }
    }
}
