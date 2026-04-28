using System;
using System.Threading;
using ECommerce.Business.Services.Sync;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    /// <summary>
    /// SyncConflictCoordinator testleri.
    /// 
    /// KAPSAM:
    /// - Stok çakışma senaryoları: NoConflict, MikroWins, ECommerceWins, Conservative_Min
    /// - Fiyat çakışma senaryoları: NoConflict, ERP_Wins, AdminOverride
    /// - Ürün bilgi çakışma senaryoları: NoConflict, ERP_Wins, AdminOverride
    /// - Edge case'ler: null tarih, 0 değer, negatif stok, eşit tarihler
    /// </summary>
    public class SyncConflictCoordinatorTests
    {
        private readonly SyncConflictCoordinator _coordinator;
        private readonly Mock<ISyncLogger> _syncLoggerMock;

        public SyncConflictCoordinatorTests()
        {
            _syncLoggerMock = new Mock<ISyncLogger>();
            // StartOperationAsync mock — optional CancellationToken parametresi
            // expression tree optional param sorununu It.IsAny<CancellationToken>() ile çözüyoruz
            _syncLoggerMock
                .Setup(x => x.StartOperationAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ECommerce.Entities.Concrete.MicroSyncLog { Id = 1 });

            _coordinator = new SyncConflictCoordinator(
                new Mock<ILogger<SyncConflictCoordinator>>().Object,
                _syncLoggerMock.Object);
        }

        // ════════════════════════════════════════════════════════════════
        // STOK ÇAKIŞMA TESTLERİ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void ResolveStockConflict_EqualValues_ReturnsNoConflict()
        {
            // Arrange — değerler eşit, tarih farklı
            var result = _coordinator.ResolveStockConflict(
                "SKU-001", mikroValue: 50m, ecommerceValue: 50m,
                mikroLastUpdate: DateTime.UtcNow.AddMinutes(-5),
                ecommerceLastUpdate: DateTime.UtcNow.AddMinutes(-1));

            // Assert
            Assert.False(result.HasConflict);
            Assert.Equal(50m, result.ResolvedValue);
            Assert.Equal("NoConflict", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_MikroNewer_MikroWins()
        {
            // Arrange — Mikro tarihi daha yeni
            var now = DateTime.UtcNow;
            var result = _coordinator.ResolveStockConflict(
                "SKU-002", mikroValue: 100m, ecommerceValue: 80m,
                mikroLastUpdate: now,
                ecommerceLastUpdate: now.AddMinutes(-10));

            Assert.True(result.HasConflict);
            Assert.Equal(100m, result.ResolvedValue);
            Assert.Equal("MikroWins", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_ECommerceNewer_ECommerceWins()
        {
            // Arrange — EC tarihi daha yeni (sipariş düştü senaryosu)
            var now = DateTime.UtcNow;
            var result = _coordinator.ResolveStockConflict(
                "SKU-003", mikroValue: 100m, ecommerceValue: 95m,
                mikroLastUpdate: now.AddMinutes(-10),
                ecommerceLastUpdate: now);

            Assert.True(result.HasConflict);
            Assert.Equal(95m, result.ResolvedValue);
            Assert.Equal("ECommerceWins", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_BothSameTime_Conservative_Min()
        {
            // Arrange — aynı anda güncellendi → muhafazakar yaklaşım: min()
            var sameTime = DateTime.UtcNow;
            var result = _coordinator.ResolveStockConflict(
                "SKU-004", mikroValue: 100m, ecommerceValue: 80m,
                mikroLastUpdate: sameTime,
                ecommerceLastUpdate: sameTime);

            Assert.True(result.HasConflict);
            Assert.Equal(80m, result.ResolvedValue); // min(100, 80) = 80
            Assert.Equal("Conservative_Min", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_NullEcommerceDate_MikroWins()
        {
            // Arrange — EC tarih null (ilk sync, EC hiç güncellenmemiş)
            var result = _coordinator.ResolveStockConflict(
                "SKU-005", mikroValue: 200m, ecommerceValue: 0m,
                mikroLastUpdate: DateTime.UtcNow,
                ecommerceLastUpdate: null);

            Assert.True(result.HasConflict);
            Assert.Equal(200m, result.ResolvedValue);
            Assert.Equal("MikroWins", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_NullMikroDate_ECommerceWins()
        {
            // Arrange — Mikro tarih null (Mikro'da erişim sorunu)
            var result = _coordinator.ResolveStockConflict(
                "SKU-006", mikroValue: 50m, ecommerceValue: 75m,
                mikroLastUpdate: null,
                ecommerceLastUpdate: DateTime.UtcNow);

            Assert.True(result.HasConflict);
            Assert.Equal(75m, result.ResolvedValue);
            Assert.Equal("ECommerceWins", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_ZeroMikroStock_MinReturnsZero()
        {
            // Arrange — Mikro stok 0, EC > 0, aynı zaman → min() = 0
            // NEDEN ÖNEMLİ: Stok bitti senaryosu doğru çalışmalı
            var sameTime = DateTime.UtcNow;
            var result = _coordinator.ResolveStockConflict(
                "SKU-007", mikroValue: 0m, ecommerceValue: 10m,
                mikroLastUpdate: sameTime,
                ecommerceLastUpdate: sameTime);

            Assert.True(result.HasConflict);
            Assert.Equal(0m, result.ResolvedValue);
            Assert.Equal("Conservative_Min", result.Strategy);
        }

        [Fact]
        public void ResolveStockConflict_BothSameTime_LogsConflict()
        {
            // Arrange — aynı anda güncellendi → sync logger çağrılmalı
            var sameTime = DateTime.UtcNow;

            _coordinator.ResolveStockConflict(
                "SKU-008", mikroValue: 100m, ecommerceValue: 80m,
                mikroLastUpdate: sameTime,
                ecommerceLastUpdate: sameTime);

            // Assert — çakışma loglandı
            _syncLoggerMock.Verify(x => x.StartOperationAsync(
                "StokConflict", "Conflict",
                "SKU-008", It.IsAny<string?>(),
                It.Is<string>(s => s.Contains("min")),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ════════════════════════════════════════════════════════════════
        // FİYAT ÇAKIŞMA TESTLERİ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void ResolvePriceConflict_EqualPrices_ReturnsNoConflict()
        {
            var result = _coordinator.ResolvePriceConflict(
                "SKU-010", mikroPrice: 99.90m, ecommercePrice: 99.90m);

            Assert.False(result.HasConflict);
            Assert.Equal(99.90m, result.ResolvedPrice);
            Assert.False(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolvePriceConflict_DifferentPrices_ERPWins()
        {
            // KURAL: Mikro her zaman master (ERP-Wins) — admin override yoksa
            var result = _coordinator.ResolvePriceConflict(
                "SKU-011", mikroPrice: 120.00m, ecommercePrice: 100.00m);

            Assert.True(result.HasConflict);
            Assert.Equal(120.00m, result.ResolvedPrice);
            Assert.Equal("ERP_Wins", result.Strategy);
            Assert.False(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolvePriceConflict_AdminOverride_AdminWinsAndPushes()
        {
            // Admin bilinçli fiyat değişikliği — admin değeri korunmalı + push
            var result = _coordinator.ResolvePriceConflict(
                "SKU-012", mikroPrice: 120.00m, ecommercePrice: 89.90m,
                isAdminOverride: true);

            Assert.True(result.HasConflict);
            Assert.Equal(89.90m, result.ResolvedPrice);
            Assert.Equal("AdminOverride", result.Strategy);
            Assert.True(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolvePriceConflict_ZeroMikroPrice_ERPWinsWithZero()
        {
            // Edge case: Mikro fiyat 0 gelebilir (pasif ürün) — ERP master kuralı geçerli
            var result = _coordinator.ResolvePriceConflict(
                "SKU-013", mikroPrice: 0m, ecommercePrice: 99.90m);

            Assert.True(result.HasConflict);
            Assert.Equal(0m, result.ResolvedPrice);
            Assert.Equal("ERP_Wins", result.Strategy);
        }

        // ════════════════════════════════════════════════════════════════
        // ÜRÜN BİLGİ ÇAKIŞMA TESTLERİ
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public void ResolveInfoConflict_SameNames_ReturnsNoConflict()
        {
            var result = _coordinator.ResolveInfoConflict(
                "SKU-020", mikroName: "Test Ürün", ecommerceName: "Test Ürün");

            Assert.False(result.HasConflict);
            Assert.Equal("Test Ürün", result.ResolvedName);
            Assert.False(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolveInfoConflict_DifferentNames_ERPWins()
        {
            var result = _coordinator.ResolveInfoConflict(
                "SKU-021", mikroName: "Mikro Adı", ecommerceName: "EC Adı");

            Assert.True(result.HasConflict);
            Assert.Equal("Mikro Adı", result.ResolvedName);
            Assert.Equal("ERP_Wins", result.Strategy);
            Assert.False(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolveInfoConflict_AdminOverride_AdminWinsAndPushes()
        {
            var result = _coordinator.ResolveInfoConflict(
                "SKU-022", mikroName: "Mikro Adı", ecommerceName: "Admin Düzenledi",
                isAdminOverride: true);

            Assert.True(result.HasConflict);
            Assert.Equal("Admin Düzenledi", result.ResolvedName);
            Assert.Equal("AdminOverride", result.Strategy);
            Assert.True(result.ShouldPushToMikro);
        }

        [Fact]
        public void ResolveInfoConflict_NullMikroName_ERPWinsFallsback()
        {
            // Edge case: Mikro ürün adı null — EC adı korunmalı
            var result = _coordinator.ResolveInfoConflict(
                "SKU-023", mikroName: null, ecommerceName: "EC Adı");

            Assert.True(result.HasConflict);
            // null ?? "EC Adı" → ERP_Wins stratejisi ama Mikro null ise EC adı kullanılır
            Assert.Equal("EC Adı", result.ResolvedName);
        }

        [Fact]
        public void ResolveInfoConflict_BothNull_ReturnsEmpty()
        {
            // Edge case: her ikisi de null — boş string dönmeli
            var result = _coordinator.ResolveInfoConflict(
                "SKU-024", mikroName: null, ecommerceName: null);

            Assert.False(result.HasConflict); // null == null → eşit
            Assert.Equal(string.Empty, result.ResolvedName);
        }
    }
}
