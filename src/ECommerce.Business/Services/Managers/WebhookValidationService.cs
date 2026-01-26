// ==========================================================================
// WebhookValidationService.cs - Webhook Güvenlik Doğrulama Servisi
// ==========================================================================
// HMAC-SHA256 imza doğrulama, timestamp kontrolü, idempotency yönetimi.
// Ödeme sağlayıcılarından gelen webhook'ların güvenliğini sağlar.
// ==========================================================================

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Webhook güvenlik doğrulama servisi.
    /// HMAC imza, timestamp, nonce ve idempotency kontrollerini yönetir.
    /// </summary>
    public class WebhookValidationService : IWebhookValidationService
    {
        private readonly ECommerceDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookValidationService> _logger;

        // Varsayılan timestamp toleransı (5 dakika)
        private const int DefaultMaxAgeSeconds = 300;

        public WebhookValidationService(
            ECommerceDbContext context,
            IConfiguration configuration,
            ILogger<WebhookValidationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public WebhookSignatureResult ValidateSignature(string payload, string signature, string provider)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return WebhookSignatureResult.Invalid("Payload boş olamaz.");
            }

            if (string.IsNullOrEmpty(signature))
            {
                return WebhookSignatureResult.Invalid("İmza header'ı eksik.");
            }

            // Provider'a göre secret key al
            var secretKey = GetWebhookSecret(provider);
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogWarning(
                    "⚠️ Webhook secret key bulunamadı. Provider={Provider}", provider);
                return WebhookSignatureResult.Invalid($"'{provider}' için webhook secret yapılandırılmamış.");
            }

            try
            {
                // HMAC-SHA256 hesapla
                var computedSignature = ComputeHmacSha256(payload, secretKey);

                // İmza formatına göre karşılaştır
                // Bazı provider'lar "sha256=" prefix'i kullanır
                var normalizedSignature = NormalizeSignature(signature);
                var normalizedComputed = NormalizeSignature(computedSignature);

                // Timing-safe karşılaştırma (timing attack önleme)
                var isValid = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(normalizedSignature.ToLowerInvariant()),
                    Encoding.UTF8.GetBytes(normalizedComputed.ToLowerInvariant()));

                if (!isValid)
                {
                    _logger.LogWarning(
                        "❌ Webhook imza doğrulaması başarısız. Provider={Provider}", provider);
                    
                    return new WebhookSignatureResult
                    {
                        IsValid = false,
                        ErrorMessage = "İmza doğrulaması başarısız.",
                        ComputedSignature = computedSignature
                    };
                }

                _logger.LogDebug(
                    "✅ Webhook imza doğrulaması başarılı. Provider={Provider}", provider);

                return WebhookSignatureResult.Valid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İmza doğrulama hatası. Provider={Provider}", provider);
                return WebhookSignatureResult.Invalid("İmza doğrulama sırasında hata oluştu.");
            }
        }

        /// <inheritdoc />
        public bool ValidateTimestamp(long timestamp, int maxAgeSeconds = DefaultMaxAgeSeconds)
        {
            try
            {
                // Unix timestamp'i DateTime'a çevir
                var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                var now = DateTime.UtcNow;

                // Gelecekte mi? (clock skew için 60 saniye tolerans)
                if (eventTime > now.AddSeconds(60))
                {
                    _logger.LogWarning(
                        "⚠️ Webhook timestamp gelecekte. EventTime={EventTime}, Now={Now}",
                        eventTime, now);
                    return false;
                }

                // Çok eski mi?
                var age = (now - eventTime).TotalSeconds;
                if (age > maxAgeSeconds)
                {
                    _logger.LogWarning(
                        "⚠️ Webhook timestamp çok eski. Age={Age}s, MaxAge={MaxAge}s",
                        age, maxAgeSeconds);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timestamp doğrulama hatası. Timestamp={Timestamp}", timestamp);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsEventAlreadyProcessedAsync(string provider, string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return false; // Event ID yoksa idempotency kontrolü yapamayız
            }

            // PaymentWebhookEvents tablosunda kontrol et
            var exists = await _context.PaymentWebhookEvents
                .AnyAsync(e => e.Provider == provider && 
                              e.ProviderEventId == eventId &&
                              (e.ProcessingStatus == "Processed" || e.ProcessingStatus == "Duplicate"));

            if (exists)
            {
                _logger.LogInformation(
                    "🔄 Duplicate webhook tespit edildi. Provider={Provider}, EventId={EventId}",
                    provider, eventId);
            }

            return exists;
        }

        /// <inheritdoc />
        public async Task<int> RecordWebhookEventAsync(WebhookEventRecord eventRecord)
        {
            var entity = new PaymentWebhookEvent
            {
                Provider = eventRecord.Provider,
                ProviderEventId = eventRecord.ProviderEventId,
                PaymentIntentId = eventRecord.PaymentIntentId,
                EventType = eventRecord.EventType,
                RawPayload = MaskSensitiveData(eventRecord.RawPayload),
                Signature = MaskSignature(eventRecord.Signature),
                SignatureValid = eventRecord.SignatureValid,
                SourceIpAddress = eventRecord.SourceIpAddress,
                EventTimestamp = eventRecord.EventTimestamp.HasValue 
                    ? DateTimeOffset.FromUnixTimeSeconds(eventRecord.EventTimestamp.Value).UtcDateTime 
                    : null,
                ProcessingStatus = eventRecord.ProcessingStatus.ToString(),
                ReceivedAt = DateTime.UtcNow,
                OrderId = eventRecord.OrderId
            };

            _context.PaymentWebhookEvents.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "📝 Webhook event kaydedildi. Id={Id}, Provider={Provider}, EventId={EventId}, Type={Type}",
                entity.Id, entity.Provider, entity.ProviderEventId, entity.EventType);

            return entity.Id;
        }

        /// <inheritdoc />
        public async Task UpdateEventStatusAsync(int eventId, WebhookProcessingStatus status, string? errorMessage = null)
        {
            var entity = await _context.PaymentWebhookEvents.FindAsync(eventId);
            if (entity == null)
            {
                _logger.LogWarning("Webhook event bulunamadı. Id={Id}", eventId);
                return;
            }

            entity.ProcessingStatus = status.ToString();
            entity.ProcessedAt = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                entity.ErrorMessage = errorMessage;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "📝 Webhook event durumu güncellendi. Id={Id}, Status={Status}",
                eventId, status);
        }

        /// <inheritdoc />
        public async Task<WebhookValidationResult> ValidateWebhookAsync(WebhookValidationRequest request)
        {
            try
            {
                // 1. Idempotency kontrolü
                if (!string.IsNullOrEmpty(request.EventId))
                {
                    var isDuplicate = await IsEventAlreadyProcessedAsync(request.Provider, request.EventId);
                    if (isDuplicate)
                    {
                        // Duplicate event'i de kaydet (audit için)
                        await RecordWebhookEventAsync(new WebhookEventRecord
                        {
                            Provider = request.Provider,
                            ProviderEventId = request.EventId,
                            EventType = request.EventType ?? "unknown",
                            RawPayload = request.RawPayload,
                            Signature = request.Signature,
                            SignatureValid = true, // Varsayılan
                            SourceIpAddress = request.SourceIpAddress,
                            EventTimestamp = request.Timestamp,
                            OrderId = request.OrderId,
                            ProcessingStatus = WebhookProcessingStatus.Duplicate
                        });

                        return WebhookValidationResult.Duplicate(request.EventId);
                    }
                }

                // 2. Timestamp kontrolü (varsa)
                if (request.Timestamp.HasValue)
                {
                    var timestampValid = ValidateTimestamp(request.Timestamp.Value);
                    if (!timestampValid)
                    {
                        // Geçersiz timestamp'i kaydet
                        await RecordWebhookEventAsync(new WebhookEventRecord
                        {
                            Provider = request.Provider,
                            ProviderEventId = request.EventId ?? Guid.NewGuid().ToString(),
                            EventType = request.EventType ?? "unknown",
                            RawPayload = request.RawPayload,
                            Signature = request.Signature,
                            SignatureValid = false,
                            SourceIpAddress = request.SourceIpAddress,
                            EventTimestamp = request.Timestamp,
                            OrderId = request.OrderId,
                            ProcessingStatus = WebhookProcessingStatus.InvalidTimestamp
                        });

                        return WebhookValidationResult.InvalidTimestamp();
                    }
                }

                // 3. İmza doğrulama
                bool signatureValid = true;
                if (!string.IsNullOrEmpty(request.Signature))
                {
                    var signatureResult = ValidateSignature(
                        request.RawPayload, 
                        request.Signature, 
                        request.Provider);

                    signatureValid = signatureResult.IsValid;

                    if (!signatureValid)
                    {
                        // Geçersiz imzayı kaydet
                        await RecordWebhookEventAsync(new WebhookEventRecord
                        {
                            Provider = request.Provider,
                            ProviderEventId = request.EventId ?? Guid.NewGuid().ToString(),
                            EventType = request.EventType ?? "unknown",
                            RawPayload = request.RawPayload,
                            Signature = request.Signature,
                            SignatureValid = false,
                            SourceIpAddress = request.SourceIpAddress,
                            EventTimestamp = request.Timestamp,
                            OrderId = request.OrderId,
                            ProcessingStatus = WebhookProcessingStatus.InvalidSignature
                        });

                        return WebhookValidationResult.InvalidSignature(
                            signatureResult.ErrorMessage ?? "İmza doğrulaması başarısız.");
                    }
                }
                else
                {
                    // İmza zorunlu mu kontrol et
                    var requireSignature = _configuration.GetValue<bool>($"Webhooks:{request.Provider}:RequireSignature", true);
                    if (requireSignature)
                    {
                        _logger.LogWarning(
                            "⚠️ İmza zorunlu ama header eksik. Provider={Provider}", request.Provider);
                        
                        return WebhookValidationResult.InvalidSignature("İmza header'ı zorunlu.");
                    }
                }

                // 4. Tüm doğrulamalar başarılı - kaydet
                var recordId = await RecordWebhookEventAsync(new WebhookEventRecord
                {
                    Provider = request.Provider,
                    ProviderEventId = request.EventId ?? Guid.NewGuid().ToString(),
                    PaymentIntentId = request.PaymentIntentId,
                    EventType = request.EventType ?? "unknown",
                    RawPayload = request.RawPayload,
                    Signature = request.Signature,
                    SignatureValid = signatureValid,
                    SourceIpAddress = request.SourceIpAddress,
                    EventTimestamp = request.Timestamp,
                    OrderId = request.OrderId,
                    ProcessingStatus = WebhookProcessingStatus.Received
                });

                _logger.LogInformation(
                    "✅ Webhook doğrulama başarılı. Provider={Provider}, EventId={EventId}, RecordId={RecordId}",
                    request.Provider, request.EventId, recordId);

                return WebhookValidationResult.Success(recordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Webhook doğrulama hatası. Provider={Provider}", request.Provider);
                
                return WebhookValidationResult.Failed(
                    "VALIDATION_ERROR",
                    "Webhook doğrulama sırasında beklenmeyen bir hata oluştu.");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Provider'a göre webhook secret key'i getirir.
        /// </summary>
        private string? GetWebhookSecret(string provider)
        {
            // Önce environment variable'dan bak
            var envKey = $"WEBHOOK_SECRET_{provider.ToUpperInvariant()}";
            var secret = Environment.GetEnvironmentVariable(envKey);
            
            if (!string.IsNullOrEmpty(secret))
            {
                return secret;
            }

            // Configuration'dan bak
            secret = _configuration[$"Webhooks:{provider}:Secret"];
            if (!string.IsNullOrEmpty(secret))
            {
                return secret;
            }

            // Genel webhook secret
            secret = _configuration["Webhooks:DefaultSecret"];
            
            return secret;
        }

        /// <summary>
        /// HMAC-SHA256 hesaplar.
        /// </summary>
        private static string ComputeHmacSha256(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// İmza formatını normalize eder (prefix'leri kaldırır).
        /// </summary>
        private static string NormalizeSignature(string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return string.Empty;

            // "sha256=" veya "v1=" gibi prefix'leri kaldır
            var prefixes = new[] { "sha256=", "v1=", "sha256:", "hmac-sha256:" };
            
            foreach (var prefix in prefixes)
            {
                if (signature.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return signature.Substring(prefix.Length);
                }
            }

            return signature;
        }

        /// <summary>
        /// Hassas verileri maskeler (kart numarası, CVV vb.).
        /// </summary>
        private static string? MaskSensitiveData(string? payload)
        {
            if (string.IsNullOrEmpty(payload))
                return payload;

            // Basit maskeleme - production'da daha kapsamlı olmalı
            var masked = payload;

            // Kart numarası maskeleme (16 haneli sayılar)
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"\b(\d{4})\d{8}(\d{4})\b",
                "$1****$2");

            // CVV maskeleme
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"""cvv""\s*:\s*""\d{3,4}""",
                @"""cvv"":""***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Son kullanma tarihi maskeleme
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                @"""expiry""\s*:\s*""\d{2}/\d{2,4}""",
                @"""expiry"":""**/**""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return masked;
        }

        /// <summary>
        /// İmzanın tamamını loglamaz, sadece ilk birkaç karakteri gösterir.
        /// </summary>
        private static string? MaskSignature(string? signature)
        {
            if (string.IsNullOrEmpty(signature) || signature.Length <= 8)
                return signature;

            return signature.Substring(0, 8) + "...";
        }

        #endregion
    }
}
