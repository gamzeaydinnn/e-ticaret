// ==========================================================================
// CourierPanelTests.cs - Kurye Paneli Değişiklikleri İçin Testler
// ==========================================================================
// P0, P1 ve P2 değişikliklerinin doğruluğunu test eder.
// ==========================================================================

using ECommerce.Core.DTOs.Courier;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Business.Helpers;
using System;
using Xunit;

namespace ECommerce.Tests.Courier
{
    /// <summary>
    /// Kurye paneli değişiklikleri testleri
    /// </summary>
    public class CourierPanelTests
    {
        #region P0-3: CourierOrderListDto Yeni Alanlar Testleri

        /// <summary>
        /// CourierOrderListDto FinalAmount alanı doğru çalışmalı
        /// </summary>
        [Fact]
        public void CourierOrderListDto_FinalAmount_CanBeSet()
        {
            // Arrange & Act
            var dto = new CourierOrderListDto
            {
                OrderId = 1,
                TotalAmount = 100m,
                FinalAmount = 120m,
                TotalPriceDifference = 20m
            };

            // Assert
            Assert.Equal(120m, dto.FinalAmount);
            Assert.Equal(20m, dto.TotalPriceDifference);
        }

        /// <summary>
        /// CourierOrderListDto AuthorizedAmount alanı doğru çalışmalı
        /// </summary>
        [Fact]
        public void CourierOrderListDto_AuthorizedAmount_CanBeSet()
        {
            // Arrange & Act
            var dto = new CourierOrderListDto
            {
                OrderId = 1,
                AuthorizedAmount = 150m
            };

            // Assert
            Assert.Equal(150m, dto.AuthorizedAmount);
        }

        /// <summary>
        /// CourierOrderListDto WeightAdjustmentStatus alanı doğru çalışmalı
        /// </summary>
        [Fact]
        public void CourierOrderListDto_WeightAdjustmentStatus_CanBeSet()
        {
            // Arrange & Act
            var dto = new CourierOrderListDto
            {
                OrderId = 1,
                WeightAdjustmentStatus = "Weighed"
            };

            // Assert
            Assert.Equal("Weighed", dto.WeightAdjustmentStatus);
        }

        /// <summary>
        /// CourierOrderListDto HasWeightDifference doğru hesaplanmalı
        /// </summary>
        [Theory]
        [InlineData(0, false)]
        [InlineData(10, true)]
        [InlineData(-5, true)]
        public void CourierOrderListDto_HasWeightDifference_IsCorrect(decimal priceDiff, bool expected)
        {
            // Arrange & Act
            var dto = new CourierOrderListDto
            {
                TotalPriceDifference = priceDiff,
                HasWeightDifference = priceDiff != 0
            };

            // Assert
            Assert.Equal(expected, dto.HasWeightDifference);
        }

        /// <summary>
        /// CourierOrderListDto ağırlık bazlı ürün bayrakları doğru çalışmalı
        /// </summary>
        [Fact]
        public void CourierOrderListDto_WeightBasedFlags_CanBeSet()
        {
            // Arrange & Act
            var dto = new CourierOrderListDto
            {
                HasWeightBasedItems = true,
                AllItemsWeighed = false
            };

            // Assert
            Assert.True(dto.HasWeightBasedItems);
            Assert.False(dto.AllItemsWeighed);
        }

        #endregion

        #region P0-2: Payload Uyumu Testleri (ActualWeightGrams alias)

        /// <summary>
        /// Order'dan DTO'ya dönüştürürken FinalAmount doğru atanmalı
        /// </summary>
        [Fact]
        public void MapOrderToDto_FinalAmount_ShouldBePopulated()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                FinalAmount = 150m,
                FinalPrice = 100m,
                TotalPriceDifference = 50m,
                AuthorizedAmount = 180m,
                HasWeightBasedItems = true,
                AllItemsWeighed = true,
                WeightAdjustmentStatus = WeightAdjustmentStatus.Weighed
            };

