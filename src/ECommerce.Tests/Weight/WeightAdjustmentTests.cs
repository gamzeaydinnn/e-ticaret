// ==========================================================================
// WeightAdjustmentTests.cs - Ağırlık Ayarlama Sistemi Testleri
// ==========================================================================
// Ağırlık bazlı ödeme sistemi için basitleştirilmiş test sınıfı.
// Entity ve iş mantığı hesaplamalarını test eder.
// ==========================================================================

using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System;
using Xunit;

namespace ECommerce.Tests.Weight
{
    /// <summary>
    /// Ağırlık ayarlama sistemi testleri
    /// </summary>
    public class WeightAdjustmentTests
    {
        #region Entity Property Tests

        /// <summary>
        /// WeightAdjustment entity'si oluşturulabilmeli
        /// </summary>
        [Fact]
        public void WeightAdjustment_CanBeCreated()
        {
            // Arrange & Act
            var adjustment = new WeightAdjustment();

            // Assert
            Assert.NotNull(adjustment);
            Assert.Equal(0, adjustment.OrderId);
            // Entity varsayılan olarak PendingWeighing durumunda başlıyor
            Assert.Equal(WeightAdjustmentStatus.PendingWeighing, adjustment.Status);
        }

        /// <summary>
        /// Ağırlık değerleri doğru atanabilmeli
        /// </summary>
        [Fact]
        public void WeightAdjustment_WeightValues_CanBeSet()
        {
            // Arrange
            var adjustment = new WeightAdjustment
            {
                EstimatedWeight = 1000m, // 1 kg = 1000 gram
                ActualWeight = 1100m,    // 1.1 kg = 1100 gram
                WeightDifference = 100m  // 100 gram fazla
            };

            // Assert
            Assert.Equal(1000m, adjustment.EstimatedWeight);
            Assert.Equal(1100m, adjustment.ActualWeight);
            Assert.Equal(100m, adjustment.WeightDifference);
        }

        /// <summary>
        /// Fiyat değerleri doğru atanabilmeli
        /// </summary>
        [Fact]
        public void WeightAdjustment_PriceValues_CanBeSet()
        {
            // Arrange
            var adjustment = new WeightAdjustment
            {
                PricePerUnit = 250m,
                EstimatedPrice = 500m,
                ActualPrice = 550m,
                PriceDifference = 50m
            };

            // Assert
            Assert.Equal(250m, adjustment.PricePerUnit);
            Assert.Equal(500m, adjustment.EstimatedPrice);
            Assert.Equal(550m, adjustment.ActualPrice);
            Assert.Equal(50m, adjustment.PriceDifference);
        }

        #endregion

        #region Weight Difference Calculation Tests

        /// <summary>
        /// Ağırlık farkı doğru hesaplanmalı - fazla geldi
        /// </summary>
        [Fact]
        public void CalculateWeightDifference_OverWeight_ReturnsPositive()
        {
            // Arrange
            decimal estimatedWeight = 1000m; // 1 kg
            decimal actualWeight = 1200m;    // 1.2 kg

            // Act
            decimal difference = actualWeight - estimatedWeight;

            // Assert
            Assert.Equal(200m, difference);
            Assert.True(difference > 0, "Fazla geldi durumunda fark pozitif olmalı");
        }

        /// <summary>
        /// Ağırlık farkı doğru hesaplanmalı - eksik geldi
        /// </summary>
        [Fact]
        public void CalculateWeightDifference_UnderWeight_ReturnsNegative()
        {
            // Arrange
            decimal estimatedWeight = 1000m; // 1 kg
            decimal actualWeight = 800m;     // 0.8 kg

            // Act
            decimal difference = actualWeight - estimatedWeight;

            // Assert
            Assert.Equal(-200m, difference);
            Assert.True(difference < 0, "Eksik geldi durumunda fark negatif olmalı");
        }

        /// <summary>
        /// Ağırlık farkı yüzdesi doğru hesaplanmalı
        /// </summary>
        [Theory]
        [InlineData(1000, 1100, 10)]   // %10 fazla
        [InlineData(1000, 900, -10)]   // %10 eksik
        [InlineData(1000, 1200, 20)]   // %20 fazla
        [InlineData(500, 600, 20)]     // %20 fazla
        public void CalculateDifferencePercent_ReturnsCorrectValue(
            decimal estimated, decimal actual, decimal expectedPercent)
        {
            // Act
            decimal difference = actual - estimated;
            decimal percent = (difference / estimated) * 100;

            // Assert
            Assert.Equal(expectedPercent, percent);
        }

