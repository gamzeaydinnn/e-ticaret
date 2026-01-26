// ==========================================================================
// IPaymentCaptureService.cs - Ödeme Provizyon/Capture Interface
// ==========================================================================
// Authorize → Capture akışını yöneten servis interface'i.
// %10 tolerans ile provizyon alır, teslim anında final tutarı çeker.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Ödeme Authorize/Capture akışını yöneten servis interface'i.
    /// </summary>
    public interface IPaymentCaptureService
    {
        /// <summary>
        /// Sipariş için provizyon (authorize) işlemi yapar.
        /// Sipariş tutarının %10 tolerans fazlası kadar provizyon alır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderAmount">Sipariş tutarı (TL)</param>
        /// <param name="tolerancePercentage">Tolerans yüzdesi (varsayılan 0.10)</param>
        /// <returns>Provizyon sonucu</returns>
        Task<PaymentAuthorizationResult> AuthorizePaymentAsync(int orderId, decimal orderAmount, 
            decimal tolerancePercentage = 0.10m);

        /// <summary>
        /// Teslim anında ödemeyi çeker (capture).
        /// Authorize edilen tutardan final tutarı çeker.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="finalAmount">Final tutar (teslim anındaki gerçek tutar)</param>
        /// <returns>Çekim sonucu</returns>
        Task<PaymentCaptureResult> CapturePaymentAsync(int orderId, decimal finalAmount);

        /// <summary>
        /// Provizyonu iptal eder (void).
        /// Sipariş iptal edildiğinde kullanılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="reason">İptal sebebi</param>
        /// <returns>İptal sonucu</returns>
        Task<PaymentVoidResult> VoidAuthorizationAsync(int orderId, string reason);

        /// <summary>
        /// Kısmi iade yapar (partial refund).
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="refundAmount">İade tutarı</param>
        /// <param name="reason">İade sebebi</param>
        /// <returns>İade sonucu</returns>
        Task<PaymentRefundResult> RefundPaymentAsync(int orderId, decimal refundAmount, string reason);

        /// <summary>
        /// Siparişin ödeme durumunu kontrol eder.
        /// Provizyon durumu, capture durumu, tolerans bilgisi döner.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Ödeme durum bilgisi</returns>
        Task<PaymentStatusInfo> GetPaymentStatusAsync(int orderId);

        /// <summary>
        /// Authorize edilmiş ama capture edilmemiş ödemeleri listeler.
        /// Timeout kontrolü için kullanılır.
        /// </summary>
        /// <param name="olderThanHours">Belirtilen saatten eski kayıtlar</param>
        /// <returns>Bekleyen provizyon listesi</returns>
        Task<PendingAuthorizationList> GetPendingAuthorizationsAsync(int olderThanHours = 24);
    }

    #region Result DTOs

    /// <summary>
    /// Provizyon (authorize) sonucu.
    /// </summary>
    public class PaymentAuthorizationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        
        /// <summary>
        /// Authorize edilen tutar (tolerans dahil).
        /// </summary>
        public decimal AuthorizedAmount { get; set; }
        
        /// <summary>
        /// Orijinal sipariş tutarı.
        /// </summary>
        public decimal OrderAmount { get; set; }
        
        /// <summary>
        /// Uygulanan tolerans yüzdesi.
        /// </summary>
        public decimal TolerancePercentage { get; set; }
        
        /// <summary>
        /// Provizyon referans ID'si (ödeme sağlayıcıdan).
        /// </summary>
        public string? AuthorizationReference { get; set; }
        
        /// <summary>
        /// Provizyon geçerlilik süresi.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        public static PaymentAuthorizationResult Succeeded(decimal authorizedAmount, decimal orderAmount, 
            decimal tolerancePercentage, string? authRef = null, DateTime? expiresAt = null)
        {
            return new PaymentAuthorizationResult
            {
                Success = true,
                AuthorizedAmount = authorizedAmount,
                OrderAmount = orderAmount,
                TolerancePercentage = tolerancePercentage,
                AuthorizationReference = authRef,
                ExpiresAt = expiresAt,
                Message = "Provizyon başarıyla alındı."
            };
        }

        public static PaymentAuthorizationResult Failed(string message, string? errorCode = null)
        {
            return new PaymentAuthorizationResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Capture (çekim) sonucu.
    /// </summary>
    public class PaymentCaptureResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        
        /// <summary>
        /// Çekilen tutar.
        /// </summary>
        public decimal CapturedAmount { get; set; }
        
        /// <summary>
        /// İade edilen tutar (authorize - capture farkı).
        /// </summary>
        public decimal ReleasedAmount { get; set; }
        
        /// <summary>
        /// Capture işlem referansı.
        /// </summary>
        public string? CaptureReference { get; set; }
        
        /// <summary>
        /// İşlem zamanı.
        /// </summary>
        public DateTime CapturedAt { get; set; }

        /// <summary>
        /// Final tutar authorize edilen tutarı aştı mı?
        /// Bu durumda ek işlem gerekebilir.
        /// </summary>
        public bool ExceededAuthorization { get; set; }

        public static PaymentCaptureResult Succeeded(decimal capturedAmount, decimal releasedAmount, 
            string? captureRef = null)
        {
            return new PaymentCaptureResult
            {
                Success = true,
                CapturedAmount = capturedAmount,
                ReleasedAmount = releasedAmount,
                CaptureReference = captureRef,
                CapturedAt = DateTime.UtcNow,
                Message = "Ödeme başarıyla çekildi."
            };
        }

        public static PaymentCaptureResult ExceededAuth(decimal capturedAmount, decimal authorizedAmount)
        {
            return new PaymentCaptureResult
            {
                Success = false,
                ExceededAuthorization = true,
                CapturedAmount = capturedAmount,
                ErrorCode = "EXCEEDED_AUTHORIZATION",
                Message = $"Final tutar ({capturedAmount:N2} TL) authorize edilen tutarı ({authorizedAmount:N2} TL) aşıyor."
            };
        }

        public static PaymentCaptureResult Failed(string message, string? errorCode = null)
        {
            return new PaymentCaptureResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Void (provizyon iptal) sonucu.
    /// </summary>
    public class PaymentVoidResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        
        /// <summary>
        /// İptal edilen tutar.
        /// </summary>
        public decimal VoidedAmount { get; set; }
        
        /// <summary>
        /// Void referans ID'si.
        /// </summary>
        public string? VoidReference { get; set; }

        public static PaymentVoidResult Succeeded(decimal voidedAmount, string? voidRef = null)
        {
            return new PaymentVoidResult
            {
                Success = true,
                VoidedAmount = voidedAmount,
                VoidReference = voidRef,
                Message = "Provizyon başarıyla iptal edildi."
            };
        }

        public static PaymentVoidResult Failed(string message, string? errorCode = null)
        {
            return new PaymentVoidResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Refund (iade) sonucu.
    /// </summary>
    public class PaymentRefundResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        
        /// <summary>
        /// İade edilen tutar.
        /// </summary>
        public decimal RefundedAmount { get; set; }
        
        /// <summary>
        /// Kalan tutar (kısmi iade sonrası).
        /// </summary>
        public decimal RemainingAmount { get; set; }
        
        /// <summary>
        /// Refund referans ID'si.
        /// </summary>
        public string? RefundReference { get; set; }

        public static PaymentRefundResult Succeeded(decimal refundedAmount, decimal remainingAmount, 
            string? refundRef = null)
        {
            return new PaymentRefundResult
            {
                Success = true,
                RefundedAmount = refundedAmount,
                RemainingAmount = remainingAmount,
                RefundReference = refundRef,
                Message = "İade başarıyla gerçekleştirildi."
            };
        }

        public static PaymentRefundResult Failed(string message, string? errorCode = null)
        {
            return new PaymentRefundResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Ödeme durum bilgisi.
    /// </summary>
    public class PaymentStatusInfo
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// Ödeme yöntemi (credit_card, cash_on_delivery, etc.).
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Provizyon var mı?
        /// </summary>
        public bool HasAuthorization { get; set; }
        
        /// <summary>
        /// Authorize edilen tutar.
        /// </summary>
        public decimal AuthorizedAmount { get; set; }
        
        /// <summary>
        /// Capture edildi mi?
        /// </summary>
        public bool IsCaptured { get; set; }
        
        /// <summary>
        /// Capture edilen tutar.
        /// </summary>
        public decimal CapturedAmount { get; set; }
        
        /// <summary>
        /// Tolerans yüzdesi.
        /// </summary>
        public decimal TolerancePercentage { get; set; }
        
        /// <summary>
        /// Provizyon geçerlilik süresi.
        /// </summary>
        public DateTime? AuthorizationExpiresAt { get; set; }
        
        /// <summary>
        /// Provizyon süresi doldu mu?
        /// </summary>
        public bool IsAuthorizationExpired { get; set; }
        
        /// <summary>
        /// Capture durumu.
        /// </summary>
        public string CaptureStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bekleyen provizyon listesi.
    /// </summary>
    public class PendingAuthorizationList
    {
        public int TotalCount { get; set; }
        public int ExpiringCount { get; set; }
        public System.Collections.Generic.List<PendingAuthorization> Items { get; set; } = new();
    }

    /// <summary>
    /// Bekleyen provizyon kaydı.
    /// </summary>
    public class PendingAuthorization
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal AuthorizedAmount { get; set; }
        public DateTime AuthorizedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpiring { get; set; }
        public int HoursUntilExpiry { get; set; }
    }

    #endregion
}
