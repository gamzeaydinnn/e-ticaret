// ProductOptionDto: Seçenek türü bilgilerini döndürmek için kullanılan DTO.
// Hacim, Renk, Beden gibi seçenek kategorilerini temsil eder.
// Her seçenek türü birden fazla değere sahip olabilir.

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.ProductOption
{
    /// <summary>
    /// Ürün seçenek türünü temsil eden DTO.
    /// Örn: Hacim, Renk, Beden, Paket Tipi
    /// API response'larında ve admin panelinde kullanılır.
    /// </summary>
    public class ProductOptionDto
    {
        /// <summary>
        /// Seçenek benzersiz ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Seçenek adı (benzersiz)
        /// Örn: "Hacim", "Renk", "Beden"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// Düşük değer = üstte gösterilir
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Açıklama (admin için yardımcı bilgi)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Pasif seçenekler UI'da gösterilmez
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Bu seçeneğe ait değerler
        /// Örn: Hacim için: 330ml, 500ml, 1L, 2L
        /// </summary>
        public List<ProductOptionValueDto> Values { get; set; } = new();

        /// <summary>
        /// Değer sayısı
        /// </summary>
        public int ValueCount => Values.Count;
    }

    /// <summary>
    /// Seçenek değerini temsil eden DTO.
    /// Örn: Hacim seçeneği için "330ml" değeri.
    /// </summary>
    public class ProductOptionValueDto
    {
        /// <summary>
        /// Değer benzersiz ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bağlı olduğu seçenek ID'si
        /// </summary>
        public int OptionId { get; set; }

        /// <summary>
        /// Değer metni
        /// Örn: "330ml", "Kırmızı", "XL"
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// Dropdown/liste sıralaması için
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Renk kodu (sadece renk seçenekleri için)
        /// Hex formatında: "#FF0000"
        /// </summary>
        public string? ColorCode { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Seçenek listesi için basitleştirilmiş DTO.
    /// Dropdown'larda ve hızlı listelerde kullanılır.
    /// </summary>
    public class ProductOptionListDto
    {
        /// <summary>
        /// Seçenek ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Seçenek adı
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Değer sayısı
        /// </summary>
        public int ValueCount { get; set; }

        /// <summary>
        /// Aktif mi?
        /// </summary>
        public bool IsActive { get; set; }
    }
}
