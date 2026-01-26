using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Weight
{
    /// <summary>
    /// Mikro API'den (tartı cihazından) gelen webhook payload'u.
    /// Bu DTO, tartı cihazının gönderdiği ham veriyi temsil eder.
    /// 
    /// Güvenlik:
    /// - Signature doğrulama (HMAC-SHA256)
    /// - Timestamp kontrolü (5 dakika penceresi)
    /// - ExternalReportId ile idempotency
    /// </summary>
    public class MicroWeightWebhookRequestDto
    {
        /// <summary>
        /// Tartı cihazından gelen benzersiz rapor ID'si (idempotency için)
        /// </summary>
        [Required(ErrorMessage = "Rapor ID zorunludur")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Rapor ID 1-100 karakter olmalıdır")]
        public string ReportId { get; set; } = string.Empty;

        /// <summary>
        /// İlişkili sipariş ID'si
        /// </summary>
        [Required(ErrorMessage = "Sipariş ID zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçersiz sipariş ID")]
        public int OrderId { get; set; }

        /// <summary>
        /// İlişkili sipariş kalemi ID'si (opsiyonel - paket bazlı rapor için null olabilir)
        /// </summary>
        public int? OrderItemId { get; set; }

        /// <summary>
        /// Tartılan gerçek ağırlık (gram cinsinden)
        /// </summary>
        [Required(ErrorMessage = "Tartı ağırlığı zorunludur")]
        [Range(0, 1000000, ErrorMessage = "Ağırlık 0-1.000.000 gram arasında olmalıdır")]
        public int ReportedWeightGrams { get; set; }

        /// <summary>
        /// Tartı cihazı kaynak bilgisi (cihaz ID veya lokasyon)
        /// </summary>
        [StringLength(100, ErrorMessage = "Kaynak bilgisi en fazla 100 karakter olabilir")]
        public string Source { get; set; } = "MicroScale";

        /// <summary>
        /// Tartının yapıldığı zaman (ISO 8601 formatında)
        /// </summary>
        [Required(ErrorMessage = "Zaman damgası zorunludur")]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Ek metadata (JSON formatında, cihaz bilgileri vb.)
        /// </summary>
        [StringLength(5000, ErrorMessage = "Metadata en fazla 5000 karakter olabilir")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Kurye notu (opsiyonel)
        /// </summary>
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir")]
        public string? CourierNote { get; set; }
    }

    /// <summary>
    /// Webhook yanıt DTO'su
    /// </summary>
    public class MicroWeightWebhookResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Oluşturulan rapor ID'si
        /// </summary>
        public int? ReportId { get; set; }

        /// <summary>
        /// Rapor durumu
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Fazla gramaj
        /// </summary>
        public int? OverageGrams { get; set; }

        /// <summary>
        /// Fazla tutar
        /// </summary>
        public decimal? OverageAmount { get; set; }

        /// <summary>
        /// Hata detayı (varsa)
        /// </summary>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// Kurye tarafından manuel girilen tartı farkı DTO'su
    /// </summary>
    public class CourierWeightAdjustmentDto
    {
        /// <summary>
        /// Sipariş ID
        /// </summary>
        [Required(ErrorMessage = "Sipariş ID zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçersiz sipariş ID")]
        public int OrderId { get; set; }

        /// <summary>
        /// Sipariş kalemi ID (opsiyonel - tek ürün için)
        /// </summary>
        public int? OrderItemId { get; set; }

        /// <summary>
        /// Ağırlık farkı (gram cinsinden)
        /// Pozitif: Fazla geldi, Negatif: Eksik geldi
        /// </summary>
        [Required(ErrorMessage = "Ağırlık farkı zorunludur")]
        [Range(-100000, 100000, ErrorMessage = "Ağırlık farkı -100.000 ile +100.000 gram arasında olmalıdır")]
        public int WeightDifferenceGrams { get; set; }

        /// <summary>
        /// Kurye notu (opsiyonel)
        /// </summary>
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Kurye tarafından manuel girilen tartı farkı yanıt DTO'su
    /// </summary>
    public class CourierWeightAdjustmentResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Oluşturulan rapor ID'si
        /// </summary>
        public int? ReportId { get; set; }

        /// <summary>
        /// Fark tutarı
        /// </summary>
        public decimal? DifferenceAmount { get; set; }

        /// <summary>
        /// Yeni sipariş toplam tutarı
        /// </summary>
        public decimal? NewTotalAmount { get; set; }
    }
}
