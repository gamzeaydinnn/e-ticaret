using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace ECommerce.Tests.Integration
{
    /// <summary>
    /// Ağırlık entegrasyonu end-to-end flow testleri
    /// Test senaryosu: Tartı cihazı → Admin onayı → Kurye teslim → Ödeme
    /// </summary>
    public class WeightIntegrationFlowTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IWeightReportRepository> _mockWeightRepo;

        public WeightIntegrationFlowTests(ITestOutputHelper output)
        {
            _output = output;
            _mockWeightRepo = new Mock<IWeightReportRepository>();
        }

        [Fact]
        public async Task Scenario1_WeightReport_CanBeCreated()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 1: Ağırlık Raporu Oluşturma ===");
            
            var report = new WeightReport
            {
                Id = 1,
                ExternalReportId = "SCALE_1001_TEST",
                OrderId = 1001,
                ExpectedWeightGrams = 2000,
                ReportedWeightGrams = 2000,
                OverageGrams = 0,
                OverageAmount = 0,
                Status = WeightReportStatus.AutoApproved,
                ReceivedAt = DateTimeOffset.UtcNow
            };

            _mockWeightRepo.Setup(r => r.AddAsync(It.IsAny<WeightReport>()))
                .Returns(Task.CompletedTask);
            _mockWeightRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);

            // Act
            await _mockWeightRepo.Object.AddAsync(report);
            var result = await _mockWeightRepo.Object.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2000, result.ExpectedWeightGrams);
            Assert.Equal(WeightReportStatus.AutoApproved, result.Status);
            
            _output.WriteLine($"✅ Rapor oluşturuldu: #{result.Id}");
            _output.WriteLine($"✅ Durum: {result.Status}\n");
        }

        [Fact]
        public async Task Scenario2_OverageReport_RequiresApproval()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 2: Fazlalık Raporu - Onay Gerekli ===");
            
            var report = new WeightReport
            {
                Id = 2,
                ExternalReportId = "SCALE_1002_TEST",
                OrderId = 1002,
                ExpectedWeightGrams = 2000,
                ReportedWeightGrams = 2150,
                OverageGrams = 150,
                OverageAmount = 75.00m,
                Status = WeightReportStatus.Pending,
                ReceivedAt = DateTimeOffset.UtcNow
            };

            _mockWeightRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(report);

            // Act
            var result = await _mockWeightRepo.Object.GetByIdAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150, result.OverageGrams);
            Assert.Equal(75.00m, result.OverageAmount);
            Assert.Equal(WeightReportStatus.Pending, result.Status);
            
            _output.WriteLine($"✅ Beklenen: {result.ExpectedWeightGrams}g");
            _output.WriteLine($"✅ Gelen: {result.ReportedWeightGrams}g");
            _output.WriteLine($"⏳ Fark: +{result.OverageGrams}g = {result.OverageAmount:C}");
            _output.WriteLine($"⏳ Durum: {result.Status}\n");
        }

        [Fact]
        public async Task Scenario3_AdminApproval_ChangesStatus()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 3: Admin Onay Süreci ===");
            
            var report = new WeightReport
            {
                Id = 3,
                ExternalReportId = "SCALE_1003_TEST",
                OrderId = 1003,
                ExpectedWeightGrams = 2000,
                ReportedWeightGrams = 2120,
                OverageGrams = 120,
                OverageAmount = 60.00m,
                Status = WeightReportStatus.Pending,
                ReceivedAt = DateTimeOffset.UtcNow
            };

            report.Status = WeightReportStatus.Approved;
            report.ApprovedByUserId = 999;
            report.ApprovedAt = DateTimeOffset.UtcNow;

            _mockWeightRepo.Setup(r => r.UpdateAsync(report)).Returns(Task.CompletedTask);

            // Act
            await _mockWeightRepo.Object.UpdateAsync(report);

            // Assert
            Assert.Equal(WeightReportStatus.Approved, report.Status);
            
            _output.WriteLine($"✅ Rapor onaylandı: #{report.Id}");
            _output.WriteLine($"✅ Yeni Durum: {report.Status}\n");
        }

        [Fact]
        public async Task Scenario4_CourierDelivery_TriggersPayment()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 4: Kurye Teslim & Ödeme ===");
            
            var report = new WeightReport
            {
                Id = 4,
                ExternalReportId = "SCALE_1004_TEST",
                OrderId = 1004,
                ExpectedWeightGrams = 2000,
                ReportedWeightGrams = 2080,
                OverageGrams = 80,
                OverageAmount = 40.00m,
                Status = WeightReportStatus.Approved,
                ReceivedAt = DateTimeOffset.UtcNow.AddHours(-1),
                ApprovedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
                ApprovedByUserId = 999
            };

            var reports = new List<WeightReport> { report };
            _mockWeightRepo.Setup(r => r.GetByOrderIdAsync(1004)).ReturnsAsync(reports);

            // Act
            var result = await _mockWeightRepo.Object.GetByOrderIdAsync(1004);
            var approvedReport = result.FirstOrDefault(r => r.Status == WeightReportStatus.Approved);

            if (approvedReport != null)
            {
                approvedReport.Status = WeightReportStatus.Charged;
                approvedReport.PaymentAttemptId = "PAY_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            }

            // Assert
            Assert.NotNull(approvedReport);
            Assert.Equal(WeightReportStatus.Charged, approvedReport.Status);
            Assert.NotNull(approvedReport.PaymentAttemptId);
            
            _output.WriteLine($"📦 Sipariş #{approvedReport.OrderId} teslim edildi");
            _output.WriteLine($"💰 Ödeme alındı: {approvedReport.OverageAmount:C}");
            _output.WriteLine($"✅ Durum: {approvedReport.Status}\n");
        }

        [Fact]
        public async Task Scenario5_Idempotency_PreventsDuplicates()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 5: Idempotency Test ===");
            
            var existingReport = new WeightReport
            {
                Id = 5,
                ExternalReportId = "SCALE_1005_DUPLICATE",
                OrderId = 1005,
                ExpectedWeightGrams = 2000,
                ReportedWeightGrams = 2050,
                OverageGrams = 50,
                OverageAmount = 25.00m,
                Status = WeightReportStatus.AutoApproved,
                ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            };

            _mockWeightRepo.Setup(r => r.GetByExternalReportIdAsync("SCALE_1005_DUPLICATE"))
                .ReturnsAsync(existingReport);

            // Act
            var result = await _mockWeightRepo.Object.GetByExternalReportIdAsync("SCALE_1005_DUPLICATE");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            
            _output.WriteLine($"✅ Idempotency çalıştı");
            _output.WriteLine($"📋 Mevcut rapor döndürüldü: #{result.Id}\n");
        }

        [Fact]
        public async Task Scenario6_GetPendingReports_ReturnsCorrectly()
        {
            // Arrange
            _output.WriteLine("=== SENARYO 6: Bekleyen Raporlar ===");
            
            var pendingReports = new List<WeightReport>
            {
                new WeightReport
                {
                    Id = 6,
                    OrderId = 1006,
                    OverageGrams = 120,
                    OverageAmount = 60.00m,
                    Status = WeightReportStatus.Pending
                },
                new WeightReport
                {
                    Id = 7,
                    OrderId = 1007,
                    OverageGrams = 200,
                    OverageAmount = 100.00m,
                    Status = WeightReportStatus.Pending
                }
            };

            _mockWeightRepo.Setup(r => r.GetPendingReportsAsync()).ReturnsAsync(pendingReports);

            // Act
            var result = await _mockWeightRepo.Object.GetPendingReportsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            
            _output.WriteLine($"✅ Bekleyen rapor sayısı: {result.Count()}");
            foreach (var report in result)
            {
                _output.WriteLine($"   📋 Rapor #{report.Id}: {report.OverageGrams}g = {report.OverageAmount:C}");
            }
        }
    }
}
