// ==========================================================================
// PosnetRefundBankErrorHelper.cs - İade/iptal banka hata sınıflandırıcısı
// ==========================================================================
// POSNET respCode değerlerini iş kurallarına map eder (0211, 0218, 0220, 0411…).
// PaymentManager ve RefundManager aynı sınıflandırmayı kullanır — tek doğruluk kaynağı.
// ==========================================================================

using System;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET iade/iptal hata kodları için ortak yardımcı.
    /// </summary>
    public static class PosnetRefundBankErrorHelper
    {
        /// <summary>
        /// RefundFailureReason alanına yazılacak standart metin.
        /// </summary>
        public static string FormatFailureReason(
            string? rawResponseCode,
            string? responseText,
            string? fallback = null)
        {
            var code = NormalizeResponseCode(rawResponseCode);
            var text = string.IsNullOrWhiteSpace(responseText)
                ? fallback ?? "POSNET iade işlemi başarısız oldu."
                : responseText.Trim();

            return string.IsNullOrWhiteSpace(code) ? text : $"{code} - {text}";
        }

        public static string? NormalizeResponseCode(string? rawCode)
        {
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return null;
            }

            var trimmed = rawCode.Trim();
            return trimmed.Length <= 4 ? trimmed.PadLeft(4, '0') : trimmed;
        }

        public static PosnetErrorCode ResolveErrorCode(string? rawCode, string? errorText)
        {
            var parsed = PosnetErrorCodeExtensions.ParseFromString(rawCode);
            if (parsed != PosnetErrorCode.Unknown)
            {
                return parsed;
            }

            if (ContainsCode(errorText, "0211")) return PosnetErrorCode.GroupClosedUseRefund;
            if (ContainsCode(errorText, "0229")) return PosnetErrorCode.PreviousDayCaptureUseRefund;
            if (ContainsCode(errorText, "0220")) return PosnetErrorCode.AlreadyReversed;
            if (ContainsCode(errorText, "0218")) return PosnetErrorCode.CannotReverseAfterRefund;
            if (ContainsCode(errorText, "0411")) return PosnetErrorCode.NotYetFinanciallySettled;
            if (ContainsCode(errorText, "0450")) return PosnetErrorCode.RefundNotAllowed;
            if (ContainsCode(errorText, "0123")) return PosnetErrorCode.OriginalTransactionNotFound;
            if (ContainsCode(errorText, "0091")) return PosnetErrorCode.BankTimeout091;

            return PosnetErrorCode.Unknown;
        }

        public static bool IsGroupClosedError(PosnetErrorCode code, string? errorText) =>
            code == PosnetErrorCode.GroupClosedUseRefund ||
            code == PosnetErrorCode.PreviousDayCaptureUseRefund ||
            ContainsCode(errorText, "0211") ||
            ContainsCode(errorText, "0229");

        public static bool IsNotYetSettledError(PosnetErrorCode code, string? errorText) =>
            code == PosnetErrorCode.NotYetFinanciallySettled ||
            ContainsCode(errorText, "0411") ||
            ContainsCode(errorText, "FINANSALLASMAMIS");

        public static bool IsAlreadyReversedError(PosnetErrorCode code, string? errorText) =>
            code == PosnetErrorCode.AlreadyReversed ||
            ContainsCode(errorText, "0220") ||
            ContainsCode(errorText, "0370");

        public static bool IsCannotReverseAfterRefundError(PosnetErrorCode code, string? errorText) =>
            code == PosnetErrorCode.CannotReverseAfterRefund ||
            ContainsCode(errorText, "0218");

        /// <summary>
        /// Banka timeout / geçici hatalar — otomatik retry için.
        /// </summary>
        public static bool IsTransientRetryableError(PosnetErrorCode code, string? errorText) =>
            code.IsRetryable() ||
            code == PosnetErrorCode.BankTimeout091 ||
            ContainsCode(errorText, "0091") ||
            ContainsCode(errorText, "0400");

        private static bool ContainsCode(string? text, string code) =>
            !string.IsNullOrWhiteSpace(text) &&
            text.Contains(code, StringComparison.OrdinalIgnoreCase);
    }
}
