// ==========================================================================
// PosnetRefundExecutionDtos.cs - POSNET iade/iptal yürütme sözleşmesi
// ==========================================================================
// Tüm iade giriş noktalarının (RefundManager) tek bir ödeme metoduna
// bağlanması için kullanılır. reverse/return kararı PaymentManager'da kalır.
// ==========================================================================

namespace ECommerce.Core.DTOs.Payment
{
    /// <summary>
    /// POSNET kart iadesi/iptali için birleşik yürütme isteği.
    /// RefundManager iş kurallarını hesaplar; PaymentManager banka operasyonunu seçer.
    /// </summary>
    public class PosnetRefundExecutionRequest
    {
        public int OrderId { get; set; }

        /// <summary>Orijinal sale/capt ödeme kaydı ID'si (MADDE 10).</summary>
        public int PaymentId { get; set; }

        public decimal RefundAmount { get; set; }

        /// <summary>Sipariş bazlı iade edilebilir üst limit (CapturedAmount öncelikli).</summary>
        public decimal MaxRefundableAmount { get; set; }

        /// <summary>Provizyon aşamasında mı? true ise reverse(auth) uygulanır.</summary>
        public bool IsAuthOnly { get; set; }

        public string? PreAuthHostLogKey { get; set; }

        /// <summary>Audit log ve banka reason alanı için açıklama.</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// POSNET iade/iptal yürütme sonucu — banka respCode dahil teşhis bilgisi taşır.
    /// </summary>
    public class PosnetRefundExecutionResult
    {
        public bool Success { get; set; }

        /// <summary>reverse | return | none</summary>
        public string TransactionType { get; set; } = "none";

        public string? HostLogKey { get; set; }

        public string? FailureReason { get; set; }

        public string? BankResponseCode { get; set; }

        public string? BankResponseText { get; set; }

        public static PosnetRefundExecutionResult Ok(string transactionType, string? hostLogKey)
        {
            return new PosnetRefundExecutionResult
            {
                Success = true,
                TransactionType = transactionType,
                HostLogKey = hostLogKey
            };
        }

        public static PosnetRefundExecutionResult Fail(
            string reason,
            string transactionType = "none",
            string? bankResponseCode = null,
            string? bankResponseText = null)
        {
            return new PosnetRefundExecutionResult
            {
                Success = false,
                TransactionType = transactionType,
                FailureReason = reason,
                BankResponseCode = bankResponseCode,
                BankResponseText = bankResponseText
            };
        }
    }
}
