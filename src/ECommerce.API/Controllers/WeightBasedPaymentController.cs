/*
 * ═══════════════════════════════════════════════════════════════════════════════════════════════
 * WEIGHT BASED PAYMENT CONTROLLER
 * Ağırlık Bazlı Dinamik Ödeme Sistemi - Ödeme İşlemleri API
 * ═══════════════════════════════════════════════════════════════════════════════════════════════
 * 
 * Bu controller, ağırlık bazlı ürünler için ödeme işlemlerini yönetir.
 * Pre-Authorization, Post-Authorization ve kısmi iade işlemlerini gerçekleştirir.
 * 
 * ENDPOINT'LER:
 * 
 * Kurye İşlevleri:
 * - POST /api/weight-payment/orders/{orderId}/finalize-delivery → Teslimat tamamla + ödeme kesinleştir
 * - POST /api/weight-payment/orders/{orderId}/cash-difference → Nakit fark tahsilatı
 * - GET  /api/weight-payment/orders/{orderId}/payment-status → Ödeme durumu sorgula
 * 
 * Ödeme İşlemleri:
 * - POST /api/weight-payment/orders/{orderId}/pre-auth → Pre-Authorization başlat
 * - POST /api/weight-payment/orders/{orderId}/post-auth → Post-Authorization (kesin çekim)
 * - POST /api/weight-payment/orders/{orderId}/partial-refund → Kısmi iade
 * 
 * Admin İşlevleri:
 * - POST /api/weight-payment/admin/cancel-expired → Süresi dolan provizyonları iptal et
 * - GET  /api/weight-payment/orders/{orderId}/audit-trail → Ödeme işlem geçmişi
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ağırlık Bazlı Ödeme Controller'ı
    /// Pre-Auth, Post-Auth, Refund işlemlerini yönetir
    /// </summary>
    [ApiController]
    [Route("api/weight-payment")]
    [Authorize]
    public class WeightBasedPaymentController : ControllerBase
    {
        // ═══════════════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════════════

        private readonly IWeightBasedPaymentService _paymentService;
        private readonly IWeightAdjustmentService _adjustmentService;
        private readonly IOrderService _orderService;
        private readonly ICourierService _courierService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<WeightBasedPaymentController>? _logger;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════

        public WeightBasedPaymentController(
            IWeightBasedPaymentService paymentService,
            IWeightAdjustmentService adjustmentService,
            IOrderService orderService,
            ICourierService courierService,
            UserManager<User> userManager,
            ILogger<WeightBasedPaymentController>? logger = null)
        {
            _paymentService = paymentService;
            _adjustmentService = adjustmentService;
            _orderService = orderService;
            _courierService = courierService;
            _userManager = userManager;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KURYE İŞLEVLERİ
        // Kurye teslimat sırasında kullanacağı endpoint'ler
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Teslimatı tamamla ve ödeme farkını kesinleştir
        /// 
        /// Bu endpoint, kurye "Teslim Edildi" butonuna bastığında çağrılır.
        /// 
        /// KART ÖDEMELERİ İÇİN:
        /// - Pre-Auth tutarı serbest bırakılır
        /// - Gerçek tutar (ActualPrice toplamı) karttan çekilir
        /// - Fark varsa kısmi iade yapılır
        /// 
        /// NAKİT ÖDEMELERİ İÇİN:
        /// - Fark hesaplanır ve kurye bilgilendirilir
        /// - Kurye farkı tahsil ettikten sonra cash-difference endpoint'i çağırır
        /// </summary>
        [HttpPost("orders/{orderId}/finalize-delivery")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> FinalizeDeliveryAndPayment(
            int orderId, 
            [FromBody] FinalizeDeliveryPaymentRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Kurye bilgisini al
                var courierId = await GetCurrentCourierIdAsync();
                if (courierId == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Teslimat finalize başlatılıyor. OrderId: {OrderId}, CourierId: {CourierId}",
                    orderId, courierId.Value);

                // Önce tüm ürünlerin tartılıp tartılmadığını kontrol et
                var summary = await _adjustmentService.GetOrderWeightSummaryAsync(orderId);
                if (summary == null)
                    return NotFound(new { success = false, message = "Sipariş bulunamadı" });

                if (!summary.AllItemsWeighed)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Tüm ağırlık bazlı ürünler tartılmadan teslimat tamamlanamaz",
                        pendingItems = summary.WeightBasedItemCount - summary.WeighedItemCount
                    });
                }

                // Ödeme durumunu kontrol et
                var paymentStatus = await _paymentService.GetPaymentStatusAsync(orderId, cancellationToken);

                // Kart ödemesi ise Post-Authorization yap
                if (paymentStatus.IsCardPayment)
                {
                    // Provizyon hala geçerli mi?
                    if (paymentStatus.PreAuthorizationExpired)
                    {
                        return BadRequest(new {
                            success = false,
                            message = "Ön provizyon süresi dolmuş. Yeni ödeme alınması gerekiyor.",
                            preAuthDate = paymentStatus.PreAuthorizationDate,
                            requiresNewPayment = true
                        });
                    }

                    // Post-Authorization (kesin çekim)
                    var postAuthResult = await _paymentService.ProcessPostAuthorizationAsync(
                        orderId,
                        summary.ActualTotal,
                        paymentStatus.PreAuthorizationHostLogKey!,
                        cancellationToken);

                    if (!postAuthResult.IsSuccess)
                    {
                        _logger?.LogWarning(
                            "[WEIGHT-PAYMENT-API] Post-Auth başarısız. OrderId: {OrderId}, Error: {Error}",
                            orderId, postAuthResult.ErrorMessage);

                        return BadRequest(new {
                            success = false,
                            message = postAuthResult.ErrorMessage,
                            data = postAuthResult
                        });
                    }

                    // Fark varsa iade yap
                    if (postAuthResult.DifferenceAmount > 0)
                    {
                        var refundResult = await _paymentService.ProcessPartialRefundAsync(
                            orderId,
                            postAuthResult.DifferenceAmount,
                            paymentStatus.PreAuthorizationHostLogKey!,
                            "Ağırlık farkı iadesi",
                            cancellationToken);

                        _logger?.LogInformation(
                            "[WEIGHT-PAYMENT-API] Kısmi iade tamamlandı. OrderId: {OrderId}, RefundAmount: {Amount}",
                            orderId, refundResult.RefundedAmount);
                    }

                    // WeightAdjustmentService ile teslimatı tamamla
                    var finalizeResult = await _adjustmentService.FinalizeWeightBasedPaymentAsync(
                        orderId, courierId.Value, request?.CourierNotes);

                    _logger?.LogInformation(
                        "[WEIGHT-PAYMENT-API] Kart ödemeli teslimat tamamlandı. OrderId: {OrderId}, FinalAmount: {Amount}",
                        orderId, postAuthResult.CapturedAmount);

                    return Ok(new {
                        success = true,
                        message = "Teslimat ve ödeme başarıyla tamamlandı",
                        paymentType = "card",
                        data = new {
                            originalAmount = postAuthResult.OriginalBlockedAmount,
                            finalAmount = postAuthResult.CapturedAmount,
                            differenceAmount = postAuthResult.DifferenceAmount,
                            refunded = postAuthResult.RefundProcessed,
                            refundedAmount = postAuthResult.RefundedAmount
                        }
                    });
                }
                else
                {
                    // Nakit ödeme - fark hesabı yap
                    var cashDifference = await _paymentService.CalculateCashPaymentDifferenceAsync(
                        orderId,
                        summary.EstimatedTotal,
                        summary.ActualTotal,
                        20, // Admin onay eşiği %20
                        cancellationToken);

                    // Admin onayı gerekiyorsa beklet
                    if (cashDifference.RequiresAdminApproval)
                    {
                        await _adjustmentService.RequestAdminReviewAsync(orderId, 
                            $"Yüksek ağırlık farkı: %{cashDifference.DifferencePercent:F1}");

                        return Ok(new {
                            success = true,
                            message = "Yüksek fark tespit edildi. Admin onayı bekleniyor.",
                            requiresAdminApproval = true,
                            paymentType = "cash",
                            data = cashDifference
                        });
                    }

                    // Teslimatı tamamla
                    var finalizeResult = await _adjustmentService.FinalizeWeightBasedPaymentAsync(
                        orderId, courierId.Value, request?.CourierNotes);

                    _logger?.LogInformation(
                        "[WEIGHT-PAYMENT-API] Nakit ödemeli teslimat - fark hesaplandı. OrderId: {OrderId}, Diff: {Diff}",
                        orderId, cashDifference.DifferenceAmount);

                    return Ok(new {
                        success = true,
                        message = cashDifference.DifferenceDescription,
                        paymentType = "cash",
                        requiresCashSettlement = cashDifference.DifferenceAmount != 0,
                        data = cashDifference
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Teslimat finalize hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Bir hata oluştu", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Nakit ödeme fark tahsilatını kaydet
        /// 
        /// Kurye nakit farkı tahsil ettikten/verdikten sonra bu endpoint'i çağırır.
        /// </summary>
        [HttpPost("orders/{orderId}/cash-difference")]
        [Authorize(Roles = "Courier")]
        public async Task<IActionResult> RecordCashDifferenceCollection(
            int orderId,
            [FromBody] CashDifferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var courierId = await GetCurrentCourierIdAsync();
                if (courierId == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                var result = await _paymentService.CompleteCashPaymentDifferenceAsync(
                    orderId,
                    request.DifferenceAmount,
                    request.Direction,
                    request.CourierNotes,
                    cancellationToken);

                if (!result)
                    return BadRequest(new { success = false, message = "Nakit tahsilat kaydedilemedi" });

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Nakit fark kaydedildi. OrderId: {OrderId}, Amount: {Amount}, Direction: {Dir}",
                    orderId, request.DifferenceAmount, request.Direction);

                return Ok(new {
                    success = true,
                    message = "Nakit fark tahsilatı kaydedildi"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Nakit fark kayıt hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Sipariş ödeme durumunu sorgula
        /// </summary>
        [HttpGet("orders/{orderId}/payment-status")]
        [Authorize(Roles = "Courier,Admin,User")]
        public async Task<IActionResult> GetPaymentStatus(int orderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await _paymentService.GetPaymentStatusAsync(orderId, cancellationToken);
                return Ok(new { success = true, data = status });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Ödeme durumu sorgulama hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ÖDEME İŞLEMLERİ
        // Pre-Auth, Post-Auth ve Refund için endpoint'ler
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Pre-Authorization (Ön Provizyon) başlat
        /// 
        /// Sipariş oluşturulduğunda, tahmini tutar + güvenlik marjı karttan bloke edilir.
        /// Bu işlem checkout sırasında otomatik yapılabilir.
        /// </summary>
        [HttpPost("orders/{orderId}/pre-auth")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessPreAuthorization(
            int orderId,
            [FromBody] PreAuthRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _paymentService.ProcessPreAuthorizationAsync(
                    orderId,
                    request.EstimatedAmount,
                    request.SecurityMarginPercent,
                    request.ExistingHostLogKey,
                    cancellationToken);

                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage, data = result });

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Pre-Auth başarılı. OrderId: {OrderId}, BlockedAmount: {Amount}",
                    orderId, result.BlockedAmount);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Pre-Auth hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Post-Authorization (Kesin Çekim)
        /// 
        /// Tartım sonrası gerçek tutar üzerinden kesin çekim yapılır.
        /// </summary>
        [HttpPost("orders/{orderId}/post-auth")]
        [Authorize(Roles = "Admin,Courier")]
        public async Task<IActionResult> ProcessPostAuthorization(
            int orderId,
            [FromBody] PostAuthRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _paymentService.ProcessPostAuthorizationAsync(
                    orderId,
                    request.ActualAmount,
                    request.HostLogKey,
                    cancellationToken);

                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage, data = result });

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Post-Auth başarılı. OrderId: {OrderId}, CapturedAmount: {Amount}",
                    orderId, result.CapturedAmount);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Post-Auth hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kısmi İade
        /// 
        /// Müşteri lehine fark varsa kısmi iade yapılır.
        /// </summary>
        [HttpPost("orders/{orderId}/partial-refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessPartialRefund(
            int orderId,
            [FromBody] PartialRefundRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _paymentService.ProcessPartialRefundAsync(
                    orderId,
                    request.RefundAmount,
                    request.HostLogKey,
                    request.Reason,
                    cancellationToken);

                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage, data = result });

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Kısmi iade başarılı. OrderId: {OrderId}, RefundedAmount: {Amount}",
                    orderId, result.RefundedAmount);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Kısmi iade hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ADMİN İŞLEVLERİ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Süresi dolan provizyonları iptal et
        /// 
        /// Bu endpoint scheduled job veya admin tarafından manuel çağrılabilir.
        /// 48 saati geçen ve finalize edilmemiş provizyonlar iptal edilir.
        /// </summary>
        [HttpPost("admin/cancel-expired")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelExpiredPreAuthorizations(CancellationToken cancellationToken = default)
        {
            try
            {
                var cancelledCount = await _paymentService.CancelExpiredPreAuthorizationsAsync(cancellationToken);

                _logger?.LogInformation(
                    "[WEIGHT-PAYMENT-API] Süresi dolan provizyonlar iptal edildi. Count: {Count}",
                    cancelledCount);

                return Ok(new {
                    success = true,
                    message = $"{cancelledCount} adet provizyon iptal edildi",
                    cancelledCount
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Provizyon iptal hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Provizyon geçerliliğini kontrol et
        /// </summary>
        [HttpGet("orders/{orderId}/pre-auth-valid")]
        [Authorize(Roles = "Admin,Courier")]
        public async Task<IActionResult> CheckPreAuthorizationValidity(
            int orderId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isValid = await _paymentService.IsPreAuthorizationValidAsync(orderId, cancellationToken);
                var status = await _paymentService.GetPaymentStatusAsync(orderId, cancellationToken);

                return Ok(new {
                    success = true,
                    data = new {
                        orderId,
                        isValid,
                        preAuthDate = status.PreAuthorizationDate,
                        expiresAt = status.PreAuthorizationDate?.AddHours(48),
                        preAuthAmount = status.PreAuthorizationAmount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-PAYMENT-API] Provizyon geçerlilik kontrolü hatası. OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Mevcut kullanıcının kurye ID'sini alır
        /// </summary>
        private async Task<int?> GetCurrentCourierIdAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return null;

            var couriers = await _courierService.GetAllAsync();
            var courier = couriers.FirstOrDefault(c => c.UserId == userId);
            return courier?.Id;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // REQUEST DTO'LAR
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Teslimat tamamlama request
    /// </summary>
    public class FinalizeDeliveryPaymentRequest
    {
        /// <summary>Kurye notu</summary>
        public string? CourierNotes { get; set; }
    }

    /// <summary>
    /// Nakit fark tahsilatı request
    /// </summary>
    public class CashDifferenceRequest
    {
        /// <summary>Fark tutarı</summary>
        public decimal DifferenceAmount { get; set; }

        /// <summary>Fark yönü</summary>
        public PaymentDifferenceDirection Direction { get; set; }

        /// <summary>Kurye notu</summary>
        public string? CourierNotes { get; set; }
    }

    /// <summary>
    /// Pre-Auth request
    /// </summary>
    public class PreAuthRequest
    {
        /// <summary>Tahmini tutar</summary>
        public decimal EstimatedAmount { get; set; }

        /// <summary>Güvenlik marjı yüzdesi (varsayılan %15)</summary>
        public decimal SecurityMarginPercent { get; set; } = 15m;

        /// <summary>Mevcut HostLogKey (3D Secure sonrası)</summary>
        public string? ExistingHostLogKey { get; set; }
    }

    /// <summary>
    /// Post-Auth request
    /// </summary>
    public class PostAuthRequest
    {
        /// <summary>Gerçek tutar</summary>
        public decimal ActualAmount { get; set; }

        /// <summary>Pre-Auth HostLogKey</summary>
        public string HostLogKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kısmi iade request
    /// </summary>
    public class PartialRefundRequest
    {
        /// <summary>İade tutarı</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Orijinal işlem HostLogKey</summary>
        public string HostLogKey { get; set; } = string.Empty;

        /// <summary>İade nedeni</summary>
        public string? Reason { get; set; }
    }
}
