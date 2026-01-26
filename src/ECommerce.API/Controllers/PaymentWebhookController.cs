// ==========================================================================
// PaymentWebhookController.cs - Ödeme Webhook Handler
// ==========================================================================
// Ödeme sağlayıcılarından gelen webhook'ları işler.
// HMAC güvenlik, idempotency, timestamp kontrolü içerir.
// ==========================================================================

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ödeme webhook endpoint'leri.
    /// Her provider için ayrı endpoint.
    /// </summary>
    [ApiController]
    [Route("api/webhooks")]
    [AllowAnonymous] // Webhook'lar authentication gerektirmez, imza ile doğrulanır
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IWebhookValidationService _webhookService;
        private readonly IPaymentCaptureService _paymentCaptureService;
        private readonly IOrderStateMachine _orderStateMachine;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ECommerceDbContext _context;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IWebhookValidationService webhookService,
            IPaymentCaptureService paymentCaptureService,
            IOrderStateMachine orderStateMachine,
            IRealTimeNotificationService notificationService,
            ECommerceDbContext context,
            ILogger<PaymentWebhookController> logger)
        {
            _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
            _paymentCaptureService = paymentCaptureService ?? throw new ArgumentNullException(nameof(paymentCaptureService));
            _orderStateMachine = orderStateMachine ?? throw new ArgumentNullException(nameof(orderStateMachine));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Iyzico webhook endpoint'i.
        /// POST /api/webhooks/iyzico
        /// </summary>
        [HttpPost("iyzico")]
        public async Task<IActionResult> HandleIyzicoWebhook()
        {
            return await ProcessWebhookAsync("iyzico", "X-Iyz-Signature", "iyziEventType");
        }

        /// <summary>
        /// PayTR webhook endpoint'i.
        /// POST /api/webhooks/paytr
        /// </summary>
        [HttpPost("paytr")]
        public async Task<IActionResult> HandlePaytrWebhook()
        {
            return await ProcessWebhookAsync("paytr", "X-PayTR-Signature", "type");
        }

        /// <summary>
        /// Stripe webhook endpoint'i.
        /// POST /api/webhooks/stripe
        /// </summary>
        [HttpPost("stripe")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            return await ProcessWebhookAsync("stripe", "Stripe-Signature", "type");
        }

        /// <summary>
        /// POSNET webhook endpoint'i (Yapı Kredi).
        /// POST /api/webhooks/posnet
        /// </summary>
        [HttpPost("posnet")]
        public async Task<IActionResult> HandlePosnetWebhook()
        {
            return await ProcessWebhookAsync("posnet", "X-Posnet-Signature", "eventType");
        }

        /// <summary>
        /// Genel webhook handler.
        /// POST /api/webhooks/{provider}
        /// </summary>
        [HttpPost("{provider}")]
        public async Task<IActionResult> HandleGenericWebhook(string provider)
        {
            return await ProcessWebhookAsync(provider, "X-Webhook-Signature", "type");
        }

        #region Private Methods

        /// <summary>
        /// Webhook'u işleyen ana metod.
        /// Tüm doğrulamaları yapar ve event'i işler.
        /// </summary>
        private async Task<IActionResult> ProcessWebhookAsync(
            string provider, 
            string signatureHeader, 
            string eventTypeField)
        {
            int? recordedEventId = null;
            
            try
            {
                // 1. Raw body'yi oku (model binding öncesi, imza doğrulama için)
                string rawBody;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    rawBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(rawBody))
                {
                    _logger.LogWarning("❌ Boş webhook body. Provider={Provider}", provider);
                    return BadRequest(new { error = "Empty body" });
                }

                // 2. Header'lardan bilgileri al
                var signature = Request.Headers[signatureHeader].ToString();
                var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                // 3. JSON parse et
                JsonDocument? jsonDoc = null;
                string? eventId = null;
                string? eventType = null;
                long? timestamp = null;
                int? orderId = null;
                string? paymentIntentId = null;

                try
                {
                    jsonDoc = JsonDocument.Parse(rawBody);
                    var root = jsonDoc.RootElement;

                    // Event ID'yi bul (provider'a göre farklı field adları)
                    eventId = TryGetJsonString(root, "id", "event_id", "eventId", "webhookId");
                    eventType = TryGetJsonString(root, eventTypeField, "event", "event_type");
                    
                    // Timestamp'i bul
                    if (root.TryGetProperty("timestamp", out var tsElement) ||
                        root.TryGetProperty("created", out tsElement) ||
                        root.TryGetProperty("created_at", out tsElement))
                    {
                        if (tsElement.ValueKind == JsonValueKind.Number)
                        {
                            timestamp = tsElement.GetInt64();
                        }
                        else if (tsElement.ValueKind == JsonValueKind.String)
                        {
                            if (long.TryParse(tsElement.GetString(), out var ts))
                            {
                                timestamp = ts;
                            }
                        }
                    }

                    // Order ID'yi bul
                    var orderIdStr = TryGetJsonString(root, "orderId", "order_id", "merchantOrderId", 
                        "merchant_order_id", "reference");
                    if (!string.IsNullOrEmpty(orderIdStr) && int.TryParse(orderIdStr, out var oid))
                    {
                        orderId = oid;
                    }

                    // Payment Intent ID'yi bul
                    paymentIntentId = TryGetJsonString(root, "paymentId", "payment_id", "transactionId",
                        "transaction_id", "intent_id");
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "❌ Webhook JSON parse hatası. Provider={Provider}", provider);
                    return BadRequest(new { error = "Invalid JSON" });
                }

                _logger.LogInformation(
                    "📥 Webhook alındı. Provider={Provider}, EventType={EventType}, EventId={EventId}, OrderId={OrderId}",
                    provider, eventType ?? "unknown", eventId ?? "N/A", orderId);

                // 4. Webhook doğrulama
                var validationRequest = new WebhookValidationRequest
                {
                    Provider = provider,
                    RawPayload = rawBody,
                    Signature = string.IsNullOrEmpty(signature) ? null : signature,
                    EventId = eventId,
                    EventType = eventType,
                    Timestamp = timestamp,
                    SourceIpAddress = sourceIp,
                    OrderId = orderId,
                    PaymentIntentId = paymentIntentId
                };

                var validationResult = await _webhookService.ValidateWebhookAsync(validationRequest);
                recordedEventId = validationResult.RecordedEventId;

                // Duplicate event - 200 OK dön ama işleme
                if (validationResult.IsDuplicate)
                {
                    _logger.LogInformation(
                        "🔄 Duplicate webhook, işlenmedi. Provider={Provider}, EventId={EventId}",
                        provider, eventId);
                    return Ok(new { status = "duplicate", message = "Event already processed" });
                }

                // Doğrulama başarısız
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "❌ Webhook doğrulama başarısız. Provider={Provider}, Error={Error}",
                        provider, validationResult.ErrorMessage);

                    // İmza hatası ciddi - 401 dön
                    if (validationResult.ErrorCode == "INVALID_SIGNATURE")
                    {
                        return Unauthorized(new { error = "Invalid signature" });
                    }

                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                // 5. Event'i işle
                await ProcessWebhookEventAsync(provider, eventType, jsonDoc!.RootElement, orderId, paymentIntentId);

                // 6. Başarılı işleme - durumu güncelle
                if (recordedEventId.HasValue)
                {
                    await _webhookService.UpdateEventStatusAsync(
                        recordedEventId.Value, 
                        WebhookProcessingStatus.Processed);
                }

                _logger.LogInformation(
                    "✅ Webhook başarıyla işlendi. Provider={Provider}, EventType={EventType}, OrderId={OrderId}",
                    provider, eventType, orderId);

                // Provider'a göre response formatı
                return Ok(new { status = "success", received = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "❌ Webhook işleme hatası. Provider={Provider}", provider);

                // Hata durumunu kaydet
                if (recordedEventId.HasValue)
                {
                    await _webhookService.UpdateEventStatusAsync(
                        recordedEventId.Value,
                        WebhookProcessingStatus.Failed,
                        ex.Message);
                }

                // 500 yerine 200 dön - webhook'ların yeniden denenmesini önlemek için
                // (hata loglandı, manuel müdahale gerekebilir)
                return Ok(new { status = "error", message = "Internal error, logged for review" });
            }
        }

        /// <summary>
        /// Webhook event'ini tipine göre işler.
        /// </summary>
        private async Task ProcessWebhookEventAsync(
            string provider, 
            string? eventType, 
            JsonElement payload,
            int? orderId,
            string? paymentIntentId)
        {
            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("Event type belirtilmemiş. Provider={Provider}", provider);
                return;
            }

            var normalizedType = eventType.ToLowerInvariant();

            // Payment success events
            if (normalizedType.Contains("payment.success") ||
                normalizedType.Contains("payment.completed") ||
                normalizedType.Contains("charge.succeeded"))
            {
                await HandlePaymentSuccessAsync(orderId, paymentIntentId, payload);
            }
            // Payment failed events
            else if (normalizedType.Contains("payment.failed") ||
                     normalizedType.Contains("charge.failed"))
            {
                await HandlePaymentFailedAsync(orderId, paymentIntentId, payload);
            }
            // Refund events
            else if (normalizedType.Contains("refund"))
            {
                await HandleRefundAsync(orderId, paymentIntentId, payload);
            }
            // Chargeback events
            else if (normalizedType.Contains("chargeback") ||
                     normalizedType.Contains("dispute"))
            {
                await HandleChargebackAsync(orderId, paymentIntentId, payload);
            }
            // Authorization events
            else if (normalizedType.Contains("authorization") ||
                     normalizedType.Contains("preauth"))
            {
                await HandleAuthorizationAsync(orderId, paymentIntentId, payload);
            }
            // Capture events
            else if (normalizedType.Contains("capture"))
            {
                await HandleCaptureAsync(orderId, paymentIntentId, payload);
            }
            else
            {
                _logger.LogInformation(
                    "📋 Bilinmeyen event tipi, işlenmedi. Type={Type}, Provider={Provider}",
                    eventType, provider);
            }
        }

        /// <summary>
        /// Ödeme başarılı webhook'unu işler.
        /// </summary>
        private async Task HandlePaymentSuccessAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Payment success webhook: OrderId bulunamadı.");
                return;
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId.Value);

            if (order == null)
            {
                _logger.LogWarning("Payment success webhook: Sipariş bulunamadı. OrderId={OrderId}", orderId);
                return;
            }

            // Sipariş durumunu güncelle
            if (order.Status == Entities.Enums.OrderStatus.New || 
                order.Status == Entities.Enums.OrderStatus.Pending)
            {
                await _orderStateMachine.TransitionAsync(
                    orderId.Value,
                    Entities.Enums.OrderStatus.Confirmed,
                    null, // System
                    "Ödeme webhook'u ile onaylandı");
            }

            // Admin'e bildirim
            await _notificationService.NotifyPaymentSuccessAsync(
                orderId.Value,
                order.OrderNumber,
                order.FinalPrice,
                "Webhook");

            _logger.LogInformation(
                "✅ Ödeme başarılı işlendi. OrderId={OrderId}, Amount={Amount}",
                orderId, order.FinalPrice);
        }

        /// <summary>
        /// Ödeme başarısız webhook'unu işler.
        /// </summary>
        private async Task HandlePaymentFailedAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Payment failed webhook: OrderId bulunamadı.");
                return;
            }

            var order = await _context.Orders.FindAsync(orderId.Value);
            if (order == null) return;

            // Sipariş durumunu güncelle
            await _orderStateMachine.TransitionAsync(
                orderId.Value,
                Entities.Enums.OrderStatus.PaymentFailed,
                null,
                "Ödeme webhook'u ile başarısız olarak işaretlendi");

            // Admin'e bildirim
            var reason = TryGetJsonString(payload, "failure_reason", "error_message", "message") ?? "Bilinmiyor";
            await _notificationService.NotifyPaymentFailedAsync(
                orderId.Value,
                order.OrderNumber,
                reason,
                "Webhook");

            _logger.LogWarning(
                "❌ Ödeme başarısız işlendi. OrderId={OrderId}, Reason={Reason}",
                orderId, reason);
        }

        /// <summary>
        /// İade webhook'unu işler.
        /// </summary>
        private async Task HandleRefundAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Refund webhook: OrderId bulunamadı.");
                return;
            }

            var order = await _context.Orders.FindAsync(orderId.Value);
            if (order == null) return;

            // İade tutarını al
            decimal refundAmount = 0;
            if (payload.TryGetProperty("amount", out var amountElement))
            {
                if (amountElement.ValueKind == JsonValueKind.Number)
                {
                    refundAmount = amountElement.GetDecimal();
                }
            }

            // Sipariş durumunu güncelle
            await _orderStateMachine.TransitionAsync(
                orderId.Value,
                Entities.Enums.OrderStatus.Refunded,
                null,
                $"Webhook üzerinden iade yapıldı: {refundAmount:N2} TL");

            // Admin'e bildirim
            await _notificationService.NotifyRefundRequestedAsync(
                orderId.Value,
                order.OrderNumber,
                refundAmount,
                "Webhook üzerinden iade");

            _logger.LogInformation(
                "💰 İade işlendi. OrderId={OrderId}, Amount={Amount}",
                orderId, refundAmount);
        }

        /// <summary>
        /// Chargeback webhook'unu işler.
        /// </summary>
        private async Task HandleChargebackAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Chargeback webhook: OrderId bulunamadı.");
                return;
            }

            var order = await _context.Orders.FindAsync(orderId.Value);
            if (order == null) return;

            // Sipariş durumunu güncelle
            await _orderStateMachine.TransitionAsync(
                orderId.Value,
                Entities.Enums.OrderStatus.ChargebackPending,
                null,
                "Chargeback webhook'u ile itiraz bekliyor");

            // Admin'e acil bildirim
            await _notificationService.NotifyAdminAlertAsync(
                "critical",
                "Chargeback Uyarısı",
                $"Sipariş #{order.OrderNumber} için chargeback talebi alındı!",
                $"/admin/orders/{orderId}");

            _logger.LogWarning(
                "⚠️ Chargeback işlendi. OrderId={OrderId}", orderId);
        }

        /// <summary>
        /// Authorization webhook'unu işler.
        /// </summary>
        private async Task HandleAuthorizationAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Authorization webhook: OrderId bulunamadı.");
                return;
            }

            // Provizyon tutarını al
            decimal authorizedAmount = 0;
            if (payload.TryGetProperty("authorized_amount", out var amountElement) ||
                payload.TryGetProperty("amount", out amountElement))
            {
                if (amountElement.ValueKind == JsonValueKind.Number)
                {
                    authorizedAmount = amountElement.GetDecimal();
                }
            }

            var order = await _context.Orders.FindAsync(orderId.Value);
            if (order != null)
            {
                order.AuthorizedAmount = authorizedAmount;
                order.CaptureStatus = Entities.Enums.CaptureStatus.Pending;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "💳 Authorization işlendi. OrderId={OrderId}, Amount={Amount}",
                orderId, authorizedAmount);
        }

        /// <summary>
        /// Capture webhook'unu işler.
        /// </summary>
        private async Task HandleCaptureAsync(int? orderId, string? paymentIntentId, JsonElement payload)
        {
            if (!orderId.HasValue)
            {
                _logger.LogWarning("Capture webhook: OrderId bulunamadı.");
                return;
            }

            // Capture tutarını al
            decimal capturedAmount = 0;
            if (payload.TryGetProperty("captured_amount", out var amountElement) ||
                payload.TryGetProperty("amount", out amountElement))
            {
                if (amountElement.ValueKind == JsonValueKind.Number)
                {
                    capturedAmount = amountElement.GetDecimal();
                }
            }

            var order = await _context.Orders.FindAsync(orderId.Value);
            if (order != null)
            {
                order.CapturedAmount = capturedAmount;
                order.CapturedAt = DateTime.UtcNow;
                order.CaptureStatus = Entities.Enums.CaptureStatus.Success;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "💰 Capture işlendi. OrderId={OrderId}, Amount={Amount}",
                orderId, capturedAmount);
        }

        /// <summary>
        /// JSON'dan string değer almaya çalışır (birden fazla field adı dener).
        /// </summary>
        private static string? TryGetJsonString(JsonElement element, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (element.TryGetProperty(fieldName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        return prop.GetString();
                    }
                    if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.GetRawText();
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
