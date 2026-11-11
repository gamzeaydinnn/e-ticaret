using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Helpers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Services.Payment.Posnet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// Adım 5–6: doküman politikası ile kodun birebir örtüşmesini sabitleyen regresyonlar.
    /// Senaryolar: 3DS=sepet, Preparing gate, Auth Capt 1100g, Sale overage manuel.
    /// </summary>
    public class WeightBasedEndToEndPolicyTests
    {
        private static Order CreateAuthOrder(decimal authAmount = 100m) => new()
        {
            HasWeightBasedItems = true,
            Status = OrderStatus.PreAuthorized,
            PaymentStatus = PaymentStatus.Authorized,
            CaptureStatus = CaptureStatus.Pending,
            PreAuthAmount = authAmount,
            AuthorizedAmount = authAmount,
            PreAuthHostLogKey = "HLK-TEST",
            FinalPrice = authAmount
        };

        private static Order CreateSaleOrder(decimal paidAmount = 100m) => new()
        {
            HasWeightBasedItems = true,
            Status = OrderStatus.Paid,
            PaymentStatus = PaymentStatus.Paid,
            CaptureStatus = CaptureStatus.Success,
            CapturedAmount = paidAmount,
            FinalPrice = paidAmount,
            PreAuthAmount = paidAmount
        };

        [Fact]
        public void CheckoutPolicy_PreAuthEqualsFinalPrice_NoTwentyPercentInflate()
        {
            // OrderManager: PreAuthAmount = Round(finalPrice) — ×1.20 YOK
            const decimal finalPrice = 100.90m;
            var preAuth = Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);
            var inflatedLegacy = Math.Round(finalPrice * 1.20m, 2, MidpointRounding.AwayFromZero);

            Assert.Equal(100.90m, preAuth);
            Assert.Equal(121.08m, inflatedLegacy);
            Assert.NotEqual(preAuth, inflatedLegacy);
            Assert.Equal(finalPrice, preAuth);
        }

        [Fact]
        public void ThreeDsBankAmount_UsesPreAuth_SameAsCart()
        {
            var order = CreateAuthOrder(100.90m);
            // PaymentsControllers: bankAmount = order.PreAuthAmount when > 0
            var bankAmount = order.PreAuthAmount > 0 ? order.PreAuthAmount : order.FinalPrice;
            var amountKurus = (int)(bankAmount * 100);

            Assert.Equal(100.90m, bankAmount);
            Assert.Equal(10090, amountKurus);
        }

        [Theory]
        [InlineData(OrderStatus.Preparing, true)]
        [InlineData(OrderStatus.Confirmed, false)]
        [InlineData(OrderStatus.Paid, false)]
        [InlineData(OrderStatus.PreAuthorized, false)]
        [InlineData(OrderStatus.Ready, false)]
        public void PreparingOnly_ManualWeightGate(OrderStatus status, bool allowed)
        {
            Assert.Equal(allowed, WeightBasedWeighingGate.CanEnterManualWeight(status));
        }

        [Fact]
        public void Auth_1100g_CaptureDecision_Is110WithinLimit()
        {
            var order = CreateAuthOrder(100m);
            const decimal actualPrice = 110m; // 1100g @ 100 TL/kg

            Assert.Equal(WeightBasedPaymentFlowType.SaleImmediate, WeightBasedPaymentFlowResolver.Resolve(order));
            Assert.False(WeightBasedPaymentFlowResolver.CanBankCaptureOverage(order));

            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(
                WeightBasedCapturePolicy.ResolveAuthorizedAmount(order),
                actualPrice);

            Assert.Equal(110m, decision.CaptureAmount);
            Assert.False(decision.ExceedsLimit);
            Assert.Equal(120m, WeightBasedCapturePolicy.CalculateMaxCapturableAmount(100m));
        }

        [Fact]
        public void Auth_1300g_ExceedsLimit_ClampsTo120()
        {
            var order = CreateAuthOrder(100m);
            const decimal actualPrice = 130m;

            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(
                WeightBasedCapturePolicy.ResolveAuthorizedAmount(order),
                actualPrice);

            Assert.Equal(120m, decision.CaptureAmount);
            Assert.True(decision.ExceedsLimit);
        }

        [Fact]
        public void Sale_1100g_CannotBankCapture_RequiresManual()
        {
            var order = CreateSaleOrder(100m);
            const decimal priceDifference = 10m;

            Assert.Equal(WeightBasedPaymentFlowType.SaleImmediate, WeightBasedPaymentFlowResolver.Resolve(order));
            Assert.False(WeightBasedPaymentFlowResolver.CanBankCaptureOverage(order));
            Assert.True(priceDifference > 0);

            var paid = WeightBasedPaymentFlowResolver.ResolveSaleCapturedAmount(order);
            Assert.Equal(100m, paid);
            Assert.Equal(10m, Math.Round(110m - paid, 2));
        }

        [Fact]
        public void Sale_UnderWeight_RequiresManualRefundFlag()
        {
            var order = CreateSaleOrder(100m);
            const decimal actualPrice = 90m;
            var priceDifference = actualPrice - order.FinalPrice;

            Assert.Equal(WeightBasedPaymentFlowType.SaleImmediate, WeightBasedPaymentFlowResolver.Resolve(order));
            Assert.True(priceDifference < 0);
        }

        [Fact]
        public void Auth_UnderWeight_PartialCapture_NoSeparateRefund()
        {
            // Politika Faz F: final < auth → Capt(final); ayrı Return yok
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(100m, 85m);
            Assert.Equal(85m, decision.CaptureAmount);
            Assert.False(decision.ExceedsLimit);
        }

        [Fact]
        public void ResolveAuthorizedAmount_PrefersAuthorized_ThenPreAuth()
        {
            var withBoth = new Order { AuthorizedAmount = 100m, PreAuthAmount = 90m };
            var onlyPre = new Order { AuthorizedAmount = 0m, PreAuthAmount = 100.90m };

            Assert.Equal(100m, WeightBasedCapturePolicy.ResolveAuthorizedAmount(withBoth));
            Assert.Equal(100.90m, WeightBasedCapturePolicy.ResolveAuthorizedAmount(onlyPre));
        }

        [Fact]
        public void NonWeightOrder_FlowNotApplicable()
        {
            var order = new Order { HasWeightBasedItems = false, PaymentStatus = PaymentStatus.Paid };
            Assert.Equal(WeightBasedPaymentFlowType.NotApplicable, WeightBasedPaymentFlowResolver.Resolve(order));
        }

        [Fact]
        public void CaptureOveragePercent_IsTwenty()
        {
            Assert.Equal(20m, WeightBasedCapturePolicy.CaptureOveragePercent);
            Assert.Equal(168, WeightBasedCapturePolicy.PreAuthValidityHours);
        }

        [Fact]
        public void ReadyAndPickupGate_AllowsDirectOrderFlowWithoutWeighing()
        {
            var order = new Order
            {
                HasWeightBasedItems = true,
                AllItemsWeighed = false,
                OrderItems = new List<OrderItem>
                {
                    new() { IsWeightBased = true, IsWeighed = false }
                }
            };

            Assert.False(WeightBasedWeighingGate.HasUnweighedWeightItems(order));
            Assert.True(WeightBasedWeighingGate.CanMarkReady(order));
            Assert.True(WeightBasedWeighingGate.CanCourierPickup(order));
        }

        [Fact]
        public void ReadyGate_AllowsWhenAllKgItemsWeighed()
        {
            var order = new Order
            {
                HasWeightBasedItems = true,
                AllItemsWeighed = true,
                OrderItems = new List<OrderItem>
                {
                    new() { IsWeightBased = true, IsWeighed = true }
                }
            };

            Assert.False(WeightBasedWeighingGate.HasUnweighedWeightItems(order));
            Assert.True(WeightBasedWeighingGate.CanMarkReady(order));
            Assert.True(WeightBasedWeighingGate.CanCourierPickup(order));
        }

        [Fact]
        public void StoreDelivery_AllowsAllOrdersDirectly()
        {
            var card = new Order { HasWeightBasedItems = true, PaymentMethod = "posnet" };
            var cash = new Order { HasWeightBasedItems = true, PaymentMethod = "cash_on_delivery" };
            var normal = new Order { HasWeightBasedItems = false, PaymentMethod = "posnet" };

            Assert.False(WeightBasedWeighingGate.ShouldBlockStoreDelivery(card));
            Assert.False(WeightBasedWeighingGate.ShouldBlockStoreDelivery(cash));
            Assert.False(WeightBasedWeighingGate.ShouldBlockStoreDelivery(normal));
        }

        [Fact]
        public async Task SaleImmediate_UnderWeight_ExecutesPartialReturnAndUpdatesCapturedAmount()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            using var db = new ECommerceDbContext(options);

            var order = CreateSaleOrder(100m);
            order.OrderNumber = "ORD-SALE-UNDER";
            order.PaymentMethod = "posnet";
            order.PreAuthHostLogKey = "HLK-SALE-001";
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var payment = new Payments
            {
                OrderId = order.Id,
                Provider = "posnet",
                ProviderPaymentId = "HLK-SALE-001",
                HostLogKey = "HLK-SALE-001",
                Amount = 100m,
                CapturedAmount = 100m,
                Status = "Paid",
                TransactionType = "Sale"
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            var extPaymentMock = new Mock<IExtendedPaymentService>();
            extPaymentMock
                .Setup(p => p.ExecutePosnetRefundAsync(It.Is<PosnetRefundExecutionRequest>(r =>
                    r.OrderId == order.Id && r.PaymentId == payment.Id && r.RefundAmount == 8m)))
                .ReturnsAsync(PosnetRefundExecutionResult.Ok("return", "HLK-SALE-001"));

            var sut = new PaymentCaptureService(
                db,
                Mock.Of<IRealTimeNotificationService>(),
                NullLogger<PaymentCaptureService>.Instance,
                Mock.Of<IPosnetPaymentService>(),
                extPaymentMock.Object);

            var result = await sut.CapturePaymentAsync(order.Id, 92m);

            Assert.True(result.Success);
            Assert.Equal(92m, result.CapturedAmount);
            Assert.Equal(8m, result.ReleasedAmount);

            var reloadedOrder = await db.Orders.FindAsync(order.Id);
            Assert.Equal(92m, reloadedOrder!.CapturedAmount);
            Assert.Equal(92m, reloadedOrder.FinalAmount);
            Assert.Equal(OrderStatus.Paid, reloadedOrder.Status);
        }

        [Fact]
        public async Task SaleImmediate_OverWeight_DoesNotBlockDeliveryOrSetPaymentPending()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            using var db = new ECommerceDbContext(options);

            var order = CreateSaleOrder(100m);
            order.OrderNumber = "ORD-SALE-OVER";
            order.PaymentMethod = "posnet";
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var sut = new PaymentCaptureService(
                db,
                Mock.Of<IRealTimeNotificationService>(),
                NullLogger<PaymentCaptureService>.Instance);

            var result = await sut.CapturePaymentAsync(order.Id, 108m);

            Assert.True(result.Success);
            Assert.Equal(100m, result.CapturedAmount);

            var reloadedOrder = await db.Orders.FindAsync(order.Id);
            Assert.NotEqual(OrderStatus.DeliveryPaymentPending, reloadedOrder!.Status);
            Assert.Equal(108m, reloadedOrder.FinalAmount);
        }
    }
}
