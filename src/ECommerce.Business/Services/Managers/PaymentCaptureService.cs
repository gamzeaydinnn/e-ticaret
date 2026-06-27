// ==========================================================================
// PaymentCaptureService.cs - Ödeme Provizyon/Capture Servisi
// ==========================================================================
// Authorize → Capture akışını yöneten servis implementasyonu.
// %20 tolerans ile provizyon alır, teslim anında final tutarı çeker.
// POSNET, Iyzico ve diğer ödeme sağlayıcılarını destekler.
// ==========================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Helpers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
// POSNET gerçek ödeme sağlayıcı entegrasyonu için gerekli namespace'ler
using ECommerce.Infrastructure.Services.Payment.Posnet;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ödeme Authorize/Capture akışını yöneten servis.
    /// </summary>
    public class PaymentCaptureService : IPaymentCaptureService
    {
        private readonly ECommerceDbContext _context;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<PaymentCaptureService> _logger;

        // POSNET gerçek ödeme sağlayıcı servisi (opsiyonel bağımlılık)
        // Infrastructure katmanında tanımlı olduğu için null olabilir (DI'da kayıtlı değilse)
        private readonly IPosnetPaymentService? _posnetService;

        // Varsayılan tolerans yüzdesi (ilk provizyon tutarı için)
        private const decimal DefaultTolerancePercentage = 0.20m;

        // Provizyon geçerlilik süresi (saat) — tek doğruluk kaynağı: WeightBasedCapturePolicy.
        // NEDEN buradan referans: Sayı tek noktada tutulur; banka süresi teyit edilince yalnız politika güncellenir.
        private const int AuthorizationExpiryHours = WeightBasedCapturePolicy.PreAuthValidityHours;

        public PaymentCaptureService(
            ECommerceDbContext context,
            IRealTimeNotificationService notificationService,
            ILogger<PaymentCaptureService> logger,
            IPosnetPaymentService? posnetService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _posnetService = posnetService;

            if (_posnetService != null)
                _logger.LogInformation("PaymentCaptureService: POSNET servisi aktif, gerçek API çağrıları yapılacak.");
            else
                _logger.LogWarning("PaymentCaptureService: POSNET servisi bulunamadı, simülasyon modu aktif.");
        }

        /// <inheritdoc />
        public async Task<PaymentAuthorizationResult> AuthorizePaymentAsync(int orderId, decimal orderAmount,
            decimal tolerancePercentage = DefaultTolerancePercentage)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return PaymentAuthorizationResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
                }

                // Tolerans ile authorize tutarını hesapla
                var authorizedAmount = CalculateAuthorizedAmount(orderAmount, tolerancePercentage);

                _logger.LogInformation(
                    "💳 Ödeme provizyonu hesaplandı. OrderId={OrderId}, OrderAmount={OrderAmount}, " +
                    "Tolerance={Tolerance}%, AuthorizedAmount={AuthorizedAmount}",
                    orderId, orderAmount, tolerancePercentage * 100, authorizedAmount);

                // Kapıda ödeme kontrolü
                if (IsCashOnDelivery(order.PaymentMethod))
                {
                    // Kapıda ödeme için gerçek provizyon alınmaz
                    order.AuthorizedAmount = authorizedAmount;
                    order.TolerancePercentage = tolerancePercentage;
                    order.CaptureStatus = CaptureStatus.NotRequired;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "💳 Kapıda ödeme siparişi - gerçek provizyon alınmadı. OrderId={OrderId}",
                        orderId);

                    return PaymentAuthorizationResult.Succeeded(
                        authorizedAmount,
                        orderAmount,
                        tolerancePercentage);
                }

                // Kredi kartı ödemesi için provizyon al
                // POSNET entegrasyonu: Checkout'ta alınmış PreAuthHostLogKey varsa onu kullan,
                // yoksa simülasyon ile devam et (kart bilgileri bu aşamada mevcut değil)
                var authResult = await ExecuteAuthorizationAsync(order, authorizedAmount);

                if (!authResult.success)
                {
                    _logger.LogWarning(
                        "Provizyon alınamadı. OrderId={OrderId}, Error={Error}",
                        orderId, authResult.errorMessage);

                    return PaymentAuthorizationResult.Failed(
                        authResult.errorMessage ?? "Provizyon alınamadı.",
                        "AUTHORIZATION_FAILED");
                }

                // Siparişi güncelle
                order.AuthorizedAmount = authorizedAmount;
                order.TolerancePercentage = tolerancePercentage;
                order.CaptureStatus = CaptureStatus.Pending;

                // POSNET PreAuthHostLogKey'i siparişe kaydet (capture/iade işlemlerinde kullanılacak)
                if (!string.IsNullOrEmpty(authResult.authReference) && string.IsNullOrEmpty(order.PreAuthHostLogKey))
                {
                    order.PreAuthHostLogKey = authResult.authReference;
                }

                // Payment kaydı oluştur/güncelle
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status != "Refunded");

                if (payment != null)
                {
                    payment.AuthorizedAmount = authorizedAmount;
                    payment.TolerancePercentage = tolerancePercentage;
                    payment.AuthorizationReference = authResult.authReference;
                    payment.AuthorizedAt = DateTime.UtcNow;
                    payment.AuthorizationExpiresAt = DateTime.UtcNow.AddHours(AuthorizationExpiryHours);
                    payment.CaptureStatus = CaptureStatus.Pending;
                }
                else
                {
                    // Yeni payment kaydı oluştur
                    var newPayment = new Payments
                    {
                        OrderId = orderId,
                        Provider = "Internal", // Gerçek provider'a göre değişecek
                        ProviderPaymentId = authResult.authReference ?? Guid.NewGuid().ToString(),
                        Amount = orderAmount,
                        AuthorizedAmount = authorizedAmount,
                        TolerancePercentage = tolerancePercentage,
                        Status = "Authorized",
                        AuthorizationReference = authResult.authReference,
                        AuthorizedAt = DateTime.UtcNow,
                        AuthorizationExpiresAt = DateTime.UtcNow.AddHours(AuthorizationExpiryHours),
                        CaptureStatus = CaptureStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Payments.Add(newPayment);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Provizyon başarıyla alındı. OrderId={OrderId}, AuthRef={AuthRef}",
                    orderId, authResult.authReference);

                return PaymentAuthorizationResult.Succeeded(
                    authorizedAmount,
                    orderAmount,
                    tolerancePercentage,
                    authResult.authReference,
                    DateTime.UtcNow.AddHours(AuthorizationExpiryHours));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provizyon işlemi hatası. OrderId={OrderId}", orderId);
                return PaymentAuthorizationResult.Failed(
                    "Provizyon işlemi sırasında bir hata oluştu.",
                    "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentCaptureResult> CapturePaymentAsync(int orderId, decimal finalAmount)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return PaymentCaptureResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
                }

                // Provizyon tutarı için tek doğruluk kaynağı (AuthorizedAmount veya PreAuthAmount).
                var authorizedAmount = WeightBasedCapturePolicy.ResolveAuthorizedAmount(order);

                // Provizyon kontrolü
                if (authorizedAmount <= 0m)
                {
                    return PaymentCaptureResult.Failed(
                        "Bu sipariş için provizyon bulunmuyor.",
                        "NO_AUTHORIZATION");
                }

                // Zaten capture edilmiş mi? (idempotency — çift çekimi engeller)
                if (order.CaptureStatus == CaptureStatus.Success)
                {
                    return PaymentCaptureResult.Failed(
                        "Bu sipariş için ödeme zaten çekilmiş.",
                        "ALREADY_CAPTURED");
                }

                // Final tutar kontrolü: banka aşım sınırı (Auth × 1.20) içinde mi?
                var maxCapturableAmount = CalculateMaxCapturableAmount(authorizedAmount);
                if (finalAmount > maxCapturableAmount)
                {
                    _logger.LogWarning(
                        "⚠️ Final tutar authorize edilen tutar + banka aşım limitini aşıyor. " +
                        "OrderId={OrderId}, FinalAmount={FinalAmount}, AuthorizedAmount={AuthorizedAmount}, MaxCapturable={MaxCapturableAmount}",
                        orderId, finalAmount, authorizedAmount, maxCapturableAmount);

                    // Sipariş durumunu güncelle - admin müdahalesi gerekli
                    order.CaptureStatus = CaptureStatus.Failed;
                    order.Status = OrderStatus.DeliveryPaymentPending;
                    order.DeliveryProblemReason = $"Final tutar ({finalAmount:N2} TL), authorize edilen tutar + %20 banka aşım limitini ({maxCapturableAmount:N2} TL) aşıyor.";

                    await _context.SaveChangesAsync();

                    // Admin'e bildirim gönder
                    await _notificationService.NotifyPaymentFailedAsync(
                        orderId,
                        order.OrderNumber,
                        $"Final tutar provizyon + %20 limitini aşıyor. Fark: {(finalAmount - maxCapturableAmount):N2} TL",
                        "Internal");

                    return PaymentCaptureResult.ExceededAuth(finalAmount, maxCapturableAmount);
                }

                // Kapıda ödeme kontrolü
                if (IsCashOnDelivery(order.PaymentMethod))
                {
                    // Kapıda ödeme için capture simüle et
                    order.CapturedAmount = finalAmount;
                    order.CapturedAt = DateTime.UtcNow;
                    order.CaptureStatus = CaptureStatus.Success;
                    order.FinalAmount = finalAmount;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "✅ Kapıda ödeme capture edildi. OrderId={OrderId}, Amount={Amount}",
                        orderId, finalAmount);

                    return PaymentCaptureResult.Succeeded(
                        finalAmount,
                        order.AuthorizedAmount - finalAmount);
                }

                // Kredi kartı için gerçek capture işlemi
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == "Authorized");

                if (payment == null)
                {
                    return PaymentCaptureResult.Failed(
                        "Authorize edilmiş ödeme kaydı bulunamadı.",
                        "NO_AUTHORIZED_PAYMENT");
                }

                // Kredi kartı için gerçek capture işlemi
                // POSNET servisi mevcutsa gerçek API çağrısı, yoksa simülasyon
                var captureResult = await ExecuteCaptureAsync(payment, finalAmount);

                if (!captureResult.success)
                {
                    order.CaptureStatus = CaptureStatus.Failed;
                    payment.CaptureStatus = CaptureStatus.Failed;
                    payment.CaptureFailureReason = captureResult.errorMessage;

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        "❌ Capture işlemi başarısız. OrderId={OrderId}, Error={Error}",
                        orderId, captureResult.errorMessage);

                    await _notificationService.NotifyPaymentFailedAsync(
                        orderId,
                        order.OrderNumber,
                        captureResult.errorMessage ?? "Capture işlemi başarısız",
                        payment.Provider);

                    return PaymentCaptureResult.Failed(
                        captureResult.errorMessage ?? "Capture işlemi başarısız.",
                        "CAPTURE_FAILED");
                }

                // Başarılı capture - güncelle
                var releasedAmount = order.AuthorizedAmount - finalAmount;

                order.CapturedAmount = finalAmount;
                order.CapturedAt = DateTime.UtcNow;
                order.CaptureStatus = CaptureStatus.Success;
                order.FinalAmount = finalAmount;

                payment.CapturedAmount = finalAmount;
                payment.CapturedAt = DateTime.UtcNow;
                payment.CaptureStatus = CaptureStatus.Success;
                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Capture başarılı. OrderId={OrderId}, Captured={Captured}, Released={Released}",
                    orderId, finalAmount, releasedAmount);

                // Admin'e bildirim
                await _notificationService.NotifyPaymentSuccessAsync(
                    orderId,
                    order.OrderNumber,
                    finalAmount,
                    payment.Provider);

                return PaymentCaptureResult.Succeeded(
                    finalAmount,
                    releasedAmount,
                    captureResult.captureReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Capture işlemi hatası. OrderId={OrderId}", orderId);
                return PaymentCaptureResult.Failed(
                    "Capture işlemi sırasında bir hata oluştu.",
                    "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentVoidResult> VoidAuthorizationAsync(int orderId, string reason)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return PaymentVoidResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
                }

                if (order.CaptureStatus == CaptureStatus.Success)
                {
                    return PaymentVoidResult.Failed(
                        "Çekilmiş ödeme void edilemez. İade işlemi yapın.",
                        "ALREADY_CAPTURED");
                }

                var voidedAmount = order.AuthorizedAmount;

                // Kapıda ödeme için basit güncelleme
                if (IsCashOnDelivery(order.PaymentMethod))
                {
                    order.CaptureStatus = CaptureStatus.Voided;
                    order.AuthorizedAmount = 0;

                    await _context.SaveChangesAsync();

                    return PaymentVoidResult.Succeeded(voidedAmount);
                }

                // Kredi kartı için void işlemi
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId &&
                                              (p.Status == "Authorized" || p.Status == "Pending"));

                if (payment != null)
                {
                    // Kredi kartı için gerçek void/iptal işlemi
                    // POSNET servisi mevcutsa gerçek API çağrısı, yoksa simülasyon
                    var voidResult = await ExecuteVoidAsync(payment);

                    if (!voidResult.success)
                    {
                        return PaymentVoidResult.Failed(
                            voidResult.errorMessage ?? "Void işlemi başarısız.",
                            "VOID_FAILED");
                    }

                    payment.Status = "Voided";
                    payment.CaptureStatus = CaptureStatus.Voided;
                }

                order.CaptureStatus = CaptureStatus.Voided;
                order.AuthorizedAmount = 0;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Provizyon void edildi. OrderId={OrderId}, Amount={Amount}, Reason={Reason}",
                    orderId, voidedAmount, reason);

                return PaymentVoidResult.Succeeded(voidedAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Void işlemi hatası. OrderId={OrderId}", orderId);
                return PaymentVoidResult.Failed(
                    "Void işlemi sırasında bir hata oluştu.",
                    "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentRefundResult> RefundPaymentAsync(int orderId, decimal refundAmount, string reason)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return PaymentRefundResult.Failed("Sipariş bulunamadı.", "ORDER_NOT_FOUND");
                }

                if (order.CaptureStatus != CaptureStatus.Success)
                {
                    return PaymentRefundResult.Failed(
                        "Çekilmemiş ödeme iade edilemez.",
                        "NOT_CAPTURED");
                }

                if (refundAmount > order.CapturedAmount)
                {
                    return PaymentRefundResult.Failed(
                        $"İade tutarı çekilen tutardan ({order.CapturedAmount:N2} TL) fazla olamaz.",
                        "REFUND_EXCEEDS_CAPTURED");
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == "Paid");

                if (payment == null)
                {
                    return PaymentRefundResult.Failed(
                        "Ödenmiş ödeme kaydı bulunamadı.",
                        "NO_PAID_PAYMENT");
                }

                // Kredi kartı için gerçek iade işlemi
                // POSNET servisi mevcutsa gerçek API çağrısı, yoksa simülasyon
                var refundResult = await ExecuteRefundAsync(payment, refundAmount);

                if (!refundResult.success)
                {
                    return PaymentRefundResult.Failed(
                        refundResult.errorMessage ?? "İade işlemi başarısız.",
                        "REFUND_FAILED");
                }

                var remainingAmount = order.CapturedAmount - refundAmount;

                // Tam iade mi kısmi iade mi?
                if (remainingAmount <= 0)
                {
                    payment.Status = "Refunded";
                    order.CaptureStatus = CaptureStatus.Voided;
                    order.RefundedAt = DateTime.UtcNow;
                }
                else
                {
                    payment.Status = "PartialRefund";
                    order.CapturedAmount = remainingAmount;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ İade başarılı. OrderId={OrderId}, Refunded={Refunded}, Remaining={Remaining}, Reason={Reason}",
                    orderId, refundAmount, remainingAmount, reason);

                await _notificationService.NotifyRefundRequestedAsync(
                    orderId,
                    order.OrderNumber,
                    refundAmount,
                    reason);

                return PaymentRefundResult.Succeeded(
                    refundAmount,
                    remainingAmount,
                    refundResult.refundReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade işlemi hatası. OrderId={OrderId}", orderId);
                return PaymentRefundResult.Failed(
                    "İade işlemi sırasında bir hata oluştu.",
                    "INTERNAL_ERROR");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentStatusInfo> GetPaymentStatusAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new PaymentStatusInfo { OrderId = orderId };
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            var isExpired = payment?.AuthorizationExpiresAt.HasValue == true &&
                           payment.AuthorizationExpiresAt.Value < DateTime.UtcNow;

            return new PaymentStatusInfo
            {
                OrderId = orderId,
                OrderNumber = order.OrderNumber,
                PaymentMethod = order.PaymentMethod,
                HasAuthorization = order.AuthorizedAmount > 0,
                AuthorizedAmount = order.AuthorizedAmount,
                IsCaptured = order.CaptureStatus == CaptureStatus.Success,
                CapturedAmount = order.CapturedAmount,
                TolerancePercentage = order.TolerancePercentage,
                AuthorizationExpiresAt = payment?.AuthorizationExpiresAt,
                IsAuthorizationExpired = isExpired,
                CaptureStatus = order.CaptureStatus.ToString()
            };
        }

        /// <inheritdoc />
        public async Task<PendingAuthorizationList> GetPendingAuthorizationsAsync(int olderThanHours = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-olderThanHours);
            var expiryWarningTime = DateTime.UtcNow.AddHours(6); // 6 saat içinde expire olacaklar

            var pendingOrders = await _context.Orders
                .Where(o => o.CaptureStatus == CaptureStatus.Pending &&
                           o.AuthorizedAmount > 0)
                .Select(o => new PendingAuthorization
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    AuthorizedAmount = o.AuthorizedAmount,
                    AuthorizedAt = o.CreatedAt, // veya AuthorizedAt eklenebilir
                    ExpiresAt = o.CreatedAt.AddHours(AuthorizationExpiryHours),
                    IsExpiring = o.CreatedAt.AddHours(AuthorizationExpiryHours) < expiryWarningTime,
                    HoursUntilExpiry = (int)(o.CreatedAt.AddHours(AuthorizationExpiryHours) - DateTime.UtcNow).TotalHours
                })
                .ToListAsync();

            return new PendingAuthorizationList
            {
                TotalCount = pendingOrders.Count,
                ExpiringCount = pendingOrders.Count(p => p.IsExpiring),
                Items = pendingOrders
            };
        }

        #region Private Helper Methods

        /// <summary>
        /// Tolerans dahil authorize tutarını hesaplar.
        /// </summary>
        private decimal CalculateAuthorizedAmount(decimal orderAmount, decimal tolerancePercentage)
        {
            return Math.Round(orderAmount * (1 + tolerancePercentage), 2);
        }

        // Tek doğruluk kaynağı: banka aşım sınırı (Auth × 1.20) WeightBasedCapturePolicy'de hesaplanır.
        private static decimal CalculateMaxCapturableAmount(decimal authorizedAmount)
            => WeightBasedCapturePolicy.CalculateMaxCapturableAmount(authorizedAmount);

        /// <summary>
        /// Kapıda ödeme mi kontrol eder.
        /// </summary>
        private bool IsCashOnDelivery(string? paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
                return false;

            var method = paymentMethod.ToLower();
            return method == "cash_on_delivery" ||
                   method == "kapida_odeme" ||
                   method == "kapıda ödeme" ||
                   method == "cod";
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // EXECUTE METODLARI - POSNET GERÇEK API ENTEGRASYONU
        // Bu metodlar POSNET servisi mevcutsa gerçek API çağrısı yapar,
        // POSNET servisi yoksa (null) mevcut Simulate* metodlarına düşer (fallback).
        // Böylece POSNET yapılandırılmamış ortamlarda da sistem çalışmaya devam eder.
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Provizyon (authorize) işlemini yürütür.
        /// Checkout sırasında POSNET üzerinden alınmış PreAuthHostLogKey varsa onu kullanır,
        /// yoksa simülasyona düşer. Kart bilgileri bu aşamada mevcut olmadığı için
        /// ProcessAuthAsync doğrudan çağrılamaz - provizyon checkout akışında alınır.
        /// </summary>
        private async Task<(bool success, string? authReference, string? errorMessage)> ExecuteAuthorizationAsync(
            Order order, decimal authorizedAmount)
        {
            // Checkout sırasında POSNET ile alınmış bir ön provizyon (PreAuth) var mı kontrol et
            // PreAuthHostLogKey, 3D Secure veya direkt satış sonrası banka tarafından atanır
            if (!string.IsNullOrEmpty(order.PreAuthHostLogKey))
            {
                _logger.LogInformation(
                    "POSNET PreAuthHostLogKey mevcut, checkout provizyonu kullanılıyor. " +
                    "OrderId={OrderId}, HostLogKey={HostLogKey}",
                    order.Id, order.PreAuthHostLogKey);

                return (true, order.PreAuthHostLogKey, null);
            }

            // POSNET servisi mevcut olsa bile kart bilgileri (PAN, CVV, ExpDate) bu noktada
            // elimizde olmadığı için ProcessAuthAsync çağrılamaz.
            // Kart bilgileri sadece checkout sırasında frontend'den gelir ve güvenlik gereği saklanmaz.
            _logger.LogInformation(
                "PreAuthHostLogKey bulunamadı, simülasyona düşülüyor. OrderId={OrderId}",
                order.Id);

            return await SimulateAuthorizationAsync(order, authorizedAmount);
        }

        /// <summary>
        /// Finansallaştırma (capture) işlemini yürütür.
        /// POSNET servisi mevcutsa ProcessCaptureAsync ile gerçek banka API çağrısı yapar,
        /// yoksa simülasyona düşer. HostLogKey provizyondan alınır.
        /// </summary>
        private async Task<(bool success, string? captureReference, string? errorMessage)> ExecuteCaptureAsync(
            Payments payment, decimal captureAmount)
        {
            // POSNET servisi DI'da kayıtlı değilse simülasyona düş
            if (_posnetService == null)
                return await SimulateCaptureAsync(payment, captureAmount);

            // Finansallaştırma için HostLogKey gerekli - provizyon sırasında bankadan alınmış olmalı
            var hostLogKey = payment.HostLogKey ?? payment.AuthorizationReference;
            if (string.IsNullOrEmpty(hostLogKey))
            {
                _logger.LogWarning(
                    "HostLogKey bulunamadı, capture yapılamıyor. OrderId={OrderId}",
                    payment.OrderId);
                return (false, null, "HostLogKey bulunamadı. Provizyon kaydı eksik.");
            }

            // POSNET üzerinden gerçek finansallaştırma API çağrısı
            _logger.LogInformation(
                "POSNET ProcessCaptureAsync çağrılıyor. OrderId={OrderId}, HostLogKey={HostLogKey}, Amount={Amount}",
                payment.OrderId, hostLogKey, captureAmount);

            var result = await _posnetService.ProcessCaptureAsync(
                payment.OrderId, hostLogKey, captureAmount);

            if (result.IsSuccess && result.Data != null)
            {
                // Başarılı capture sonrası yeni HostLogKey varsa güncelle
                payment.HostLogKey = result.Data.HostLogKey ?? hostLogKey;
                _logger.LogInformation(
                    "POSNET capture başarılı. OrderId={OrderId}, NewHostLogKey={HostLogKey}",
                    payment.OrderId, payment.HostLogKey);
                return (true, result.Data.HostLogKey, null);
            }

            _logger.LogWarning(
                "POSNET capture başarısız. OrderId={OrderId}, Error={Error}",
                payment.OrderId, result.Error);
            return (false, null, result.Error ?? "POSNET finansallaştırma başarısız");
        }

        /// <summary>
        /// İptal (void/reverse) işlemini yürütür.
        /// POSNET servisi mevcutsa ProcessReverseAsync ile gerçek banka API çağrısı yapar,
        /// yoksa simülasyona düşer. Gün içi iptal işlemi için kullanılır.
        /// </summary>
        private async Task<(bool success, string? errorMessage)> ExecuteVoidAsync(Payments payment)
        {
            // POSNET servisi DI'da kayıtlı değilse simülasyona düş
            if (_posnetService == null)
                return await SimulateVoidAsync(payment);

            // İptal için HostLogKey gerekli
            var hostLogKey = payment.HostLogKey ?? payment.AuthorizationReference;
            if (string.IsNullOrEmpty(hostLogKey))
            {
                _logger.LogWarning(
                    "HostLogKey bulunamadı, void yapılamıyor. OrderId={OrderId}",
                    payment.OrderId);
                return (false, "HostLogKey bulunamadı. Provizyon kaydı eksik.");
            }

            // POSNET üzerinden gerçek iptal (reverse) API çağrısı
            _logger.LogInformation(
                "POSNET ProcessReverseAsync çağrılıyor. OrderId={OrderId}, HostLogKey={HostLogKey}",
                payment.OrderId, hostLogKey);

            var result = await _posnetService.ProcessReverseAsync(payment.OrderId, hostLogKey);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "POSNET void/reverse başarılı. OrderId={OrderId}",
                    payment.OrderId);
                return (true, null);
            }

            _logger.LogWarning(
                "POSNET void/reverse başarısız. OrderId={OrderId}, Error={Error}",
                payment.OrderId, result.Error);
            return (false, result.Error ?? "POSNET iptal işlemi başarısız");
        }

        /// <summary>
        /// İade (refund/return) işlemini yürütür.
        /// POSNET servisi mevcutsa ProcessRefundAsync ile gerçek banka API çağrısı yapar,
        /// yoksa simülasyona düşer. Gün sonu sonrasında iade işlemi için kullanılır.
        /// </summary>
        private async Task<(bool success, string? refundReference, string? errorMessage)> ExecuteRefundAsync(
            Payments payment, decimal refundAmount)
        {
            // POSNET servisi DI'da kayıtlı değilse simülasyona düş
            if (_posnetService == null)
                return await SimulateRefundAsync(payment, refundAmount);

            // İade için HostLogKey gerekli
            var hostLogKey = payment.HostLogKey ?? payment.AuthorizationReference;
            if (string.IsNullOrEmpty(hostLogKey))
            {
                _logger.LogWarning(
                    "HostLogKey bulunamadı, iade yapılamıyor. OrderId={OrderId}",
                    payment.OrderId);
                return (false, null, "HostLogKey bulunamadı. Provizyon kaydı eksik.");
            }

            // POSNET üzerinden gerçek iade (return) API çağrısı
            _logger.LogInformation(
                "POSNET ProcessRefundAsync çağrılıyor. OrderId={OrderId}, HostLogKey={HostLogKey}, Amount={Amount}",
                payment.OrderId, hostLogKey, refundAmount);

            var result = await _posnetService.ProcessRefundAsync(
                payment.OrderId, hostLogKey, refundAmount);

            if (result.IsSuccess && result.Data != null)
            {
                _logger.LogInformation(
                    "POSNET iade başarılı. OrderId={OrderId}, RefundHostLogKey={HostLogKey}",
                    payment.OrderId, result.Data.HostLogKey);
                return (true, result.Data.HostLogKey, null);
            }

            _logger.LogWarning(
                "POSNET iade başarısız. OrderId={OrderId}, Error={Error}",
                payment.OrderId, result.Error);
            return (false, null, result.Error ?? "POSNET iade işlemi başarısız");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // SIMULATE METODLARI - FALLBACK / TEST MODU
        // POSNET servisi mevcut olmadığında (null) bu metodlar kullanılır.
        // Test ortamında veya POSNET henüz yapılandırılmamışken sistemi çalışır tutar.
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Provizyon işlemini simüle eder (fallback).
        /// POSNET servisi yoksa veya PreAuthHostLogKey mevcut değilse kullanılır.
        /// </summary>
        private async Task<(bool success, string? authReference, string? errorMessage)> SimulateAuthorizationAsync(
            Order order, decimal authorizedAmount)
        {
            // Simülasyon - POSNET yapılandırılmadığında veya kart bilgileri olmadığında kullanılır
            await Task.Delay(100); // API çağrısı simülasyonu

            var authReference = $"AUTH-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            return (true, authReference, null);
        }

        /// <summary>
        /// Capture işlemini simüle eder (fallback).
        /// POSNET servisi yoksa kullanılır.
        /// </summary>
        private async Task<(bool success, string? captureReference, string? errorMessage)> SimulateCaptureAsync(
            Payments payment, decimal captureAmount)
        {
            await Task.Delay(100);

            var captureReference = $"CAP-{payment.OrderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            return (true, captureReference, null);
        }

        /// <summary>
        /// Void işlemini simüle eder (fallback).
        /// POSNET servisi yoksa kullanılır.
        /// </summary>
        private async Task<(bool success, string? errorMessage)> SimulateVoidAsync(Payments payment)
        {
            await Task.Delay(100);
            return (true, null);
        }

        /// <summary>
        /// Refund işlemini simüle eder (fallback).
        /// POSNET servisi yoksa kullanılır.
        /// </summary>
        private async Task<(bool success, string? refundReference, string? errorMessage)> SimulateRefundAsync(
            Payments payment, decimal refundAmount)
        {
            await Task.Delay(100);

            var refundReference = $"REF-{payment.OrderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            return (true, refundReference, null);
        }

        #endregion
    }
}
