using ECommerce.Business.Helpers;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// Adım 1–2 regresyonları:
    /// - Capt ≤ Auth × 1.20 (1 kg → 1100 g senaryosu)
    /// - Manuel tartı yalnız Preparing
    /// </summary>
    public class WeightBasedProvisionFlowTests
    {
        [Fact]
        public void OneKgTo1100g_OverageIsWithinAuthCaptureLimit()
        {
            // Sepet/3DS: 1 kg @ 100 TL/kg → Auth = 100 TL (marjsız)
            const decimal authAmount = 100m;
            const decimal pricePerKg = 100m;
            const decimal estimatedGrams = 1000m;
            const decimal actualGrams = 1100m;

            var estimatedPrice = pricePerKg * (estimatedGrams / 1000m);
            var actualPrice = pricePerKg * (actualGrams / 1000m);
            var priceDifference = actualPrice - estimatedPrice;

            Assert.Equal(100m, estimatedPrice);
            Assert.Equal(110m, actualPrice);
            Assert.Equal(10m, priceDifference); // 100 g provizyon farkı

            var maxCapturable = WeightBasedCapturePolicy.CalculateMaxCapturableAmount(authAmount);
            Assert.Equal(120m, maxCapturable); // Auth × 1.20

            Assert.True(WeightBasedCapturePolicy.IsWithinCaptureLimit(authAmount, actualPrice));

            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(authAmount, actualPrice);
            Assert.Equal(110m, decision.CaptureAmount);
            Assert.False(decision.ExceedsLimit);
        }

        [Fact]
        public void OverageAboveTwentyPercent_IsClampedForCapture()
        {
            // Auth 100 TL; tartı sonucu 130 TL (> %20) → Capt 120, aşım bayrağı
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(100m, 130m);
            Assert.Equal(120m, decision.CaptureAmount);
            Assert.True(decision.ExceedsLimit);
        }

        [Theory]
        [InlineData(OrderStatus.Preparing, true)]
        [InlineData(OrderStatus.Confirmed, false)]
        [InlineData(OrderStatus.Pending, false)]
        [InlineData(OrderStatus.Paid, false)]
        [InlineData(OrderStatus.PreAuthorized, false)]
        [InlineData(OrderStatus.Ready, false)]
        [InlineData(OrderStatus.WeightPending, false)]
        [InlineData(OrderStatus.New, false)]
        public void ManualWeight_OnlyAllowedDuringPreparing(OrderStatus status, bool expected)
        {
            Assert.Equal(expected, WeightBasedWeighingGate.CanEnterManualWeight(status));
            Assert.Equal(expected, WeightBasedWeighingGate.IsEligibleForWeightReportsList(status));
        }

        [Fact]
        public void ThreeDsAmount_EqualsCart_NoInflateOnAuthHold()
        {
            // Politika: 3DS/Auth tutarı = sepet FinalPrice; %20 yalnız Capt aşımında
            const decimal finalPrice = 100.90m;
            var preAuthAmount = finalPrice; // OrderManager: marjsız
            var maxAtCapture = WeightBasedCapturePolicy.CalculateMaxCapturableAmount(preAuthAmount);

            Assert.Equal(100.90m, preAuthAmount);
            Assert.Equal(121.08m, maxAtCapture);
            Assert.NotEqual(preAuthAmount, maxAtCapture);
        }

        [Fact]
        public void AuthFlow_IsDetected_WhenAuthorizedAndCapturePending()
        {
            var order = new Order
            {
                HasWeightBasedItems = true,
                PaymentStatus = PaymentStatus.Authorized,
                CaptureStatus = CaptureStatus.Pending,
                PreAuthHostLogKey = "HLK-123",
                AuthorizedAmount = 100m
            };

            Assert.Equal(WeightBasedPaymentFlowType.AuthCapture, WeightBasedPaymentFlowResolver.Resolve(order));
            Assert.True(WeightBasedPaymentFlowResolver.CanBankCaptureOverage(order));
        }

        [Fact]
        public void SaleFlow_IsDetected_WhenPaidAtCheckout()
        {
            var order = new Order
            {
                HasWeightBasedItems = true,
                PaymentStatus = PaymentStatus.Paid,
                CaptureStatus = CaptureStatus.Success,
                CapturedAmount = 100m,
                FinalPrice = 100m
            };

            Assert.Equal(WeightBasedPaymentFlowType.SaleImmediate, WeightBasedPaymentFlowResolver.Resolve(order));
            Assert.False(WeightBasedPaymentFlowResolver.CanBankCaptureOverage(order));
        }

        [Fact]
        public void SaleFlow_1100gOverage_IsManualCollection()
        {
            var order = new Order
            {
                HasWeightBasedItems = true,
                PaymentStatus = PaymentStatus.Paid,
                CaptureStatus = CaptureStatus.Success,
                CapturedAmount = 100m,
                FinalPrice = 100m
            };

            var flow = WeightBasedPaymentFlowResolver.Resolve(order);
            const decimal priceDifference = 10m; // 100g @ 100 TL/kg

            Assert.Equal(WeightBasedPaymentFlowType.SaleImmediate, flow);
            Assert.True(priceDifference > 0);
            Assert.False(WeightBasedPaymentFlowResolver.CanBankCaptureOverage(order));
        }
    }
}
