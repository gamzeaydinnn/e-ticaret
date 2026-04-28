using System;
using System.Net.Http;
using System.Threading.Tasks;
using ECommerce.Business.Services.Sync;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    /// <summary>
    /// RetryService birim testleri.
    /// 
    /// KAPSAM:
    /// - IsRetryableException: retryable vs non-retryable sınıflandırması
    /// - CalculateNextRetryDelay: backoff hesaplama + jitter aralığı
    /// - Exception tipine göre doğru karar verme
    /// 
    /// NOT: ProcessPendingRetriesAsync ve diğer DB-bağımlı metodlar
    /// integration test kapsamında test edilir. Burada pure logic test ediliyor.
    /// </summary>
    public class RetryServiceTests
    {
        private readonly RetryService _service;

        public RetryServiceTests()
        {
            // RetryService constructor çok bağımlılık istiyor — sadece IsRetryableException
            // ve CalculateNextRetryDelay test edileceğinden minimal mock'lar yeterli
            _service = new RetryService(
                new Mock<IMikroSyncRepository>().Object,
                new Mock<ISyncLogger>().Object,
                new Mock<IStokSyncService>().Object,
                new Mock<IFiyatSyncService>().Object,
                new Mock<ISiparisSyncService>().Object,
                new Mock<ICariSyncService>().Object,
                new Mock<ILogger<RetryService>>().Object);
        }

        // ════════════════════════════════════════════════════════════════
        // IsRetryableException TESTLERİ — RETRYABLE DURUMLAR
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void IsRetryable_HttpRequestException_ReturnsTrue()
        {
            // Network/socket hataları — kesinlikle retryable
            var ex = new HttpRequestException("Connection refused");
            Assert.True(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_TaskCanceledException_ReturnsTrue()
        {
            // Timeout veya iptal — retryable
            var ex = new TaskCanceledException("The request was canceled due to timeout");
            Assert.True(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_OperationCanceledException_ReturnsTrue()
        {
            var ex = new OperationCanceledException("Operation timed out");
            Assert.True(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_TimeoutException_ReturnsTrue()
        {
            var ex = new TimeoutException("SQL Server timeout expired");
            Assert.True(_service.IsRetryableException(ex));
        }

        [Theory]
        [InlineData("Connection reset by peer")]
        [InlineData("timeout expired")]
        [InlineData("network unreachable")]
        [InlineData("service unavailable")]
        [InlineData("Server returned 502 Bad Gateway")]
        [InlineData("HTTP 503 error")]
        [InlineData("Gateway Timeout 504")]
        [InlineData("temporarily unavailable")]
        public void IsRetryable_TransientMessages_ReturnsTrue(string message)
        {
            // Transient hata mesajları — string bazlı kontrol
            var ex = new Exception(message);
            Assert.True(_service.IsRetryableException(ex));
        }

        // ════════════════════════════════════════════════════════════════
        // IsRetryableException TESTLERİ — NON-RETRYABLE DURUMLAR
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("401 unauthorized")]
        [InlineData("403 forbidden")]
        [InlineData("404 not found")]
        [InlineData("400 validation error")]
        public void IsRetryable_ClientErrors_ReturnsFalse(string message)
        {
            // 4xx hataları — retry anlamsız, business/auth hatası
            var ex = new Exception(message);
            Assert.False(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_UnknownException_ReturnsTrue()
        {
            // Bilinmeyen hata → varsayılan: retry dene (fail-safe)
            var ex = new InvalidOperationException("Something broke unexpectedly");
            Assert.True(_service.IsRetryableException(ex));
        }

        // ════════════════════════════════════════════════════════════════
        // CalculateNextRetryDelay TESTLERİ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void CalculateDelay_Attempt1_ReturnsZeroOrNearZero()
        {
            // İlk deneme → gecikme 0 (veya jitter ile çok küçük)
            var delay = _service.CalculateNextRetryDelay(1);
            Assert.InRange(delay.TotalSeconds, 0, 1); // 0 ± jitter
        }

        [Fact]
        public void CalculateDelay_Attempt2_Around60Seconds()
        {
            // 2. deneme → ~60sn (±10% jitter)
            var delay = _service.CalculateNextRetryDelay(2);
            Assert.InRange(delay.TotalSeconds, 54, 66); // 60 ± 10%
        }

        [Fact]
        public void CalculateDelay_Attempt3_Around300Seconds()
        {
            // 3. deneme → ~300sn (±10% jitter)
            var delay = _service.CalculateNextRetryDelay(3);
            Assert.InRange(delay.TotalSeconds, 270, 330); // 300 ± 10%
        }

        [Fact]
        public void CalculateDelay_ExceedMaxAttempt_ClampsToLastDelay()
        {
            // 4+ deneme → son delay değeri (300sn) ile sınırlandırılmalı
            var delay = _service.CalculateNextRetryDelay(10);
            Assert.InRange(delay.TotalSeconds, 270, 330);
        }

        [Fact]
        public void CalculateDelay_ZeroAttempt_HandlesGracefully()
        {
            // Edge case: 0 veya negatif attempt numarası
            var delay = _service.CalculateNextRetryDelay(0);
            Assert.True(delay.TotalSeconds >= 0); // Negatif süre olmamalı
        }

        [Fact]
        public void CalculateDelay_NegativeAttempt_HandlesGracefully()
        {
            var delay = _service.CalculateNextRetryDelay(-1);
            Assert.True(delay.TotalSeconds >= 0);
        }

        // ════════════════════════════════════════════════════════════════
        // MikroSyncException ENTEGRASYONU
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void IsRetryable_MikroApiException_5xx_ReturnsTrue()
        {
            var ex = new ECommerce.Core.Exceptions.MikroApiException("Server Error", 500);
            Assert.True(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_MikroCircuitOpenException_ReturnsTrue()
        {
            // Circuit open → varsayılan davranış: retry dene (genel exception olarak görülür)
            // NEDEN: CircuitOpen mesajda tanımlı keyword yok, fallback'e düşer → true
            var ex = new ECommerce.Core.Exceptions.MikroCircuitOpenException("CB açık");
            Assert.True(_service.IsRetryableException(ex));
        }

        [Fact]
        public void IsRetryable_MikroSyncTimeoutException_ReturnsTrue()
        {
            var ex = new ECommerce.Core.Exceptions.MikroSyncTimeoutException(
                "Timeout", TimeSpan.FromSeconds(30));
            Assert.True(_service.IsRetryableException(ex));
        }
    }
}
