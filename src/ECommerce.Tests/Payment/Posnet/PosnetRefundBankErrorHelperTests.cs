// ==========================================================================
// PosnetRefundBankErrorHelperTests - Faz 3 banka hata sınıflandırma testleri
// ==========================================================================

using ECommerce.Infrastructure.Services.Payment.Posnet;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;

namespace ECommerce.Tests.Payment.Posnet
{
    public class PosnetRefundBankErrorHelperTests
    {
        [Theory]
        [InlineData("211", "0211")]
        [InlineData("0211", "0211")]
        [InlineData("41", "0041")]
        public void NormalizeResponseCode_PadsToFourDigits(string input, string expected)
        {
            var result = PosnetRefundBankErrorHelper.NormalizeResponseCode(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FormatFailureReason_IncludesBankCodeAndMessage()
        {
            var text = PosnetRefundBankErrorHelper.FormatFailureReason(
                "0211",
                "Grup kapalı, iade kullanın");

            Assert.Equal("0211 - Grup kapalı, iade kullanın", text);
        }

        [Theory]
        [InlineData("0211", null, true)]
        [InlineData(PosnetErrorCode.GroupClosedUseRefund, null, true)]
        [InlineData("0229", null, true)]
        [InlineData("0411", null, false)]
        public void IsGroupClosedError_DetectsSettlementCodes(
            object codeOrEnum,
            string? errorText,
            bool expected)
        {
            var code = codeOrEnum is PosnetErrorCode enumCode
                ? enumCode
                : PosnetRefundBankErrorHelper.ResolveErrorCode(codeOrEnum.ToString(), errorText);

            var result = PosnetRefundBankErrorHelper.IsGroupClosedError(code, errorText);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("0411", null, true)]
        [InlineData(PosnetErrorCode.NotYetFinanciallySettled, null, true)]
        [InlineData("0211", null, false)]
        public void IsNotYetSettledError_Detects0411(
            object codeOrEnum,
            string? errorText,
            bool expected)
        {
            var code = codeOrEnum is PosnetErrorCode enumCode
                ? enumCode
                : PosnetRefundBankErrorHelper.ResolveErrorCode(codeOrEnum.ToString(), errorText);

            Assert.Equal(expected, PosnetRefundBankErrorHelper.IsNotYetSettledError(code, errorText));
        }

        [Theory]
        [InlineData("0220", null, true)]
        [InlineData(PosnetErrorCode.AlreadyReversed, null, true)]
        [InlineData("0211", null, false)]
        public void IsAlreadyReversedError_Detects0220Idempotency(
            object codeOrEnum,
            string? errorText,
            bool expected)
        {
            var code = codeOrEnum is PosnetErrorCode enumCode
                ? enumCode
                : PosnetRefundBankErrorHelper.ResolveErrorCode(codeOrEnum.ToString(), errorText);

            Assert.Equal(expected, PosnetRefundBankErrorHelper.IsAlreadyReversedError(code, errorText));
        }

        [Theory]
        [InlineData("0091", null, true)]
        [InlineData(PosnetErrorCode.BankTimeout091, null, true)]
        [InlineData("0211", null, false)]
        public void IsTransientRetryableError_DetectsRetryableCodes(
            object codeOrEnum,
            string? errorText,
            bool expected)
        {
            var code = codeOrEnum is PosnetErrorCode enumCode
                ? enumCode
                : PosnetRefundBankErrorHelper.ResolveErrorCode(codeOrEnum.ToString(), errorText);

            Assert.Equal(expected, PosnetRefundBankErrorHelper.IsTransientRetryableError(code, errorText));
        }

        [Fact]
        public void ResolveErrorCode_ParsesMessageEmbeddedCodes()
        {
            var code = PosnetRefundBankErrorHelper.ResolveErrorCode(
                null,
                "POSNET hata 0218: kısmi iade sonrası reverse yapılamaz");

            Assert.Equal(PosnetErrorCode.CannotReverseAfterRefund, code);
        }
    }
}