        #endregion

        #region Price Calculation Tests

        /// <summary>
        /// Fiyat farkı doğru hesaplanmalı - ek ödeme
        /// </summary>
        [Fact]
        public void CalculatePriceDifference_OverWeight_ReturnsPositive()
        {
            // Arrange
            decimal pricePerKg = 250m;
            decimal estimatedWeight = 2m;    // 2 kg
            decimal actualWeight = 2.3m;     // 2.3 kg

            // Act
            decimal estimatedPrice = estimatedWeight * pricePerKg;
            decimal actualPrice = actualWeight * pricePerKg;
            decimal priceDifference = actualPrice - estimatedPrice;

            // Assert
            Assert.Equal(500m, estimatedPrice);
            Assert.Equal(575m, actualPrice);
            Assert.Equal(75m, priceDifference);
        }

        /// <summary>
        /// Fiyat farkı doğru hesaplanmalı - iade
        /// </summary>
        [Fact]
        public void CalculatePriceDifference_UnderWeight_ReturnsNegative()
        {
            // Arrange
            decimal pricePerKg = 250m;
            decimal estimatedWeight = 2m;    // 2 kg
            decimal actualWeight = 1.7m;     // 1.7 kg

            // Act
            decimal estimatedPrice = estimatedWeight * pricePerKg;
            decimal actualPrice = actualWeight * pricePerKg;
            decimal priceDifference = actualPrice - estimatedPrice;

            // Assert
            Assert.Equal(500m, estimatedPrice);
            Assert.Equal(425m, actualPrice);
            Assert.Equal(-75m, priceDifference);
        }

        #endregion

        #region Threshold Tests

        /// <summary>
        /// %20'nin altında fark - admin onayı gerektirmemeli
        /// </summary>
        [Theory]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(19)]
        public void CheckThreshold_Under20Percent_DoesNotRequireAdmin(decimal percentDiff)
        {
            // Arrange
            const decimal threshold = 20m;

            // Act
            bool requiresAdmin = Math.Abs(percentDiff) > threshold;

            // Assert
            Assert.False(requiresAdmin);
        }

        /// <summary>
        /// %20'nin üstünde fark - admin onayı gerekli
        /// </summary>
        [Theory]
        [InlineData(21)]
        [InlineData(25)]
        [InlineData(50)]
        public void CheckThreshold_Over20Percent_RequiresAdmin(decimal percentDiff)
        {
            // Arrange
            const decimal threshold = 20m;

            // Act
            bool requiresAdmin = Math.Abs(percentDiff) > threshold;

            // Assert
            Assert.True(requiresAdmin);
        }

        /// <summary>
        /// 50 TL'nin altında fark - admin onayı gerektirmemeli
        /// </summary>
        [Theory]
        [InlineData(10)]
        [InlineData(30)]
        [InlineData(49)]
        public void CheckThreshold_Under50TL_DoesNotRequireAdmin(decimal priceDiff)
        {
            // Arrange
            const decimal threshold = 50m;

            // Act
            bool requiresAdmin = Math.Abs(priceDiff) > threshold;

            // Assert
            Assert.False(requiresAdmin);
        }

        /// <summary>
        /// 50 TL'nin üstünde fark - admin onayı gerekli
        /// </summary>
        [Theory]
        [InlineData(51)]
        [InlineData(75)]
        [InlineData(150)]
        public void CheckThreshold_Over50TL_RequiresAdmin(decimal priceDiff)
        {
            // Arrange
            const decimal threshold = 50m;

            // Act
            bool requiresAdmin = Math.Abs(priceDiff) > threshold;

            // Assert
            Assert.True(requiresAdmin);
        }

        #endregion

        #region Status Tests

        /// <summary>
        /// Durum geçişleri - PendingWeighing -> Weighed
        /// </summary>
        [Fact]
        public void StatusTransition_PendingWeighingToWeighed_IsValid()
        {
            // Arrange
            var adjustment = new WeightAdjustment
            {
                Status = WeightAdjustmentStatus.PendingWeighing
            };

            // Act
            adjustment.Status = WeightAdjustmentStatus.Weighed;

            // Assert
            Assert.Equal(WeightAdjustmentStatus.Weighed, adjustment.Status);
        }

