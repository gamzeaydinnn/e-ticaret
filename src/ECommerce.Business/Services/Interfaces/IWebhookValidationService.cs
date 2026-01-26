// ==========================================================================
// IWebhookValidationService.cs - Webhook Doğrulama Servisi Interface
// ==========================================================================
// Ödeme sağlayıcılarından gelen webhook'ların güvenliğini sağlar.
// HMAC-SHA256 imza doğrulama, timestamp kontrolü, idempotency.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Webhook güvenlik doğrulama servisi interface'i.
    /// HMAC imza, timestamp, nonce ve idempotency kontrollerini yönetir.
    /// </summary>
    public interface IWebhookValidationService
    {
        /// <summary>
        /// Webhook imzasını doğrular (HMAC-SHA256).
        /// </summary>
        /// <param name="payload">Raw webhook body (JSON string)</param>
        /// <param name="signature">Header'dan gelen imza</param>
        /// <param name="provider">Ödeme sağlayıcı adı (iyzico, paytr, stripe vs.)</param>
        /// <returns>Doğrulama sonucu</returns>
        WebhookSignatureResult ValidateSignature(string payload, string signature, string provider);

        /// <summary>
        /// Webhook timestamp'ini kontrol eder (replay attack önleme).
        /// Varsayılan: 5 dakikadan eski webhook'lar reddedilir.
        /// </summary>
        /// <param name="timestamp">Webhook timestamp (Unix epoch saniye)</param>
        /// <param name="maxAgeSeconds">Maksimum yaş (varsayılan 300 saniye = 5 dakika)</param>
        /// <returns>Geçerli ise true</returns>
        bool ValidateTimestamp(long timestamp, int maxAgeSeconds = 300);

        /// <summary>
        /// Event ID'nin daha önce işlenip işlenmediğini kontrol eder (idempotency).
        /// </summary>
        /// <param name="provider">Ödeme sağlayıcı adı</param>
        /// <param name="eventId">Sağlayıcının event ID'si</param>
        /// <returns>Daha önce işlenmişse true</returns>
        Task<bool> IsEventAlreadyProcessedAsync(string provider, string eventId);

        /// <summary>
        /// Event'i işlenmiş olarak kaydeder.
        /// </summary>
        /// <param name="eventRecord">Webhook event kaydı</param>
        /// <returns>Kayıt ID</returns>
        Task<int> RecordWebhookEventAsync(WebhookEventRecord eventRecord);

        /// <summary>
        /// Event işleme durumunu günceller.
        /// </summary>
        /// <param name="eventId">Event kayıt ID'si</param>
        /// <param name="status">Yeni durum</param>
        /// <param name="errorMessage">Hata mesajı (varsa)</param>
        Task UpdateEventStatusAsync(int eventId, WebhookProcessingStatus status, string? errorMessage = null);

        /// <summary>
        /// Tüm webhook doğrulamalarını tek çağrıda yapar.
        /// </summary>
        /// <param name="request">Webhook doğrulama isteği</param>
        /// <returns>Kapsamlı doğrulama sonucu</returns>
        Task<WebhookValidationResult> ValidateWebhookAsync(WebhookValidationRequest request);
    }

    #region DTOs ve Enum'lar

    /// <summary>
    /// Webhook imza doğrulama sonucu.
    /// </summary>
    public class WebhookSignatureResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ComputedSignature { get; set; }

        public static WebhookSignatureResult Valid() => new() { IsValid = true };
        
        public static WebhookSignatureResult Invalid(string message) => new() 
        { 
            IsValid = false, 
            ErrorMessage = message 
        };
    }

    /// <summary>
    /// Webhook işleme durumu.
    /// </summary>
    public enum WebhookProcessingStatus
    {
        /// <summary>Alındı, işleniyor</summary>
        Received = 0,
        
        /// <summary>Başarıyla işlendi</summary>
        Processed = 1,
        
        /// <summary>İşleme başarısız</summary>
        Failed = 2,
        
        /// <summary>Duplicate (zaten işlenmiş)</summary>
        Duplicate = 3,
        
        /// <summary>İmza geçersiz</summary>
        InvalidSignature = 4,
        
        /// <summary>Timestamp geçersiz (replay attack)</summary>
        InvalidTimestamp = 5,
        
        /// <summary>Bilinmeyen event tipi</summary>
        UnknownEventType = 6
    }

    /// <summary>
    /// Webhook event kayıt bilgisi.
    /// </summary>
    public class WebhookEventRecord
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderEventId { get; set; } = string.Empty;
        public string? PaymentIntentId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? RawPayload { get; set; }
        public string? Signature { get; set; }
        public bool SignatureValid { get; set; }
        public string? SourceIpAddress { get; set; }
        public long? EventTimestamp { get; set; }
        public int? OrderId { get; set; }
        public WebhookProcessingStatus ProcessingStatus { get; set; }
    }

    /// <summary>
    /// Webhook doğrulama isteği.
    /// </summary>
    public class WebhookValidationRequest
    {
        /// <summary>Ödeme sağlayıcı adı</summary>
        public string Provider { get; set; } = string.Empty;
        
        /// <summary>Raw webhook body (JSON)</summary>
        public string RawPayload { get; set; } = string.Empty;
        
        /// <summary>İmza header değeri</summary>
        public string? Signature { get; set; }
        
        /// <summary>Sağlayıcının event ID'si</summary>
        public string? EventId { get; set; }
        
        /// <summary>Event tipi (payment.success, refund.created vs.)</summary>
        public string? EventType { get; set; }
        
        /// <summary>Webhook timestamp (Unix epoch)</summary>
        public long? Timestamp { get; set; }
        
        /// <summary>İstek IP adresi</summary>
        public string? SourceIpAddress { get; set; }
        
        /// <summary>İlişkili sipariş ID (parse edilmişse)</summary>
        public int? OrderId { get; set; }
        
        /// <summary>Payment intent/transaction ID</summary>
        public string? PaymentIntentId { get; set; }
    }

    /// <summary>
    /// Kapsamlı webhook doğrulama sonucu.
    /// </summary>
    public class WebhookValidationResult
    {
        /// <summary>Tüm doğrulamalar başarılı mı?</summary>
        public bool IsValid { get; set; }
        
        /// <summary>Hata kodu</summary>
        public string? ErrorCode { get; set; }
        
        /// <summary>Hata mesajı</summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>İmza doğrulaması başarılı mı?</summary>
        public bool SignatureValid { get; set; }
        
        /// <summary>Timestamp geçerli mi?</summary>
        public bool TimestampValid { get; set; }
        
        /// <summary>Bu event daha önce işlenmiş mi?</summary>
        public bool IsDuplicate { get; set; }
        
        /// <summary>Kaydedilen event ID (işleme için)</summary>
        public int? RecordedEventId { get; set; }

        public static WebhookValidationResult Success(int recordedEventId) => new()
        {
            IsValid = true,
            SignatureValid = true,
            TimestampValid = true,
            IsDuplicate = false,
            RecordedEventId = recordedEventId
        };

        public static WebhookValidationResult Duplicate(string eventId) => new()
        {
            IsValid = false,
            IsDuplicate = true,
            ErrorCode = "DUPLICATE_EVENT",
            ErrorMessage = $"Event '{eventId}' zaten işlenmiş."
        };

        public static WebhookValidationResult InvalidSignature(string message) => new()
        {
            IsValid = false,
            SignatureValid = false,
            ErrorCode = "INVALID_SIGNATURE",
            ErrorMessage = message
        };

        public static WebhookValidationResult InvalidTimestamp() => new()
        {
            IsValid = false,
            TimestampValid = false,
            ErrorCode = "INVALID_TIMESTAMP",
            ErrorMessage = "Webhook timestamp geçersiz veya çok eski."
        };

        public static WebhookValidationResult Failed(string errorCode, string message) => new()
        {
            IsValid = false,
            ErrorCode = errorCode,
            ErrorMessage = message
        };
    }

    #endregion
}
