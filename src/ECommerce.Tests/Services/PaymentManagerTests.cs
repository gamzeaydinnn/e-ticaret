using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Entities.Enums;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class PaymentManagerTests
    {
        [Fact]
        public async Task ProcessPaymentAsync_ShouldReturnTrue_OnSuccess()
        {
            // Arrange
            var manager = new PaymentManager();

            // Act
            var result = await manager.ProcessPaymentAsync(orderId: 1, amount: 100m);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckPaymentStatusAsync_ShouldReturnTrue_OnBaselineBehavior()
        {
            // Arrange
            var manager = new PaymentManager();

            // Act
            var result = await manager.CheckPaymentStatusAsync("payment-123");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetPaymentCountAsync_ShouldAlwaysReturnZero_ForBaselineImplementation()
        {
            // Arrange
            var manager = new PaymentManager();

            // Act
            var count = await manager.GetPaymentCountAsync();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ProcessPaymentDetailedAsync_ShouldReturnSuccessful_WhenProcessPaymentAsyncReturnsTrue()
        {
            // Arrange
            var mock = new Mock<PaymentManager> { CallBase = true };
            mock
                .Setup(m => m.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(true);

            var manager = mock.Object;

            // Act
            var status = await manager.ProcessPaymentDetailedAsync(orderId: 1, amount: 50m);

            // Assert
            Assert.Equal(PaymentStatus.Successful, status);
            mock.Verify(m => m.ProcessPaymentAsync(1, 50m), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentDetailedAsync_ShouldReturnFailed_WhenProcessPaymentAsyncReturnsFalse()
        {
            // Arrange
            var mock = new Mock<PaymentManager> { CallBase = true };
            mock
                .Setup(m => m.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(false);

            var manager = mock.Object;

            // Act
            var status = await manager.ProcessPaymentDetailedAsync(orderId: 2, amount: 75m);

            // Assert
            Assert.Equal(PaymentStatus.Failed, status);
            mock.Verify(m => m.ProcessPaymentAsync(2, 75m), Times.Once);
        }

        [Fact]
        public async Task GetPaymentStatusAsync_ShouldReturnSuccessful_WhenCheckPaymentStatusAsyncReturnsTrue()
        {
            // Arrange
            var mock = new Mock<PaymentManager> { CallBase = true };
            mock
                .Setup(m => m.CheckPaymentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var manager = mock.Object;

            // Act
            var status = await manager.GetPaymentStatusAsync("payment-ok");

            // Assert
            Assert.Equal(PaymentStatus.Successful, status);
            mock.Verify(m => m.CheckPaymentStatusAsync("payment-ok"), Times.Once);
        }

        [Fact]
        public async Task GetPaymentStatusAsync_ShouldReturnFailed_WhenCheckPaymentStatusAsyncReturnsFalse()
        {
            // Arrange
            var mock = new Mock<PaymentManager> { CallBase = true };
            mock
                .Setup(m => m.CheckPaymentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var manager = mock.Object;

            // Act
            var status = await manager.GetPaymentStatusAsync("payment-fail");

            // Assert
            Assert.Equal(PaymentStatus.Failed, status);
            mock.Verify(m => m.CheckPaymentStatusAsync("payment-fail"), Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_ShouldReturnMockResult_WithExpectedFields()
        {
            // Arrange
            var manager = new PaymentManager();
            const int orderId = 123;
            const decimal amount = 250.50m;
            const string currency = "TRY";

            // Act
            PaymentInitResult result = await manager.InitiateAsync(orderId, amount, currency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("mock", result.Provider);
            Assert.False(result.RequiresRedirect);

            Assert.Equal(orderId, result.OrderId);
            Assert.Equal(amount, result.Amount);
            Assert.Equal(currency, result.Currency);

            Assert.Null(result.RedirectUrl);
            Assert.Null(result.CheckoutSessionId);
            Assert.Null(result.ClientSecret);
        }
    }
}

