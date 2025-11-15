using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.Tests.Services
{
    public class PaymentManagerTests
    {
        private static ECommerceDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(databaseName: $"payments_test_{Guid.NewGuid()}")
                .Options;

            return new ECommerceDbContext(options);
        }

        private static PaymentManager CreateManager(
            ECommerceDbContext db,
            out Mock<StripePaymentService> stripeMock,
            out Mock<IyzicoPaymentService> iyzicoMock,
            out Mock<PayPalPaymentService> payPalMock)
        {
            var paymentOptions = Options.Create(new PaymentSettings());

            stripeMock = new Mock<StripePaymentService>(paymentOptions, db) { CallBase = false };
            iyzicoMock = new Mock<IyzicoPaymentService>(paymentOptions, db) { CallBase = false };
            payPalMock = new Mock<PayPalPaymentService>(paymentOptions) { CallBase = false };

            var logMock = new Mock<ILogService>();

            var configDict = new Dictionary<string, string?>
            {
                ["Payment:Provider"] = "Stripe"
            };
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            return new PaymentManager(
                stripeMock.Object,
                iyzicoMock.Object,
                payPalMock.Object,
                db,
                logMock.Object,
                config);
        }

        [Fact]
        public async Task ProcessPaymentAsync_UsesDefaultProvider_AndReturnsResult()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var manager = CreateManager(db, out var stripeMock, out _, out _);

            stripeMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
                .ReturnsAsync(true);

            // Act
            var result = await manager.ProcessPaymentAsync(orderId: 1, amount: 100m);

            // Assert
            Assert.True(result);
            stripeMock.Verify(s => s.ProcessPaymentAsync(1, 100m), Times.Once);
        }

        [Fact]
        public async Task GetPaymentCountAsync_ReturnsNumberOfPayments()
        {
            // Arrange
            using var db = CreateInMemoryDb();

            db.Payments.Add(new Payments
            {
                OrderId = 1,
                Provider = "stripe",
                ProviderPaymentId = "p1",
                Amount = 10,
                Status = "Success"
            });
            db.Payments.Add(new Payments
            {
                OrderId = 2,
                Provider = "iyzico",
                ProviderPaymentId = "p2",
                Amount = 20,
                Status = "Pending"
            });
            await db.SaveChangesAsync();

            var manager = CreateManager(db, out _, out _, out _);

            // Act
            var count = await manager.GetPaymentCountAsync();

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task InitiateAsync_WithPaymentMethodStripe_UsesStripeProvider()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var manager = CreateManager(db, out var stripeMock, out _, out _);

            var dto = new PaymentCreateDto
            {
                OrderId = 10,
                Amount = 150m,
                Currency = "TRY",
                PaymentMethod = "stripe"
            };

            stripeMock
                .Setup(s => s.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency))
                .ReturnsAsync(new PaymentInitResult
                {
                    Provider = "stripe",
                    RequiresRedirect = true,
                    RedirectUrl = "https://stripe.test/checkout"
                });

            // Act
            var result = await manager.InitiateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("stripe", result.Provider);
            stripeMock.Verify(
                s => s.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency),
                Times.Once);
        }

        [Fact]
        public async Task GetPaymentStatusAsync_ResolvesProviderFromPaymentRecord()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            const string token = "iyz-token-123";

            db.Payments.Add(new Payments
            {
                OrderId = 5,
                Provider = "iyzico",
                ProviderPaymentId = token,
                Amount = 99.9m,
                Status = "Pending"
            });
            await db.SaveChangesAsync();

            var manager = CreateManager(db, out _, out var iyzicoMock, out _);

            iyzicoMock
                .Setup(s => s.GetPaymentStatusAsync(token))
                .ReturnsAsync(PaymentStatus.Successful);

            // Act
            var status = await manager.GetPaymentStatusAsync(token);

            // Assert
            Assert.Equal(PaymentStatus.Successful, status);
            iyzicoMock.Verify(s => s.GetPaymentStatusAsync(token), Times.Once);
        }
    }
}

