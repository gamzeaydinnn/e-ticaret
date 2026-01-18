// XmlProductItemDto: XML'den parse edilmiş bir ürün/varyant satırını temsil eder.
// XML parser tarafından oluşturulur, business katmanında entity'ye dönüştürülür.
// Tüm alanlar nullable çünkü XML'de eksik olabilir.

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// XML'den parse edilmiş tek bir ürün/varyant kaydını temsil eder.
    /// XML parser'dan business katmanına veri aktarımı için kullanılır.
    /// </summary>
    public class XmlProductItemDto
    {
        #region Zorunlu Alanlar

        /// <summary>
        /// Stok Tutma Birimi - Benzersiz tanımlayıcı
        /// Bu alan zorunludur, null ise kayıt atlanır
        /// </summary>
        public string? SKU { get; set; }

        /// <summary>
        /// Ürün/Varyant başlığı
        /// Null ise SKU kullanılır
        /// </summary>
        public string? Title { get; set; }

        #endregion

        #region Fiyat ve Stok

        /// <summary>
        /// Satış fiyatı
        /// Null veya 0 ise varsayılan değer kullanılır
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Para birimi kodu
        /// Null ise "TRY" varsayılır
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Stok adedi
        /// Null ise 0 varsayılır
        /// </summary>
        public int? Stock { get; set; }

        #endregion

        #region Ürün Tanımlama

        /// <summary>
        /// Barkod numarası (EAN, UPC vb.)
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// Ana ürün grubu SKU'su
        /// Aynı ürünün farklı varyantlarını gruplamak için
        /// </summary>
        public string? ParentSku { get; set; }

        /// <summary>
        /// Tedarikçi ürün kodu
        /// </summary>
        public string? SupplierCode { get; set; }

        #endregion

        #region Fiziksel Özellikler

        /// <summary>
        /// Ağırlık (gram)
        /// </summary>
        public int? WeightGrams { get; set; }

        /// <summary>
        /// Hacim (mililitre)
        /// </summary>
        public int? VolumeML { get; set; }

        #endregion

        #region Kategori ve Marka

        /// <summary>
        /// Kategori yolu
        /// Örn: "İçecekler > Gazlı İçecekler > Kola"
        /// </summary>
        public string? CategoryPath { get; set; }

        /// <summary>
        /// Kategori ID (doğrudan mapping varsa)
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Marka adı
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// Marka ID (doğrudan mapping varsa)
        /// </summary>
        public int? BrandId { get; set; }

        #endregion

        #region Görsel

        /// <summary>
        /// Ana görsel URL'si
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Ek görsel URL'leri
        /// </summary>
        public List<string>? AdditionalImageUrls { get; set; }

        #endregion

        #region Açıklama

        /// <summary>
        /// Ürün açıklaması
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Kısa açıklama
        /// </summary>
        public string? ShortDescription { get; set; }

        #endregion

        #region Option/Seçenek Değerleri

        /// <summary>
        /// Seçenek değerleri
        /// Key: Seçenek adı (Hacim, Renk vb.)
        /// Value: Seçenek değeri (330ml, Kırmızı vb.)
        /// </summary>
        public Dictionary<string, string>? Options { get; set; }

        #endregion

        #region Meta Bilgiler

        /// <summary>
        /// XML'deki satır numarası (hata raporlama için)
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Ham XML element'i (debug için)
        /// </summary>
        public string? RawXml { get; set; }

        /// <summary>
        /// Ek alanlar (custom mapping için)
        /// XML'den okunan ama standart alanlara map edilmeyen değerler
        /// </summary>
        public Dictionary<string, string>? ExtraFields { get; set; }

        #endregion

        #region Validation

        /// <summary>
        /// Temel validasyon kontrolleri yapar.
        /// SKU zorunludur, diğer alanlar opsiyonel.
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            // SKU zorunlu
            if (string.IsNullOrWhiteSpace(SKU))
            {
                errors.Add("SKU boş olamaz");
            }
            else if (SKU.Length > 50)
            {
                errors.Add("SKU 50 karakterden uzun olamaz");
            }

            // Fiyat kontrolü
            if (Price.HasValue && Price < 0)
            {
                errors.Add("Fiyat negatif olamaz");
            }

            // Stok kontrolü
            if (Stock.HasValue && Stock < 0)
            {
                errors.Add("Stok negatif olamaz");
            }

            // Title uzunluk kontrolü
            if (!string.IsNullOrEmpty(Title) && Title.Length > 200)
            {
                errors.Add("Başlık 200 karakterden uzun olamaz");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Kayıt geçerli mi? (hızlı kontrol)
        /// </summary>
        public bool HasValidSku => !string.IsNullOrWhiteSpace(SKU) && SKU.Length <= 50;

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Effective title döndürür (Title veya SKU)
        /// </summary>
        public string GetEffectiveTitle() => !string.IsNullOrEmpty(Title) ? Title : (SKU ?? "Unknown");

        /// <summary>
        /// Effective price döndürür (varsayılan 0)
        /// </summary>
        public decimal GetEffectivePrice() => Price ?? 0;

        /// <summary>
        /// Effective stock döndürür (varsayılan 0)
        /// </summary>
        public int GetEffectiveStock() => Stock ?? 0;

        /// <summary>
        /// Effective currency döndürür (varsayılan TRY)
        /// </summary>
        public string GetEffectiveCurrency() => !string.IsNullOrEmpty(Currency) ? Currency : "TRY";

        /// <summary>
        /// Debug için string representation
        /// </summary>
        public override string ToString()
        {
            return $"[Row {RowNumber}] SKU: {SKU}, Title: {GetEffectiveTitle()}, " +
                   $"Price: {GetEffectivePrice():N2} {GetEffectiveCurrency()}, Stock: {GetEffectiveStock()}";
        }

        #endregion
    }
}
