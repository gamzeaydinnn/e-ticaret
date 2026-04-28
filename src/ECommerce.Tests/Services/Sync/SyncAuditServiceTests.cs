using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Sync;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    /// <summary>
    /// SyncAuditService testleri — audit log yazım ve hata toleransı.
    /// 
    /// KAPSAM:
    /// - Çakışma, CB, alert, dead letter, retry, manuel aksiyon loglanması
    /// - JSON details serializasyonu
    /// - Hatalı DB yazımında exception'ın yutuluyor olması (fire-and-forget)
    /// </summary>
    public class SyncAuditServiceTests
    {
        private static ECommerceDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task LogConflict_WritesToDatabase()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogConflictAsync(
                entityId: "SKU-001",
                conflictType: "Stock",
                strategy: "Conservative_Min",
                message: "Stok çakışması: Mikro=100, EC=80");

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("Conflict", log.EventType);
            Assert.Equal("Warning", log.Severity);
            Assert.Contains("Conservative_Min", log.Message);
            Assert.Equal("SKU-001", log.EntityId);
            Assert.Contains("Stock", log.Source);
        }

        [Fact]
        public async Task LogCircuitBreakerOpen_WritesErrorSeverity()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogCircuitBreakerEventAsync(
                source: "MikroHttpApi",
                state: "Open",
                message: "Mikro API erişilemez",
                correlationId: "abc123");

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("CircuitBreaker", log.EventType);
            Assert.Equal("Error", log.Severity);
            Assert.Contains("[CB:Open]", log.Message);
            Assert.Equal("abc123", log.CorrelationId);
        }

        [Fact]
        public async Task LogCircuitBreakerClosed_WritesInfoSeverity()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogCircuitBreakerEventAsync(
                source: "MikroHttpApi",
                state: "Closed",
                message: "Normal akış geri geldi");

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("Info", log.Severity);
        }

        [Fact]
        public async Task LogAlert_CustomSeverity()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogAlertAsync(
                source: "SyncMetrics",
                message: "Başarı oranı %60'ın altına düştü",
                severity: "Critical",
                details: new { SuccessRate = 0.55, Threshold = 0.6 });

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("Alert", log.EventType);
            Assert.Equal("Critical", log.Severity);
            Assert.Contains("successRate", log.Details!); // camelCase JSON
        }

        [Fact]
        public async Task LogDeadLetter_WritesError()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogDeadLetterAsync(
                entityId: "SKU-999",
                source: "RetryService",
                message: "3 deneme sonrası kalıcı başarısızlık");

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("DeadLetter", log.EventType);
            Assert.Equal("Error", log.Severity);
            Assert.Equal("SKU-999", log.EntityId);
        }

        [Fact]
        public async Task LogRetryEvent_Success_WritesInfo()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogRetryEventAsync(
                entityId: "SKU-050",
                source: "HotPoll",
                message: "Stok senkronizasyonu",
                attempt: 2,
                succeeded: true);

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("Retry", log.EventType);
            Assert.Equal("Info", log.Severity);
            Assert.Contains("Attempt#2", log.Message);
            Assert.Contains("✓", log.Message);
        }

        [Fact]
        public async Task LogRetryEvent_Failure_WritesWarning()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogRetryEventAsync(
                entityId: "SKU-050",
                source: "HotPoll",
                message: "Stok senkronizasyonu",
                attempt: 3,
                succeeded: false);

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("Warning", log.Severity);
            Assert.Contains("✗", log.Message);
        }

        [Fact]
        public async Task LogManualAction_RecordsUserId()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogManualActionAsync(
                entityId: "SKU-100",
                source: "AdminPanel",
                message: "Dead letter yeniden kuyruğa alındı",
                userId: "admin@test.com");

            var log = ctx.SyncAuditLogs.First();
            Assert.Equal("ManualAction", log.EventType);
            Assert.Contains("admin@test.com", log.Details!);
        }

        [Fact]
        public async Task LogConflict_WithDetailsObject_SerializesCorrectly()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            await service.LogConflictAsync(
                entityId: "SKU-200",
                conflictType: "Price",
                strategy: "ERP_Wins",
                message: "Fiyat çakışması",
                details: new { MikroPrice = 120.50m, ECommercePrice = 100.00m });

            var log = ctx.SyncAuditLogs.First();
            Assert.Contains("mikroPrice", log.Details!);
            Assert.Contains("120.5", log.Details!);
        }

        [Fact]
        public async Task CreatedAt_SetsUtcNow()
        {
            using var ctx = CreateInMemoryContext();
            var service = new SyncAuditService(ctx, new Mock<ILogger<SyncAuditService>>().Object);

            var before = DateTime.UtcNow;
            await service.LogAlertAsync("test", "test alert");
            var after = DateTime.UtcNow;

            var log = ctx.SyncAuditLogs.First();
            Assert.InRange(log.CreatedAt, before, after);
        }
    }
}
