using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Order;
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
    public class RefundManagerFlowTests
    {
        private static ECommerceDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ECommerceDbContext(options);
        }

        private static RefundManager CreateSut(
            ECommerceDbContext db,
            Mock<IExtendedPaymentService> paymentMock)
        {
            var notify = new Mock<IRealTimeNotificationService>();
            notify.Setup(n => n.NotifyOrderCancelledAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            notify.Setup(n => n.NotifyOrderStatusChangedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<DateTime?>()))
                .Returns(Task.CompletedTask);

            return new RefundManager(
                db,
                paymentMock.Object,
                notify.Object,
                Mock.Of<IInventoryService>(),
                NullLogger<RefundManager>.Instance);
        }

        private static async Task<(Order order, Payments payment)> SeedCardOrderAsync(
            ECommerceDbContext db,
            OrderStatus status,
            string paymentStatus,
            string transactionType,
            DateTime? orderDateUtc = null,
            DateTime? paymentCreatedAt = null)
        {
            var order = new Order
            {
                OrderNumber = $"TEST-{Guid.NewGuid():N}".Substring(0, 16),
                UserId = 42,
                Status = status,
                PaymentMethod = "credit_card",
                PaymentStatus = paymentStatus == "Authorized"
                    ? PaymentStatus.Authorized
                    : PaymentStatus.Paid,
                CaptureStatus = paymentStatus == "Authorized"
                    ? CaptureStatus.Pending
                    : CaptureStatus.Success,
                FinalPrice = 100m,
                TotalPrice = 100m,
                CapturedAmount = paymentStatus == "Authorized" ? 0m : 100m,
                OrderDate = orderDateUtc ?? DateTime.UtcNow.AddDays(-3),
                PreAuthHostLogKey = paymentStatus == "Authorized" ? "AUTHKEY123" : null
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var payment = new Payments
            {
                OrderId = order.Id,
                Provider = "posnet",
                ProviderPaymentId = "HLK123456789",
                HostLogKey = "HLK123456789",
                Amount = 100m,
                Status = paymentStatus,
                TransactionType = transactionType,
                CreatedAt = paymentCreatedAt ?? DateTime.UtcNow.AddDays(-3)
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            return (order, payment);
        }

        [Fact]
        public async Task AdminCancel_WhenPosnetFails_DoesNotCancelOrder()
        {
            using var db = CreateDb();
            var (order, payment) = await SeedCardOrderAsync(
                db, OrderStatus.Preparing, "Paid", "sale");

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.CancelPaymentAsync(It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync(false);
            paymentMock.Setup(p => p.PartialRefundAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(false);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.AdminCancelOrderWithRefundAsync(order.Id, 1, "test fail");

            Assert.False(result.Success);
            Assert.Equal("PAYMENT_REFUND_FAILED", result.ErrorCode);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Preparing, reloaded!.Status);

            var failedReq = await db.RefundRequests.SingleAsync(r => r.OrderId == order.Id);
            Assert.Equal(RefundRequestStatus.RefundFailed, failedReq.Status);
        }

        [Fact]
        public async Task AdminCancel_WhenPosnetSucceeds_CancelsOrderAndTriggersBank()
        {
            using var db = CreateDb();
            var (order, payment) = await SeedCardOrderAsync(
                db, OrderStatus.Preparing, "Paid", "sale",
                paymentCreatedAt: DateTime.UtcNow.AddDays(-2));

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.PartialRefundAsync(payment.Id, 100m))
                .ReturnsAsync(true);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.AdminCancelOrderWithRefundAsync(order.Id, 1, "test ok");

            Assert.True(result.Success);
            paymentMock.Verify(p => p.PartialRefundAsync(payment.Id, 100m), Times.Once);
            paymentMock.Verify(p => p.CancelPaymentAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Cancelled, reloaded!.Status);
        }

        [Fact]
        public async Task AdminCancel_AuthOnly_UsesReverseNotReturn()
        {
            using var db = CreateDb();
            var (order, payment) = await SeedCardOrderAsync(
                db, OrderStatus.PreAuthorized, "Authorized", "auth",
                orderDateUtc: DateTime.UtcNow.AddDays(-1),
                paymentCreatedAt: DateTime.UtcNow.AddDays(-1));

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.CancelPaymentAsync(payment.Id, It.IsAny<string?>()))
                .ReturnsAsync(true);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.AdminCancelOrderWithRefundAsync(order.Id, 1, "auth reverse");

            Assert.True(result.Success);
            paymentMock.Verify(p => p.CancelPaymentAsync(payment.Id, It.IsAny<string?>()), Times.Once);
            paymentMock.Verify(p => p.PartialRefundAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);

            var reloaded = await db.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Cancelled, reloaded!.Status);

            var req = await db.RefundRequests.SingleAsync(r => r.OrderId == order.Id);
            Assert.Equal("reverse", req.TransactionType);
        }

        [Fact]
        public async Task CustomerAutoCancel_NextDayBeforePickup_StillAllowed()
        {
            using var db = CreateDb();
            var (order, payment) = await SeedCardOrderAsync(
                db, OrderStatus.Preparing, "Paid", "sale",
                orderDateUtc: DateTime.UtcNow.AddDays(-5),
                paymentCreatedAt: DateTime.UtcNow.AddDays(-5));

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.PartialRefundAsync(payment.Id, 100m))
                .ReturnsAsync(true);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.CreateRefundRequestAsync(order.Id, 42, new CreateRefundRequestDto
            {
                Reason = "vazgeçtim",
                RefundType = "full"
            });

            Assert.True(result.Success);
            Assert.True(result.AutoCancelled);
            Assert.Equal(OrderStatus.Cancelled, (await db.Orders.FindAsync(order.Id))!.Status);
        }

        [Fact]
        public async Task CustomerAutoCancel_WhenPosnetFails_KeepsOrderActive()
        {
            using var db = CreateDb();
            var (order, _) = await SeedCardOrderAsync(
                db, OrderStatus.Ready, "Paid", "sale");

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.CancelPaymentAsync(It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync(false);
            paymentMock.Setup(p => p.PartialRefundAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(false);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.CreateRefundRequestAsync(order.Id, 42, new CreateRefundRequestDto
            {
                Reason = "iptal",
                RefundType = "full"
            });

            Assert.False(result.Success);
            Assert.Equal("PAYMENT_REFUND_FAILED", result.ErrorCode);
            Assert.Equal(OrderStatus.Ready, (await db.Orders.FindAsync(order.Id))!.Status);
            Assert.Equal(RefundRequestStatus.RefundFailed,
                (await db.RefundRequests.SingleAsync(r => r.OrderId == order.Id)).Status);
        }

        [Fact]
        public async Task CustomerAutoCancel_AfterPickup_RequiresAdmin()
        {
            using var db = CreateDb();
            var (order, _) = await SeedCardOrderAsync(
                db, OrderStatus.PickedUp, "Paid", "sale");

            var paymentMock = new Mock<IExtendedPaymentService>();
            var sut = CreateSut(db, paymentMock);

            var result = await sut.CreateRefundRequestAsync(order.Id, 42, new CreateRefundRequestDto
            {
                Reason = "iade talebi",
                RefundType = "full"
            });

            Assert.True(result.Success);
            Assert.False(result.AutoCancelled);
            Assert.Equal(OrderStatus.PickedUp, (await db.Orders.FindAsync(order.Id))!.Status);
            Assert.Equal(RefundRequestStatus.Pending,
                (await db.RefundRequests.SingleAsync(r => r.OrderId == order.Id)).Status);
            paymentMock.Verify(p => p.CancelPaymentAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
            paymentMock.Verify(p => p.PartialRefundAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        }

        [Fact]
        public async Task AdminCancel_SameDaySale_PrefersReverse()
        {
            using var db = CreateDb();
            var (order, payment) = await SeedCardOrderAsync(
                db, OrderStatus.Preparing, "Paid", "sale",
                orderDateUtc: DateTime.UtcNow,
                paymentCreatedAt: DateTime.UtcNow);

            var paymentMock = new Mock<IExtendedPaymentService>();
            paymentMock.Setup(p => p.CancelPaymentAsync(payment.Id, It.IsAny<string?>()))
                .ReturnsAsync(true);

            var sut = CreateSut(db, paymentMock);
            var result = await sut.AdminCancelOrderWithRefundAsync(order.Id, 1, "same day");

            Assert.True(result.Success);
            paymentMock.Verify(p => p.CancelPaymentAsync(payment.Id, It.IsAny<string?>()), Times.Once);
            paymentMock.Verify(p => p.PartialRefundAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);

            var req = await db.RefundRequests.SingleAsync(r => r.OrderId == order.Id);
            Assert.Equal("reverse", req.TransactionType);
        }
    }
}
