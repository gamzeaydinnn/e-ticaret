using System;
using ECommerce.Business.Helpers;
using ECommerce.Entities.Concrete;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// <see cref="WeightBasedCapturePolicy"/> için regresyon testleri.
    ///
    /// NEDEN: Provizyon (Auth) → kesin çekim (Capt) politikası daha önce üç ayrı serviste
    /// kopyalanmıştı. Bu testler tek doğruluk kaynağındaki iş kuralını sabitler:
    /// - Banka, provizyonun EN FAZLA %20 üzerine kadar çekime izin verir (Capt ≤ Auth × 1.20).
    /// - Provizyon tutarı için AuthorizedAmount/PreAuthAmount tekilleştirmesi.
    /// </summary>
    public class WeightBasedCapturePolicyTests
    {
        [Theory]
        [InlineData(100, 120)]   // 100 → 100 × 1.20 = 120
        [InlineData(250, 300)]   // 250 → 300
        [InlineData(99.99, 119.99)] // yuvarlama (AwayFromZero): 119.988 → 119.99
        public void CalculateMaxCapturableAmount_AppliesTwentyPercentOverage(decimal auth, decimal expected)
        {
            Assert.Equal(expected, WeightBasedCapturePolicy.CalculateMaxCapturableAmount(auth));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void CalculateMaxCapturableAmount_ReturnsZero_WhenNoAuthorization(decimal auth)
        {
            Assert.Equal(0m, WeightBasedCapturePolicy.CalculateMaxCapturableAmount(auth));
        }

        [Theory]
        [InlineData(100, 100, true)]   // tam tutar → çekilebilir
        [InlineData(100, 120, true)]   // tam %20 sınırı → çekilebilir
        [InlineData(100, 120.01, false)] // sınırın 1 kuruş üstü → çekilemez
        [InlineData(100, 200, false)]  // limit aşıldı → çekilemez
        public void IsWithinCaptureLimit_HonorsTwentyPercentBankLimit(decimal auth, decimal final, bool expected)
        {
            Assert.Equal(expected, WeightBasedCapturePolicy.IsWithinCaptureLimit(auth, final));
        }

        [Fact]
        public void ClampToCaptureLimit_CapturesFullFinal_WhenWithinLimit()
        {
            // 100 auth → max 120. final 110 ≤ 120 → tam çek, aşım yok.
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(100m, 110m);
            Assert.Equal(110m, decision.CaptureAmount);
            Assert.False(decision.ExceedsLimit);
        }

        [Fact]
        public void ClampToCaptureLimit_CapturesAtLimit_WhenExceeded()
        {
            // 100 auth → max 120. final 150 > 120 → sınıra kırp (120), aşım bayrağı.
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(100m, 150m);
            Assert.Equal(120m, decision.CaptureAmount);
            Assert.True(decision.ExceedsLimit);
        }

        [Fact]
        public void ClampToCaptureLimit_CapturesFinal_WhenLowerThanAuth()
        {
            // Tartı azaldı: final 80 < auth 100 → 80 çek (banka kalanı serbest bırakır), aşım yok.
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(100m, 80m);
            Assert.Equal(80m, decision.CaptureAmount);
            Assert.False(decision.ExceedsLimit);
        }

        [Fact]
        public void ResolveAuthorizedAmount_PrefersAuthorizedAmount_WhenPositive()
        {
            var order = new Order { AuthorizedAmount = 150m, PreAuthAmount = 120m };
            Assert.Equal(150m, WeightBasedCapturePolicy.ResolveAuthorizedAmount(order));
        }

        [Fact]
        public void ResolveAuthorizedAmount_FallsBackToPreAuthAmount_WhenAuthorizedZero()
        {
            // Senaryo: ağırlık akışı yalnız PreAuthAmount'u doldurmuş; capture katmanı yine doğru karar vermeli.
            var order = new Order { AuthorizedAmount = 0m, PreAuthAmount = 120m };
            Assert.Equal(120m, WeightBasedCapturePolicy.ResolveAuthorizedAmount(order));
        }

        [Fact]
        public void ResolveAuthorizedAmount_ReturnsZero_WhenOrderNull()
        {
            Assert.Equal(0m, WeightBasedCapturePolicy.ResolveAuthorizedAmount(null));
        }

        [Fact]
        public void IsPreAuthExpired_ReturnsFalse_WhenAuthDateUnknown()
        {
            // Tarih bilinmiyorsa çekimi erkenden engellemeyiz (güvenli taraf).
            Assert.False(WeightBasedCapturePolicy.IsPreAuthExpired(null));
        }

        [Fact]
        public void IsPreAuthExpired_DetectsExpiry_BasedOnValidityHours()
        {
            var now = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
            var fresh = now.AddHours(-(WeightBasedCapturePolicy.PreAuthValidityHours - 1));
            var stale = now.AddHours(-(WeightBasedCapturePolicy.PreAuthValidityHours + 1));

            Assert.False(WeightBasedCapturePolicy.IsPreAuthExpired(fresh, now));
            Assert.True(WeightBasedCapturePolicy.IsPreAuthExpired(stale, now));
        }
    }
}
