// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET AUDİT LOG SERVİSİ
// Ödeme işlemlerinin detaylı audit trail kaydını tutar
// PCI-DSS Requirement 10: Track and monitor all access to network resources and cardholder data
// 
// ÖZELLİKLER:
// - Tüm ödeme işlemlerinin kaydı
// - Hassas veri maskeleme
// - Performans metrikleri
// - Hata izleme ve analiz
// - Compliance raporlama desteği
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Services.Payment.Posnet.Security;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET audit log servisi interface
    /// </summary>
    public interface IPosnetAuditLogService
    {
        /// <summary>
        /// Ödeme işlemi başlangıç logu
        /// </summary>
        void LogPaymentInitiated(PaymentAuditEvent auditEvent);

        /// <summary>
        /// Ödeme işlemi tamamlandı logu
        /// </summary>
        void LogPaymentCompleted(PaymentAuditEvent auditEvent);

        /// <summary>
        /// Ödeme işlemi başarısız logu
        /// </summary>
        void LogPaymentFailed(PaymentAuditEvent auditEvent);

        /// <summary>
        /// 3D Secure callback logu
        /// </summary>
        void Log3DSecureCallback(PaymentAuditEvent auditEvent);

        /// <summary>
        /// İptal/İade işlemi logu
        /// </summary>
        void LogRefundOrCancel(PaymentAuditEvent auditEvent);

        /// <summary>
        /// Güvenlik olayı logu (şüpheli işlem, rate limit vb.)
        /// </summary>
        void LogSecurityEvent(SecurityAuditEvent securityEvent);

        /// <summary>
        /// API çağrısı performans logu
        /// </summary>
        void LogApiPerformance(ApiPerformanceEvent performanceEvent);
    }

    /// <summary>
    /// Ödeme audit olayı
    /// </summary>
    public class PaymentAuditEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public PaymentAuditEventType EventType { get; set; }
        
        // İşlem bilgileri
        public int? OrderId { get; set; }
        public string? TransactionId { get; set; }
        public decimal? Amount { get; set; }
        public string? Currency { get; set; } = "TRY";
        public int? InstallmentCount { get; set; }
        
        // Kart bilgileri (maskelenmiş)
        public string? MaskedCardNumber { get; set; }
        public string? CardBrand { get; set; }
        
        // Kullanıcı/Müşteri bilgileri
        public int? CustomerId { get; set; }
        public string? CustomerEmail { get; set; } // Maskelenmiş
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        // POSNET spesifik
        public string? HostLogKey { get; set; }
        public string? AuthCode { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? MdStatus { get; set; } // 3D Secure
        
        // Hata bilgileri
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Performans
        public long? DurationMs { get; set; }
        
        // Ek bilgiler
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Ödeme audit olay türleri
    /// </summary>
    public enum PaymentAuditEventType
    {
        PaymentInitiated,
        PaymentPending3DSecure,
        Payment3DSecureCallback,
        PaymentCompleted,
        PaymentFailed,
        PaymentCancelled,
        RefundInitiated,
        RefundCompleted,
        RefundFailed,
        PointQueryInitiated,
        PointQueryCompleted
    }

    /// <summary>
    /// Güvenlik audit olayı
    /// </summary>
    public class SecurityAuditEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public SecurityEventType EventType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Description { get; set; }
        public SecuritySeverity Severity { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }

    /// <summary>
    /// Güvenlik olay türleri
    /// </summary>
    public enum SecurityEventType
    {
        RateLimitExceeded,
        SuspiciousActivity,
        InvalidMacSignature,
        FraudDetected,
        UnauthorizedAccess,
        InvalidCardData,
        RepeatedFailures,
        IpBlocked,
        ConfigurationError
    }

    /// <summary>
    /// Güvenlik olay ciddiyet seviyeleri
    /// </summary>
    public enum SecuritySeverity
    {
        Info,
        Warning,
        High,
        Critical
    }

    /// <summary>
    /// API performans olayı
    /// </summary>
    public class ApiPerformanceEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public int? HttpStatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public long? RequestSizeBytes { get; set; }
        public long? ResponseSizeBytes { get; set; }
    }

    /// <summary>
    /// POSNET audit log servisi implementasyonu
    /// </summary>
    public class PosnetAuditLogService : IPosnetAuditLogService
    {
        private readonly ILogger<PosnetAuditLogService> _logger;
        private readonly IPosnetSecurityService _securityService;

        // JSON serialization ayarları
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PosnetAuditLogService(
            ILogger<PosnetAuditLogService> logger,
            IPosnetSecurityService securityService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        }

        /// <inheritdoc/>
        public void LogPaymentInitiated(PaymentAuditEvent auditEvent)
        {
            auditEvent.EventType = PaymentAuditEventType.PaymentInitiated;
            MaskSensitiveData(auditEvent);

            _logger.LogInformation(
                "[POSNET-AUDIT] {EventType} | CorrelationId: {CorrelationId} | OrderId: {OrderId} | " +
                "Amount: {Amount} {Currency} | Card: {MaskedCard} | Customer: {CustomerId} | IP: {IpAddress}",
                auditEvent.EventType,
                auditEvent.CorrelationId,
                auditEvent.OrderId,
                auditEvent.Amount,
                auditEvent.Currency,
                auditEvent.MaskedCardNumber,
                auditEvent.CustomerId,
                auditEvent.IpAddress);

            LogStructuredEvent(auditEvent);
        }

        /// <inheritdoc/>
        public void LogPaymentCompleted(PaymentAuditEvent auditEvent)
        {
            auditEvent.EventType = PaymentAuditEventType.PaymentCompleted;
            auditEvent.IsSuccess = true;
            MaskSensitiveData(auditEvent);

            _logger.LogInformation(
                "[POSNET-AUDIT] ✅ {EventType} | CorrelationId: {CorrelationId} | OrderId: {OrderId} | " +
                "TransactionId: {TransactionId} | Amount: {Amount} {Currency} | AuthCode: {AuthCode} | " +
                "Duration: {DurationMs}ms",
                auditEvent.EventType,
                auditEvent.CorrelationId,
                auditEvent.OrderId,
                auditEvent.TransactionId,
                auditEvent.Amount,
                auditEvent.Currency,
                auditEvent.AuthCode,
                auditEvent.DurationMs);

            LogStructuredEvent(auditEvent);
        }

        /// <inheritdoc/>
        public void LogPaymentFailed(PaymentAuditEvent auditEvent)
        {
            auditEvent.EventType = PaymentAuditEventType.PaymentFailed;
            auditEvent.IsSuccess = false;
            MaskSensitiveData(auditEvent);

            _logger.LogWarning(
                "[POSNET-AUDIT] ❌ {EventType} | CorrelationId: {CorrelationId} | OrderId: {OrderId} | " +
                "ErrorCode: {ErrorCode} | ErrorMessage: {ErrorMessage} | Card: {MaskedCard} | " +
                "Duration: {DurationMs}ms",
                auditEvent.EventType,
                auditEvent.CorrelationId,
                auditEvent.OrderId,
                auditEvent.ErrorCode,
                auditEvent.ErrorMessage,
                auditEvent.MaskedCardNumber,
                auditEvent.DurationMs);

            LogStructuredEvent(auditEvent);
        }

        /// <inheritdoc/>
        public void Log3DSecureCallback(PaymentAuditEvent auditEvent)
        {
            auditEvent.EventType = PaymentAuditEventType.Payment3DSecureCallback;
            MaskSensitiveData(auditEvent);

            var logLevel = auditEvent.IsSuccess ? LogLevel.Information : LogLevel.Warning;
            var icon = auditEvent.IsSuccess ? "🔐✅" : "🔐❌";

            _logger.Log(logLevel,
                "[POSNET-AUDIT] {Icon} {EventType} | CorrelationId: {CorrelationId} | OrderId: {OrderId} | " +
                "MdStatus: {MdStatus} | ResponseCode: {ResponseCode} | IP: {IpAddress}",
                icon,
                auditEvent.EventType,
                auditEvent.CorrelationId,
                auditEvent.OrderId,
                auditEvent.MdStatus,
                auditEvent.ResponseCode,
                auditEvent.IpAddress);

            LogStructuredEvent(auditEvent);
        }

        /// <inheritdoc/>
        public void LogRefundOrCancel(PaymentAuditEvent auditEvent)
        {
            MaskSensitiveData(auditEvent);

            var icon = auditEvent.IsSuccess ? "💰✅" : "💰❌";
            var logLevel = auditEvent.IsSuccess ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(logLevel,
                "[POSNET-AUDIT] {Icon} {EventType} | CorrelationId: {CorrelationId} | OrderId: {OrderId} | " +
                "TransactionId: {TransactionId} | Amount: {Amount} {Currency} | " +
                "Success: {IsSuccess} | ErrorCode: {ErrorCode}",
                icon,
                auditEvent.EventType,
                auditEvent.CorrelationId,
                auditEvent.OrderId,
                auditEvent.TransactionId,
                auditEvent.Amount,
                auditEvent.Currency,
                auditEvent.IsSuccess,
                auditEvent.ErrorCode);

            LogStructuredEvent(auditEvent);
        }

        /// <inheritdoc/>
        public void LogSecurityEvent(SecurityAuditEvent securityEvent)
        {
            var logLevel = securityEvent.Severity switch
            {
                SecuritySeverity.Info => LogLevel.Information,
                SecuritySeverity.Warning => LogLevel.Warning,
                SecuritySeverity.High => LogLevel.Error,
                SecuritySeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Warning
            };

            var icon = securityEvent.Severity switch
            {
                SecuritySeverity.Info => "ℹ️",
                SecuritySeverity.Warning => "⚠️",
                SecuritySeverity.High => "🚨",
                SecuritySeverity.Critical => "🔴",
                _ => "⚠️"
            };

            _logger.Log(logLevel,
                "[POSNET-SECURITY] {Icon} {EventType} | CorrelationId: {CorrelationId} | " +
                "Severity: {Severity} | IP: {IpAddress} | Description: {Description}",
                icon,
                securityEvent.EventType,
                securityEvent.CorrelationId,
                securityEvent.Severity,
                securityEvent.IpAddress,
                securityEvent.Description);

            // Kritik güvenlik olayları için ek işlemler yapılabilir
            // Örn: E-posta bildirimi, SMS, Slack webhook vb.
            if (securityEvent.Severity >= SecuritySeverity.High)
            {
                HandleCriticalSecurityEvent(securityEvent);
            }
        }

        /// <inheritdoc/>
        public void LogApiPerformance(ApiPerformanceEvent performanceEvent)
        {
            var logLevel = performanceEvent.DurationMs > 5000 ? LogLevel.Warning : LogLevel.Debug;
            var icon = performanceEvent.DurationMs > 5000 ? "🐢" : "⚡";

            _logger.Log(logLevel,
                "[POSNET-PERF] {Icon} {Endpoint} | CorrelationId: {CorrelationId} | " +
                "Duration: {DurationMs}ms | Status: {HttpStatusCode} | Success: {IsSuccess}",
                icon,
                performanceEvent.Endpoint,
                performanceEvent.CorrelationId,
                performanceEvent.DurationMs,
                performanceEvent.HttpStatusCode,
                performanceEvent.IsSuccess);
        }

        /// <summary>
        /// Hassas verileri maskeler
        /// </summary>
        private void MaskSensitiveData(PaymentAuditEvent auditEvent)
        {
            // Kart numarası zaten maskelenmiş olmalı, değilse maskele
            if (!string.IsNullOrEmpty(auditEvent.MaskedCardNumber) && 
                !auditEvent.MaskedCardNumber.Contains('*'))
            {
                auditEvent.MaskedCardNumber = _securityService.MaskCardNumber(auditEvent.MaskedCardNumber);
            }

            // E-posta maskele
            if (!string.IsNullOrEmpty(auditEvent.CustomerEmail))
            {
                auditEvent.CustomerEmail = _securityService.MaskSensitiveData(
                    auditEvent.CustomerEmail, SensitiveDataType.Email);
            }
        }

        /// <summary>
        /// Yapılandırılmış log kaydı (ELK, Splunk vb. için)
        /// </summary>
        private void LogStructuredEvent(PaymentAuditEvent auditEvent)
        {
            try
            {
                // JSON formatında structured log
                var json = JsonSerializer.Serialize(auditEvent, _jsonOptions);
                
                // Bu log, log aggregation sistemleri tarafından ayrıştırılabilir
                _logger.LogDebug("[POSNET-STRUCTURED] {AuditEventJson}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-AUDIT] Structured log oluşturma hatası");
            }
        }

        /// <summary>
        /// Kritik güvenlik olayı işleme
        /// </summary>
        private void HandleCriticalSecurityEvent(SecurityAuditEvent securityEvent)
        {
            // Production'da burada:
            // 1. E-posta bildirimi gönderilebilir
            // 2. SMS/Push notification gönderilebilir
            // 3. Slack/Teams webhook çağrılabilir
            // 4. SIEM sisteme olay gönderilebilir

            _logger.LogCritical(
                "[POSNET-CRITICAL-ALERT] 🚨 Kritik güvenlik olayı! " +
                "Type: {EventType} | IP: {IpAddress} | Desc: {Description}",
                securityEvent.EventType,
                securityEvent.IpAddress,
                securityEvent.Description);
        }
    }
}
