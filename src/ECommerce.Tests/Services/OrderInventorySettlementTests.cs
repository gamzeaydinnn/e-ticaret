using System;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class OrderInventorySettlementTests
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
        public async Task SettlePaymentSuccess_CommitsReservation_AndMarksInventoryCommitted()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Settle", StockQuantity = 10 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var clientOrderId = Guid.NewGuid();
            var order = new Order
            {
                OrderNumber = "ORD-S1",
                ClientOrderId = clientOrderId,
                Status = OrderStatus.Paid,
                IsInventoryCommitted = false
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 2,
                UnitPrice = 10
            });
            db.StockReservations.Add(new StockReservation
            {
                ClientOrderId = clientOrderId,
                ProductId = product.Id,
                Quantity = 2,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsReleased = false
            });
            await db.SaveChangesAsync();

            var inventory = new Mock<IInventoryService>();
            inventory.Setup(i => i.CommitReservationAsync(clientOrderId)).Returns(Task.CompletedTask);
            var log = new Mock<IInventoryLogService>();

            var sut = new OrderInventorySettlementService(
                db, inventory.Object, log.Object, NullLogger<OrderInventorySettlementService>.Instance);

            await sut.SettlePaymentSuccessAsync(order.Id);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.True(reloaded!.IsInventoryCommitted);
            inventory.Verify(i => i.CommitReservationAsync(clientOrderId), Times.Once);
        }

        [Fact]
        public async Task SettlePaymentFailure_ReleasesReservation_WhenNotCommitted()
        {
            using var db = CreateDb();
            var clientOrderId = Guid.NewGuid();
            var order = new Order
            {
                OrderNumber = "ORD-F1",
                ClientOrderId = clientOrderId,
                Status = OrderStatus.Pending,
                IsInventoryCommitted = false
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var inventory = new Mock<IInventoryService>();
            inventory.Setup(i => i.ReleaseReservationAsync(clientOrderId)).Returns(Task.CompletedTask);
            var log = new Mock<IInventoryLogService>();

            var sut = new OrderInventorySettlementService(
                db, inventory.Object, log.Object, NullLogger<OrderInventorySettlementService>.Instance);

            await sut.SettlePaymentFailureAsync(order.Id);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.PaymentFailed, reloaded!.Status);
            Assert.False(reloaded.IsInventoryCommitted);
            inventory.Verify(i => i.ReleaseReservationAsync(clientOrderId), Times.Once);
            inventory.Verify(i => i.RestoreOrderStockAsync(
                It.IsAny<System.Collections.Generic.IEnumerable<ECommerce.Core.DTOs.Inventory.OrderStockRestoreLineDto>>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Faz0_HybridToErp_PolicyFlags()
        {
            Assert.True(LocalInventoryPolicy.UseSiparisDocumentForOnlineSaleStock);
            Assert.False(LocalInventoryPolicy.PushOutboundOnOrderStockRestore);
        }
    }
}