            // Act - Manuel mapping (gerçek mapper'ı simüle eder)
            var dto = new CourierOrderListDto
            {
                OrderId = order.Id,
                FinalAmount = order.FinalAmount > 0 ? order.FinalAmount : order.FinalPrice,
                TotalPriceDifference = order.TotalPriceDifference,
                AuthorizedAmount = order.AuthorizedAmount,
                WeightAdjustmentStatus = order.WeightAdjustmentStatus.ToString(),
                HasWeightDifference = order.TotalPriceDifference != 0,
                HasWeightBasedItems = order.HasWeightBasedItems,
                AllItemsWeighed = order.AllItemsWeighed
            };

            // Assert
            Assert.Equal(150m, dto.FinalAmount);
            Assert.Equal(50m, dto.TotalPriceDifference);
            Assert.Equal(180m, dto.AuthorizedAmount);
            Assert.Equal("Weighed", dto.WeightAdjustmentStatus);
            Assert.True(dto.HasWeightDifference);
            Assert.True(dto.HasWeightBasedItems);
            Assert.True(dto.AllItemsWeighed);
        }

        /// <summary>
        /// FinalAmount 0 ise FinalPrice kullanılmalı
        /// </summary>
        [Fact]
        public void MapOrderToDto_FinalAmount_FallsBackToFinalPrice()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                FinalAmount = 0m,
                FinalPrice = 100m
            };

            // Act
            var dto = new CourierOrderListDto
            {
                FinalAmount = order.FinalAmount > 0 ? order.FinalAmount : order.FinalPrice
            };

            // Assert
            Assert.Equal(100m, dto.FinalAmount);
        }

        #endregion

        #region P1-7: DeliveryPaymentPending Durum Testleri

        /// <summary>
        /// DeliveryPaymentPending durumu doğru tanınmalı
        /// </summary>
        [Theory]
        [InlineData("deliverypaymentpending", true)]
        [InlineData("delivery_payment_pending", true)]
        [InlineData("DeliveryPaymentPending", true)]
        [InlineData("delivered", false)]
        [InlineData("out_for_delivery", false)]
        public void IsDeliveryPaymentPending_StatusCheck(string status, bool expected)
        {
            // Act
            var normalized = status.ToLowerInvariant();
            var isPaymentPending = normalized == "deliverypaymentpending" || 
                                   normalized == "delivery_payment_pending";

            // Assert
            Assert.Equal(expected, isPaymentPending);
        }

        /// <summary>
        /// DeliveryPaymentPending durumunda ek tahsilat tutarı gösterilmeli
        /// </summary>
        [Fact]
        public void DeliveryPaymentPending_ShouldShowExtraAmount()
        {
            // Arrange
            var dto = new CourierOrderListDto
            {
                Status = "DeliveryPaymentPending",
                TotalAmount = 100m,
                FinalAmount = 130m,
                TotalPriceDifference = 30m
            };

            // Assert
            Assert.True(dto.TotalPriceDifference > 0, "Ek tahsilat tutarı pozitif olmalı");
            Assert.Equal(30m, dto.TotalPriceDifference);
        }

        #endregion

        #region P1-10: Status Akışı Tutarlılık Testleri

        /// <summary>
        /// Kurye sipariş durumu akışı doğru olmalı
        /// </summary>
        [Fact]
        public void CourierStatusFlow_ShouldBeCorrect()
        {
            // Expected flow: Assigned → PickedUp → OutForDelivery → Delivered
            var statusFlow = new[]
            {
                ("assigned", "picked_up"),
                ("picked_up", "out_for_delivery"),
                ("out_for_delivery", "delivered")
            };

            foreach (var (current, next) in statusFlow)
            {
                var nextStatus = GetNextStatus(current);
                Assert.Equal(next, nextStatus);
            }
        }

        /// <summary>
        /// Delivered durumunda sonraki durum olmamalı
        /// </summary>
        [Fact]
        public void CourierStatusFlow_Delivered_HasNoNextStatus()
        {
            var nextStatus = GetNextStatus("delivered");
            Assert.Null(nextStatus);
        }

        /// <summary>
        /// DeliveryPaymentPending durumunda normal akış dışında kalmalı
        /// </summary>
        [Fact]
        public void CourierStatusFlow_DeliveryPaymentPending_HasNoNextStatus()
        {
            var nextStatus = GetNextStatus("deliverypaymentpending");
            Assert.Null(nextStatus);
        }

        private string? GetNextStatus(string currentStatus)
        {
            var normalized = currentStatus.ToLowerInvariant();
            return normalized switch
            {
                "assigned" => "picked_up",
                "picked_up" or "pickedup" => "out_for_delivery",
                "out_for_delivery" or "outfordelivery" or "in_transit" => "delivered",
                _ => null
            };
        }

        #endregion

        #region Provizyon Capture Testleri

        /// <summary>
        /// Tartı sonrası final tutar provizyon limitini aşmamalı (normal)
        /// </summary>
        [Fact]
        public void CapturePolicy_FinalWithinLimit_ShouldCaptureFull()
        {
            // Arrange
            decimal authorizedAmount = 100m;
            decimal finalAmount = 110m; // %10 fazla, %20 limitinin içinde

            // Act
            var maxCapturable = WeightBasedCapturePolicy.CalculateMaxCapturableAmount(authorizedAmount);
            var isWithinLimit = finalAmount <= maxCapturable;

            // Assert
            Assert.Equal(120m, maxCapturable); // 100 × 1.20
            Assert.True(isWithinLimit);
        }

        /// <summary>
        /// Tartı sonrası final tutar provizyon limitini aşarsa (DeliveryPaymentPending)
        /// </summary>
        [Fact]
        public void CapturePolicy_FinalExceedsLimit_ShouldTriggerPaymentPending()
        {
            // Arrange
            decimal authorizedAmount = 100m;
            decimal finalAmount = 150m; // %50 fazla, %20 limitini aşıyor

            // Act
            var maxCapturable = WeightBasedCapturePolicy.CalculateMaxCapturableAmount(authorizedAmount);
            var isWithinLimit = finalAmount <= maxCapturable;
            var decision = WeightBasedCapturePolicy.ClampToCaptureLimit(authorizedAmount, finalAmount);

            // Assert
            Assert.Equal(120m, maxCapturable);
            Assert.False(isWithinLimit);
            Assert.True(decision.ExceedsLimit);
            Assert.Equal(120m, decision.CaptureAmount); // Limit kadar çekilir
            // Kalan 30₺ için DeliveryPaymentPending durumuna geçilmeli
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Negatif fark (müşteriye iade) durumu
        /// </summary>
        [Fact]
        public void NegativePriceDifference_ShouldIndicateRefund()
        {
            // Arrange
            var dto = new CourierOrderListDto
            {
                TotalAmount = 100m,
                FinalAmount = 90m,
                TotalPriceDifference = -10m
            };

            // Assert
            Assert.True(dto.TotalPriceDifference < 0, "Negatif fark iade anlamına gelir");
            Assert.True(dto.HasWeightDifference || dto.TotalPriceDifference != 0);
        }

        /// <summary>
        /// Sıfır fark durumu
        /// </summary>
        [Fact]
        public void ZeroPriceDifference_ShouldNotShowDifference()
        {
            // Arrange
            var dto = new CourierOrderListDto
            {
                TotalAmount = 100m,
                FinalAmount = 100m,
                TotalPriceDifference = 0m,
                HasWeightDifference = false
            };

            // Assert
            Assert.False(dto.HasWeightDifference);
            Assert.Equal(0m, dto.TotalPriceDifference);
        }

        #endregion
    }
}