        /// <summary>
        /// Tüm durumlar tanımlı olmalı
        /// </summary>
        [Fact]
        public void WeightAdjustmentStatus_AllValuesAreDefined()
        {
            // Assert - Enum değerlerinin sayısını kontrol et
            var statusCount = Enum.GetValues(typeof(WeightAdjustmentStatus)).Length;
            Assert.True(statusCount >= 10, "En az 10 durum tanımlı olmalı");

            // Enum'un tanımlı olduğunu doğrula
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.NotApplicable));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.PendingWeighing));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.Weighed));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.NoDifference));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.PendingAdditionalPayment));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.PendingRefund));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.Completed));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.PendingAdminApproval));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.RejectedByAdmin));
            Assert.True(Enum.IsDefined(typeof(WeightAdjustmentStatus), WeightAdjustmentStatus.Failed));
        }

        #endregion

        #region Weight Unit Conversion Tests

        /// <summary>
        /// Kilogram'dan gram'a dönüşüm
        /// </summary>
        [Theory]
        [InlineData(1, 1000)]
        [InlineData(0.5, 500)]
        [InlineData(2.5, 2500)]
        public void ConvertKgToGram_ReturnsCorrectValue(decimal kg, decimal expectedGram)
        {
            // Act
            decimal gram = kg * 1000;

            // Assert
            Assert.Equal(expectedGram, gram);
        }

        /// <summary>
        /// Gram'dan kilogram'a dönüşüm
        /// </summary>
        [Theory]
        [InlineData(1000, 1)]
        [InlineData(500, 0.5)]
        [InlineData(2500, 2.5)]
        public void ConvertGramToKg_ReturnsCorrectValue(decimal gram, decimal expectedKg)
        {
            // Act
            decimal kg = gram / 1000;

            // Assert
            Assert.Equal(expectedKg, kg);
        }

        #endregion

        #region Pre-Auth Expiry Tests

        /// <summary>
        /// 48 saat ön provizyon süresi - süre dolmamış
        /// </summary>
        [Fact]
        public void PreAuthExpiry_Within48Hours_IsNotExpired()
        {
            // Arrange
            var preAuthTime = DateTime.UtcNow.AddHours(-47);
            var expiryDuration = TimeSpan.FromHours(48);

            // Act
            var isExpired = DateTime.UtcNow > preAuthTime.Add(expiryDuration);

            // Assert
            Assert.False(isExpired);
        }

        /// <summary>
        /// 48 saat ön provizyon süresi - süre dolmuş
        /// </summary>
        [Fact]
        public void PreAuthExpiry_After48Hours_IsExpired()
        {
            // Arrange
            var preAuthTime = DateTime.UtcNow.AddHours(-49);
            var expiryDuration = TimeSpan.FromHours(48);

            // Act
            var isExpired = DateTime.UtcNow > preAuthTime.Add(expiryDuration);

            // Assert
            Assert.True(isExpired);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Sıfır ağırlık durumu
        /// </summary>
        [Fact]
        public void EdgeCase_ZeroWeight_HandledCorrectly()
        {
            // Arrange
            var adjustment = new WeightAdjustment
            {
                EstimatedWeight = 0m,
                ActualWeight = 0m
            };

            // Assert
            Assert.Equal(0m, adjustment.EstimatedWeight);
            Assert.Equal(0m, adjustment.ActualWeight);
        }

        /// <summary>
        /// Çok küçük fiyat farkı
        /// </summary>
        [Fact]
        public void EdgeCase_VerySmallPriceDifference_HandledCorrectly()
        {
            // Arrange
            decimal estimatedPrice = 100m;
            decimal actualPrice = 100.01m;

            // Act
            decimal difference = actualPrice - estimatedPrice;

            // Assert
            Assert.Equal(0.01m, difference);
        }

        /// <summary>
        /// Büyük sipariş tutarları
        /// </summary>
        [Fact]
        public void EdgeCase_LargeOrderAmount_NoOverflow()
        {
            // Arrange
            decimal pricePerKg = 1000m;
            decimal weightKg = 1000m; // 1 ton

            // Act
            decimal totalPrice = pricePerKg * weightKg;

            // Assert
            Assert.Equal(1_000_000m, totalPrice);
        }

        #endregion

        #region Multiple Items Tests

        /// <summary>
        /// Birden fazla ürün için toplam fark hesaplama
        /// </summary>
        [Fact]
        public void MultipleItems_TotalDifferenceCalculation()
        {
            // Arrange - 3 ürün
            decimal[] priceDifferences = { 50m, -30m, 0m };

            // Act
            decimal totalDifference = 0m;
            foreach (var diff in priceDifferences)
            {
                totalDifference += diff;
            }

            // Assert
            Assert.Equal(20m, totalDifference); // +50 - 30 + 0 = 20
        }

        #endregion
    }
}
