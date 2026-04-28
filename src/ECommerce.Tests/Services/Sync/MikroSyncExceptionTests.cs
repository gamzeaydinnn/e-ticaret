using System;
using ECommerce.Core.Exceptions;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    /// <summary>
    /// MikroSyncException hiyerarşisi testleri — IsRetryable property doğrulaması.
    /// 
    /// NEDEN: Exception tipine göre retry kararı verildiği için
    /// IsRetryable property'sinin doğru hesaplandığından emin olunmalı.
    /// </summary>
    public class MikroSyncExceptionTests
    {
        // ════════════════════════════════════════════════════════════════
        // MikroApiException
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(500, true)]
        [InlineData(502, true)]
        [InlineData(503, true)]
        [InlineData(504, true)]
        [InlineData(null, true)]  // StatusCode null → retryable (network hatası)
        public void MikroApiException_5xxOrNull_IsRetryable(int? statusCode, bool expected)
        {
            var ex = new MikroApiException("Server error", statusCode);
            Assert.Equal(expected, ex.IsRetryable);
        }

        [Theory]
        [InlineData(400, false)]
        [InlineData(401, false)]
        [InlineData(403, false)]
        [InlineData(404, false)]
        [InlineData(422, false)]
        public void MikroApiException_4xx_NotRetryable(int statusCode, bool expected)
        {
            var ex = new MikroApiException("Client error", statusCode);
            Assert.Equal(expected, ex.IsRetryable);
        }

        [Fact]
        public void MikroApiException_PreservesEndpoint()
        {
            var ex = new MikroApiException("Error", 500) { Endpoint = "/Api/StokListesi" };
            Assert.Equal("/Api/StokListesi", ex.Endpoint);
            Assert.Equal(500, ex.StatusCode);
        }

        // ════════════════════════════════════════════════════════════════
        // MikroSqlException
        // ════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(-2, true)]     // Timeout
        [InlineData(1205, true)]   // Deadlock
        [InlineData(40613, true)]  // DB unavailable
        [InlineData(40197, true)]  // Service error
        [InlineData(40501, true)]  // Service busy
        [InlineData(49918, true)]  // Not enough resources
        public void MikroSqlException_TransientErrors_IsRetryable(int sqlErrorNumber, bool expected)
        {
            var ex = new MikroSqlException("SQL Error", sqlErrorNumber);
            Assert.Equal(expected, ex.IsRetryable);
        }

        [Theory]
        [InlineData(207, false)]    // Bilinmeyen hata → not retryable
        [InlineData(8152, false)]   // String truncation
        [InlineData(null, false)]   // Null → not retryable
        public void MikroSqlException_NonTransientErrors_NotRetryable(int? sqlErrorNumber, bool expected)
        {
            var ex = new MikroSqlException("SQL Error", sqlErrorNumber);
            Assert.Equal(expected, ex.IsRetryable);
        }

        // ════════════════════════════════════════════════════════════════
        // MikroCircuitOpenException
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void MikroCircuitOpenException_IsNeverRetryable()
        {
            // CB açık → retry ile aşılamaz, süre dolmalı
            var ex = new MikroCircuitOpenException("CB açık");
            Assert.False(ex.IsRetryable);
        }

        [Fact]
        public void MikroCircuitOpenException_PreservesRecoveryTime()
        {
            var recovery = TimeSpan.FromSeconds(30);
            var ex = new MikroCircuitOpenException("CB açık") { EstimatedRecoveryTime = recovery };
            Assert.Equal(recovery, ex.EstimatedRecoveryTime);
        }

        // ════════════════════════════════════════════════════════════════
        // MikroConflictException
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void MikroConflictException_IsNeverRetryable()
        {
            // Çakışma → çözüm kararı gerektirir, blind retry anlamsız
            var ex = new MikroConflictException("Çakışma")
            {
                ConflictType = "Stock",
                Strategy = "Conservative_Min",
                StokKod = "SKU-001"
            };
            Assert.False(ex.IsRetryable);
            Assert.Equal("Stock", ex.ConflictType);
        }

        // ════════════════════════════════════════════════════════════════
        // MikroSyncTimeoutException
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void MikroSyncTimeoutException_IsAlwaysRetryable()
        {
            var ex = new MikroSyncTimeoutException("Timeout", TimeSpan.FromSeconds(45));
            Assert.True(ex.IsRetryable);
            Assert.Equal(45, ex.TimeoutDuration.TotalSeconds);
        }

        [Fact]
        public void MikroSyncTimeoutException_PreservesDirection()
        {
            var ex = new MikroSyncTimeoutException("Timeout", TimeSpan.FromSeconds(30))
            {
                Direction = "FromERP",
                StokKod = "SKU-123"
            };
            Assert.Equal("FromERP", ex.Direction);
            Assert.Equal("SKU-123", ex.StokKod);
        }

        // ════════════════════════════════════════════════════════════════
        // InnerException Propagation
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void AllExceptions_PropagateInnerException()
        {
            var inner = new InvalidOperationException("root cause");

            var apiEx = new MikroApiException("API Error", 500, inner);
            Assert.Same(inner, apiEx.InnerException);

            var sqlEx = new MikroSqlException("SQL Error", -2, inner);
            Assert.Same(inner, sqlEx.InnerException);

            var timeoutEx = new MikroSyncTimeoutException("Timeout", TimeSpan.FromSeconds(10), inner);
            Assert.Same(inner, timeoutEx.InnerException);
        }
    }
}
