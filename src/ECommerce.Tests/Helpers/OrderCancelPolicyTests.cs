using System;
using ECommerce.Core.Helpers;
using ECommerce.Entities.Enums;
using Xunit;

namespace ECommerce.Tests.Helpers
{
    public class OrderCancelPolicyTests
    {
        [Theory]
        [InlineData(OrderStatus.New)]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Preparing)]
        [InlineData(OrderStatus.Processing)]
        [InlineData(OrderStatus.Ready)]
        [InlineData(OrderStatus.ReadyForPickup)]
        [InlineData(OrderStatus.Assigned)]
        [InlineData(OrderStatus.PreAuthorized)]
        [InlineData(OrderStatus.WeightPending)]
        public void GetCancelMode_BeforePickup_IsAuto_RegardlessOfDay(OrderStatus status)
        {
            var yesterdayUtc = DateTime.UtcNow.AddDays(-2);
            var nextWeekUtc = DateTime.UtcNow.AddDays(-10);

            Assert.Equal(OrderCancelPolicy.CancelModeAuto,
                OrderCancelPolicy.GetCancelMode(status, yesterdayUtc));
            Assert.Equal(OrderCancelPolicy.CancelModeAuto,
                OrderCancelPolicy.GetCancelMode(status, nextWeekUtc));
            Assert.True(OrderCancelPolicy.CanCustomerAutoCancel(status, yesterdayUtc));
        }

        [Theory]
        [InlineData(OrderStatus.PickedUp)]
        [InlineData(OrderStatus.InTransit)]
        [InlineData(OrderStatus.OutForDelivery)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Completed)]
        [InlineData(OrderStatus.PartialRefund)]
        public void GetCancelMode_AfterPickup_IsWhatsApp(OrderStatus status)
        {
            Assert.Equal(OrderCancelPolicy.CancelModeWhatsApp,
                OrderCancelPolicy.GetCancelMode(status, DateTime.UtcNow));
            Assert.False(OrderCancelPolicy.CanCustomerAutoCancel(status, DateTime.UtcNow));
        }

        [Theory]
        [InlineData(OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Refunded)]
        public void GetCancelMode_Terminal_IsNone(OrderStatus status)
        {
            Assert.Equal(OrderCancelPolicy.CancelModeNone,
                OrderCancelPolicy.GetCancelMode(status, DateTime.UtcNow));
        }
    }
}
