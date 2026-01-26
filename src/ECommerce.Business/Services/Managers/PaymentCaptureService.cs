// ==========================================================================
// PaymentCaptureService.cs - Ödeme Provizyon/Capture Servisi
// ==========================================================================
// Authorize → Capture akışını yöneten servis implementasyonu.
// %10 tolerans ile provizyon alır, teslim anında final tutarı çeker.
// POSNET, Iyzico ve diğer ödeme sağlayıcılarını destekler.
// ==========================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

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

        // Varsayılan tolerans yüzdesi
        private const decimal DefaultTolerancePercentage = 0.10m;
        
        // Provizyon geçerlilik süresi (saat)
        private const int AuthorizationExpiryHours = 48;

        public PaymentCaptureService(
            ECommerceDbContext context,
            IRealTimeNotificationService notificationService,
            ILogger<PaymentCaptureService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                // TODO: Gerçek ödeme sağlayıcı (POSNET, Iyzico) ile entegrasyon
                // Şimdilik simüle ediyoruz
                var authResult = await SimulateAuthorizationAsync(order, authorizedAmount);
                
                if (!authResult.success)
                {
                    _logger.LogWarning(
                        "💳 Provizyon alınamadı. OrderId={OrderId}, Error={Error}",
                        orderId, authResult.errorMessage);
                    
                    return PaymentAuthorizationResult.Failed(
                        authResult.errorMessage ?? "Provizyon alınamadı.",
                        "AUTHORIZATION_FAILED");
                }

                // Siparişi güncelle
                order.AuthorizedAmount = authorizedAmount;
                order.TolerancePercentage = tolerancePercentage;
                order.CaptureStatus = CaptureStatus.Pending;

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

                // Provizyon kontrolü
                if (order.AuthorizedAmount == 0)
                {
                    return PaymentCaptureResult.Failed(
                        "Bu sipariş için provizyon bulunmuyor.",
                        "NO_AUTHORIZATION");
                }

                // Zaten capture edilmiş mi?
                if (order.CaptureStatus == CaptureStatus.Success)
                {
                    return PaymentCaptureResult.Failed(
                        "Bu sipariş için ödeme zaten çekilmiş.",
                        "ALREADY_CAPTURED");
                }

                // Final tutar kontrolü
                if (finalAmount > order.AuthorizedAmount)
                {
                    _logger.LogWarning(
                        "⚠️ Final tutar authorize edilen tutarı aşıyor. " +
                        "OrderId={OrderId}, FinalAmount={FinalAmount}, AuthorizedAmount={AuthorizedAmount}",
                        orderId, finalAmount, order.AuthorizedAmount);

                    // Sipariş durumunu güncelle - admin müdahalesi gerekli
                    order.CaptureStatus = CaptureStatus.Failed;
                    order.Status = OrderStatus.DeliveryPaymentPending;
                    order.DeliveryProblemReason = $"Final tutar ({finalAmount:N2} TL) authorize edilen tutarı ({order.AuthorizedAmount:N2} TL) aşıyor.";
                    
                    await _context.SaveChangesAsync();

                    // Admin'e bildirim gönder
                    await _notificationService.NotifyPaymentFailedAsync(
                        orderId,
                        order.OrderNumber,
                        $"Final tutar authorize tutarını aşıyor. Fark: {(finalAmount - order.AuthorizedAmount):N2} TL",
                        "Internal");

                    return PaymentCaptureResult.ExceededAuth(finalAmount, order.AuthorizedAmount);
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

                // TODO: Gerçek ödeme sağlayıcı capture işlemi
                var captureResult = await SimulateCaptureAsync(payment, finalAmount);
                
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
                    // TODO: Gerçek void işlemi
                    var voidResult = await SimulateVoidAsync(payment);
                    
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

                // TODO: Gerçek refund işlemi
                var refundResult = await SimulateRefundAsync(payment, refundAmount);
                
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

        /// <summary>
        /// Provizyon işlemini simüle eder.
        /// TODO: Gerçek ödeme sağlayıcı entegrasyonu
        /// </summary>
        private async Task<(bool success, string? authReference, string? errorMessage)> SimulateAuthorizationAsync(
            Order order, decimal authorizedAmount)
        {
            // Simülasyon - gerçek implementasyonda POSNET/Iyzico çağrılacak
            await Task.Delay(100); // API çağrısı simülasyonu
            
            var authReference = $"AUTH-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            return (true, authReference, null);
        }

        /// <summary>
        /// Capture işlemini simüle eder.
        /// TODO: Gerçek ödeme sağlayıcı entegrasyonu
        /// </summary>
        private async Task<(bool success, string? captureReference, string? errorMessage)> SimulateCaptureAsync(
            Payments payment, decimal captureAmount)
        {
            await Task.Delay(100);
            
            var captureReference = $"CAP-{payment.OrderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            return (true, captureReference, null);
        }

        /// <summary>
        /// Void işlemini simüle eder.
        /// </summary>
        private async Task<(bool success, string? errorMessage)> SimulateVoidAsync(Payments payment)
        {
            await Task.Delay(100);
            return (true, null);
        }

        /// <summary>
        /// Refund işlemini simüle eder.
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
