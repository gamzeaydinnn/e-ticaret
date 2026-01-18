// ProductVariantDetailDto: Varyant detay bilgilerini döndürmek için kullanılan DTO.
// Option değerleri, senkronizasyon bilgileri ve stok durumu dahil zengin veri.
// API response'larında kullanılır, hassas bilgiler filtrelenir.

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Ürün varyantının detaylı bilgilerini içeren DTO.
    /// API response'larında, admin panelinde ve ürün detay sayfasında kullanılır.
    /// </summary>
    public class ProductVariantDetailDto
    {
        #region Kimlik Bilgileri

        /// <summary>
        /// Varyant benzersiz ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bağlı olduğu ürün ID'si
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Bağlı olduğu ürün adı
        /// UI'da referans için
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        #endregion

        #region Temel Bilgiler

        /// <summary>
        /// Varyant başlığı
        /// Örn: "Coca Cola 330ml Kutu"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Stok Tutma Birimi - Benzersiz ürün kodu
        /// </summary>
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Satış fiyatı
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Para birimi kodu
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Formatlanmış fiyat string'i
        /// Örn: "₺15,99" veya "$9.99"
        /// </summary>
        public string FormattedPrice => Currency switch
        {
            "TRY" => $"₺{Price:N2}",
            "USD" => $"${Price:N2}",
            "EUR" => $"€{Price:N2}",
            _ => $"{Price:N2} {Currency}"
        };

        #endregion

        #region Stok Bilgileri

        /// <summary>
        /// Mevcut stok adedi
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Stok durumu açıklaması
        /// </summary>
        public string StockStatus => Stock switch
        {
            0 => "Tükendi",
            <= 5 => "Son Stoklar",
            <= 20 => "Stokta Az",
            _ => "Stokta"
        };

        /// <summary>
        /// Stokta var mı?
        /// </summary>
        public bool InStock => Stock > 0;

        #endregion

        #region Fiziksel Özellikler

        /// <summary>
        /// Barkod numarası
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
        /// Formatlanmış ağırlık
        /// Örn: "330g" veya "1.5kg"
        /// </summary>
        public string? FormattedWeight => WeightGrams switch
        {
            null => null,
            >= 1000 => $"{WeightGrams.Value / 1000.0:N1}kg",
            _ => $"{WeightGrams}g"
        };

        /// <summary>
        /// Formatlanmış hacim
        /// Örn: "330ml" veya "1.5L"
        /// </summary>
        public string? FormattedVolume => VolumeML switch
        {
            null => null,
            >= 1000 => $"{VolumeML.Value / 1000.0:N1}L",
            _ => $"{VolumeML}ml"
        };

        #endregion

        #region Tedarikçi Bilgileri

        /// <summary>
        /// Tedarikçi ürün kodu
        /// </summary>
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Ana ürün grubu SKU'su
        /// </summary>
        public string? ParentSku { get; set; }

        #endregion

        #region Option/Seçenek Bilgileri

        /// <summary>
        /// Varyanta atanmış seçenek değerleri
        /// Örn: [{ OptionName: "Hacim", Value: "330ml" }, { OptionName: "Paket", Value: "Tekli" }]
        /// </summary>
        public List<VariantOptionValueDto> OptionValues { get; set; } = new();

        /// <summary>
        /// Seçenek özetini döndürür
        /// Örn: "Hacim: 330ml, Paket: Tekli"
        /// </summary>
        public string OptionsSummary => OptionValues.Count > 0
            ? string.Join(", ", OptionValues.ConvertAll(o => $"{o.OptionName}: {o.Value}"))
            : string.Empty;

        #endregion

        #region Durum Bilgileri

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #region Senkronizasyon Bilgileri (Admin için)

        /// <summary>
        /// Son XML senkronizasyon tarihi
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// XML feed'de son görülme tarihi
        /// </summary>
        public DateTime? LastSeenAt { get; set; }

        /// <summary>
        /// Senkronizasyon durumu
        /// </summary>
        public string? SyncStatus => LastSeenAt switch
        {
            null => "Hiç senkronize edilmedi",
            _ when LastSeenAt > DateTime.UtcNow.AddDays(-1) => "Güncel",
            _ when LastSeenAt > DateTime.UtcNow.AddDays(-7) => "1 haftadan eski",
            _ => "Güncelliğini yitirmiş"
        };

        #endregion
    }

    // NOT: VariantOptionValueDto ProductDetailDto.cs dosyasında tanımlıdır.
    // Çift tanımlamayı önlemek için burada tanımlanmamıştır.
}
