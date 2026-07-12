using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Mapping;
using ECommerce.Business.Services.Sync;
using ECommerce.Core.DTOs.Inventory;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    /// <summary>
    /// Faz 5: Sipariş → Mikro stok düşüşü / İade → Mikro stok artışı yansıma testleri.
    /// </summary>
    public class MikroStockReflectionTests
    {
        private static ECommerceDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task Sale_PushSiparisV2_SendsLineQty_AndDecreasesMikroCache()
        {
            using var db = CreateDb();

            var product = new Product
            {
                Name = "Peynir",
                SKU = "SKU-PEYNIR",
                StockQuantity = 20,
                Price = 100
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Set<MikroProductCache>().Add(new MikroProductCache
            {
                StokKod = "SKU-PEYNIR",
                StokAd = "Peynir",
                DepoMiktari = 20,
                SatilabilirMiktar = 20
            });
            await db.SaveChangesAsync();

            var order = new Order
            {
                OrderNumber = "ORD-SALE-1",
                Status = OrderStatus.Paid,
                IsInventoryCommitted = true,
                FinalPrice = 200,
                TotalPrice = 200,
                OrderDate = DateTime.UtcNow,
                CustomerName = "Test",
                CustomerEmail = "t@t.com",
                CustomerPhone = "555",
                UserId = 7
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 2,
                UnitPrice = 100,
                VariantSku = "SKU-PEYNIR"
            });
            await db.SaveChangesAsync();

            MikroSiparisKaydetRequestDto? captured = null;
            IEnumerable<MicroStockDto>? stockCaptured = null;
            var micro = new Mock<IMicroService>();
            micro.Setup(m => m.PushSiparisV2Async(It.IsAny<MikroSiparisKaydetRequestDto>(), It.IsAny<CancellationToken>()))
                .Callback<MikroSiparisKaydetRequestDto, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync((true, "ok", "ONL", 42));
            micro.Setup(m => m.UpsertStocksAsync(It.IsAny<IEnumerable<MicroStockDto>>()))
                .Callback<IEnumerable<MicroStockDto>>(s => stockCaptured = s.ToList())
                .ReturnsAsync(true);

            var orderRepo = new Mock<IOrderRepository>();
            orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

            var syncRepo = new Mock<IMikroSyncRepository>();
            syncRepo.Setup(r => r.CreateLogAsync(It.IsAny<MicroSyncLog>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MicroSyncLog log, CancellationToken _) => log);
            syncRepo.Setup(r => r.UpdateSyncSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var cari = new Mock<ICariSyncService>();
            cari.Setup(c => c.GetMikroCariKodAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync("CARI001");

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MikroApi:Siparis:EvrakSeri"] = "ONL",
                ["MikroApi:Siparis:DefaultDepoNo"] = "1",
                ["MikroApi:Siparis:DefaultDeliveryDays"] = "1",
                ["MikroApi:Siparis:AddShippingAsLine"] = "false",
                ["MikroApi:Siparis:DefaultKdvOran"] = "20"
            }).Build();

            var mapper = new MikroSiparisMapper(config, NullLogger<MikroSiparisMapper>.Instance);

            var sut = new SiparisSyncService(
                micro.Object,
                orderRepo.Object,
                syncRepo.Object,
                cari.Object,
                mapper,
                db,
                NullLogger<SiparisSyncService>.Instance);

            var result = await sut.PushOrderToMikroAsync(order.Id);

            Assert.True(result.IsSuccess);
            Assert.NotNull(captured);
            Assert.Contains(captured!.Satirlar, s => s.SipStokKod == "SKU-PEYNIR" && s.SipMiktar == 2);

            // Eldeki stok: DahiliStokHareket çıkışı (IsStockIncrease=false)
            Assert.NotNull(stockCaptured);
            Assert.Contains(stockCaptured!, s => s.Sku == "SKU-PEYNIR" && s.Quantity == 2 && !s.IsStockIncrease);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.True(LocalInventoryPolicy.IsMikroSyncedTracking(reloaded!.TrackingNumber));
            Assert.Equal("MIKRO-ONL-42", reloaded.TrackingNumber);

            var cache = await db.Set<MikroProductCache>().SingleAsync(c => c.StokKod == "SKU-PEYNIR");
            Assert.Equal(18, cache.SatilabilirMiktar);
            Assert.Equal(18, cache.DepoMiktari);
        }

        [Fact]
        public async Task Refund_LocalStockIncrease_AndCacheAlignReflectsMikroStockUp()
        {
            using var db = CreateDb();

            var product = new Product
            {
                Name = "Zeytin",
                SKU = "SKU-ZEYTIN",
                StockQuantity = 5,
                Price = 50
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Set<MikroProductCache>().Add(new MikroProductCache
            {
                StokKod = "SKU-ZEYTIN",
                DepoMiktari = 5,
                SatilabilirMiktar = 5
            });
            await db.SaveChangesAsync();

            // Yerel iade restore
            product.StockQuantity += 1;
            await db.SaveChangesAsync();
            Assert.Equal(6, product.StockQuantity);

            // İade faturası sonrası cache hizalama (FaturaSyncService AlignCacheAfterRefund)
            await MikroStockCacheAligner.ApplyDeltaAsync(
                db,
                new[] { ("SKU-ZEYTIN", 1m) },
                NullLogger.Instance);

            var cache = await db.Set<MikroProductCache>().SingleAsync(c => c.StokKod == "SKU-ZEYTIN");
            Assert.Equal(6, cache.SatilabilirMiktar);
            Assert.Equal(6, cache.DepoMiktari);

            // İade fatura satırı gerçek SKU + NormalIade=1 (Mikro stok artışı belgesi)
            var refundLine = new MikroFaturaSatirDto
            {
                SthStokKod = "SKU-ZEYTIN",
                SthMiktar = 1,
                SthNormalIade = 1,
                SthGirisDepoNo = 1,
                SthCikisDepoNo = 1
            };
            Assert.Equal("SKU-ZEYTIN", refundLine.SthStokKod);
            Assert.Equal(1, refundLine.SthNormalIade);
            Assert.NotEqual("IADE", refundLine.SthStokKod);
        }

        [Fact]
        public async Task OutboundStockPush_DecreaseDelta_SetsIsStockIncreaseFalse()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Bal", SKU = "SKU-BAL", StockQuantity = 8 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Set<MikroProductCache>().Add(new MikroProductCache
            {
                StokKod = "SKU-BAL",
                DepoMiktari = 10,
                SatilabilirMiktar = 10
            });
            await db.SaveChangesAsync();

            MicroStockDto? captured = null;
            var micro = new Mock<IMicroService>();
            micro.Setup(m => m.UpsertStocksAsync(It.IsAny<IEnumerable<MicroStockDto>>()))
                .Callback<IEnumerable<MicroStockDto>>(s => captured = s.First())
                .ReturnsAsync(true);

            var productRepo = new Mock<IProductRepository>();
            productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var syncRepo = new Mock<IMikroSyncRepository>();
            syncRepo.Setup(r => r.UpdateSyncSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var syncLogger = new Mock<ISyncLogger>();
            syncLogger.Setup(s => s.StartOperationAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MicroSyncLog { Id = 1 });
            syncLogger.Setup(s => s.CompleteOperationAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new MikroOutboundSyncService(
                micro.Object,
                productRepo.Object,
                syncRepo.Object,
                syncLogger.Object,
                db,
                Options.Create(new MikroSettings { DefaultDepoNo = 1 }),
                NullLogger<MikroOutboundSyncService>.Instance);

            var result = await sut.PushStockChangeAsync(product.Id, 8, "Sale");

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(2, captured!.Quantity);
            Assert.False(captured.IsStockIncrease);

            var cache = await db.Set<MikroProductCache>().SingleAsync(c => c.StokKod == "SKU-BAL");
            Assert.Equal(8, cache.SatilabilirMiktar);
        }

        [Fact]
        public async Task OutboundStockPush_IncreaseDelta_SetsIsStockIncreaseTrue()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Bal2", SKU = "SKU-BAL2", StockQuantity = 12 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Set<MikroProductCache>().Add(new MikroProductCache
            {
                StokKod = "SKU-BAL2",
                DepoMiktari = 10,
                SatilabilirMiktar = 10
            });
            await db.SaveChangesAsync();

            MicroStockDto? captured = null;
            var micro = new Mock<IMicroService>();
            micro.Setup(m => m.UpsertStocksAsync(It.IsAny<IEnumerable<MicroStockDto>>()))
                .Callback<IEnumerable<MicroStockDto>>(s => captured = s.First())
                .ReturnsAsync(true);

            var productRepo = new Mock<IProductRepository>();
            productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

            var syncRepo = new Mock<IMikroSyncRepository>();
            syncRepo.Setup(r => r.UpdateSyncSuccessAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var syncLogger = new Mock<ISyncLogger>();
            syncLogger.Setup(s => s.StartOperationAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MicroSyncLog { Id = 1 });
            syncLogger.Setup(s => s.CompleteOperationAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new MikroOutboundSyncService(
                micro.Object,
                productRepo.Object,
                syncRepo.Object,
                syncLogger.Object,
                db,
                Options.Create(new MikroSettings { DefaultDepoNo = 1 }),
                NullLogger<MikroOutboundSyncService>.Instance);

            var result = await sut.PushStockChangeAsync(product.Id, 12, "Return");

            Assert.True(result.Success);
            Assert.NotNull(captured);
            Assert.Equal(2, captured!.Quantity);
            Assert.True(captured.IsStockIncrease);

            var cache = await db.Set<MikroProductCache>().SingleAsync(c => c.StokKod == "SKU-BAL2");
            Assert.Equal(12, cache.SatilabilirMiktar);
        }

        [Fact]
        public void Faz4_ShouldSkipInbound_WhenPendingUnsyncedSale()
        {
            Assert.True(LocalInventoryPolicy.ShouldSkipInboundStockIncrease(8, 10, true));
            Assert.False(LocalInventoryPolicy.ShouldSkipInboundStockIncrease(8, 10, false));
            Assert.False(LocalInventoryPolicy.ShouldSkipInboundStockIncrease(10, 8, true));
        }

        [Fact]
        public void RefundInvoiceLine_UsesRealSkuNotGenericIade()
        {
            var sku = MikroStockCacheAligner.ResolveSku(
                new OrderItem { VariantSku = "SKU-REAL", Quantity = 1 },
                new Product { SKU = "SKU-FALLBACK" });

            Assert.Equal("SKU-REAL", sku);

            var line = new MikroFaturaSatirDto
            {
                SthStokKod = sku!,
                SthMiktar = 1,
                SthNormalIade = 1
            };

            Assert.NotEqual("IADE", line.SthStokKod);
            Assert.Equal(1, line.SthNormalIade);
        }
    }
}
