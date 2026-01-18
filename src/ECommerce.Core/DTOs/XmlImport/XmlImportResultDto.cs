// XmlImportResultDto: XML import işlemi sonucunu döndüren DTO.
// İşlem istatistikleri, başarılı/başarısız kayıtlar ve hatalar içerir.
// Detaylı loglama ve kullanıcı bilgilendirme için tasarlanmıştır.

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// XML import işlemi sonucunu içeren DTO.
    /// İşlem tamamlandığında veya önizleme sonrasında döner.
    /// </summary>
    public class XmlImportResultDto
    {
        #region İşlem Durumu

        /// <summary>
        /// İşlem başarılı mı?
        /// Tüm kayıtlar işlendi (hatalı olanlar dahil)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem mesajı
        /// Başarı veya hata durumunda açıklayıcı mesaj
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Bu bir önizleme mi, yoksa gerçek import mi?
        /// </summary>
        public bool IsPreview { get; set; }

        #endregion

        #region İstatistikler

        /// <summary>
        /// XML'de bulunan toplam kayıt sayısı
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// İşlenen kayıt sayısı (filtre sonrası)
        /// </summary>
        public int ProcessedRecords { get; set; }

        /// <summary>
        /// Yeni eklenen varyant sayısı
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// Güncellenen varyant sayısı
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Değişiklik olmayan (aynı kalan) varyant sayısı
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Atlanan kayıt sayısı (filtre nedeniyle)
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Hatalı kayıt sayısı
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Pasifleştirilen varyant sayısı
        /// (stok 0 veya feed'de görünmeme nedeniyle)
        /// </summary>
        public int DeactivatedCount { get; set; }

        /// <summary>
        /// Yeni oluşturulan ürün sayısı
        /// (varyant değil, ana ürün)
        /// </summary>
        public int NewProductsCreated { get; set; }

        #endregion

        #region Zaman Bilgileri

        /// <summary>
        /// İşlem başlangıç zamanı
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// İşlem bitiş zamanı
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Toplam işlem süresi (saniye)
        /// </summary>
        public double? DurationSeconds => CompletedAt.HasValue
            ? (CompletedAt.Value - StartedAt).TotalSeconds
            : null;

        /// <summary>
        /// Formatlanmış işlem süresi
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                if (!DurationSeconds.HasValue) return "Devam ediyor...";
                var ts = TimeSpan.FromSeconds(DurationSeconds.Value);
                if (ts.TotalMinutes >= 1) return $"{ts.Minutes}dk {ts.Seconds}sn";
                return $"{ts.TotalSeconds:N1}sn";
            }
        }

        #endregion

        #region Hata Detayları

        /// <summary>
        /// Hatalı kayıtların detayları
        /// Her hata için SKU ve hata mesajı
        /// </summary>
        public List<XmlImportErrorDto> Errors { get; set; } = new();

        /// <summary>
        /// Uyarılar (işlem durdurulmayan sorunlar)
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        #endregion

        #region Önizleme Detayları

        /// <summary>
        /// Önizleme modunda: Eklenecek varyantların listesi
        /// İlk 50 kayıt gösterilir
        /// </summary>
        public List<XmlImportPreviewItemDto>? PreviewItems { get; set; }

        /// <summary>
        /// Önizlemede gösterilmeyen kayıt sayısı
        /// </summary>
        public int? PreviewItemsOmitted { get; set; }

        #endregion

        #region Özet

        /// <summary>
        /// İşlem özeti (kullanıcıya gösterilecek)
        /// </summary>
        public string Summary
        {
            get
            {
                if (IsPreview)
                {
                    return $"Önizleme: {TotalRecords} kayıt bulundu. " +
                           $"{CreatedCount} yeni eklenecek, {UpdatedCount} güncellenecek, " +
                           $"{SkippedCount} atlanacak, {FailedCount} hatalı.";
                }

                return $"Import tamamlandı: {CreatedCount} yeni eklendi, " +
                       $"{UpdatedCount} güncellendi, {UnchangedCount} değişmedi, " +
                       $"{FailedCount} hatalı. Süre: {FormattedDuration}";
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Başarılı sonuç oluşturur.
        /// </summary>
        public static XmlImportResultDto CreateSuccess(string message, bool isPreview = false)
        {
            return new XmlImportResultDto
            {
                Success = true,
                Message = message,
                IsPreview = isPreview,
                StartedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Hatalı sonuç oluşturur.
        /// </summary>
        public static XmlImportResultDto CreateError(string message)
        {
            return new XmlImportResultDto
            {
                Success = false,
                Message = message,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }

        #endregion
    }

    /// <summary>
    /// Import sırasında oluşan bir hatayı temsil eder.
    /// </summary>
    public class XmlImportErrorDto
    {
        /// <summary>
        /// Hatalı kaydın satır numarası (1-based)
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Hatalı kaydın SKU'su (varsa)
        /// </summary>
        public string? SKU { get; set; }

        /// <summary>
        /// Hata mesajı
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Hata tipi
        /// "Validation", "Mapping", "Database", "Unknown"
        /// </summary>
        public string ErrorType { get; set; } = "Unknown";

        /// <summary>
        /// Hatalı alan adı (varsa)
        /// </summary>
        public string? FieldName { get; set; }
    }

    /// <summary>
    /// Önizleme modunda gösterilecek kayıt.
    /// </summary>
    public class XmlImportPreviewItemDto
    {
        /// <summary>
        /// Satır numarası
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// SKU
        /// </summary>
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Ürün/Varyant adı
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Fiyat
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stok
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// İşlem türü: "Create", "Update", "Unchanged", "Skip"
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// İşlem renk kodu (UI için)
        /// </summary>
        public string ActionColor => Action switch
        {
            "Create" => "success",
            "Update" => "warning",
            "Skip" => "secondary",
            _ => "default"
        };

        /// <summary>
        /// Değişecek alanlar (Update için)
        /// </summary>
        public List<string>? ChangedFields { get; set; }
    }
}
