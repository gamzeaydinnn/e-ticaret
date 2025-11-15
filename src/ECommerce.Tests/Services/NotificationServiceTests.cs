using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class NotificationServiceTests
    {
        private static (NotificationService Service, ECommerceDbContext Context, string PickupDirectory) CreateSut()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ECommerceDbContext(options);

            var pickupDir = Path.Combine(Path.GetTempPath(), "ecommerce-tests-emails", Guid.NewGuid().ToString());

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = pickupDir
            };
            var emailOptions = Options.Create(emailSettings);

            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);

            var emailSender = new EmailSender(emailOptions, envMock.Object);

            var service = new NotificationService(emailSender, context);
            return (service, context, pickupDir);
        }

        private static int GetEmailFileCount(string pickupDir)
        {
            if (!Directory.Exists(pickupDir))
            {
                return 0;
            }
            return Directory.GetFiles(pickupDir).Length;
        }

        [Fact]
        public async Task SendOrderConfirmationAsync_ShouldSendEmail_WhenOrderExistsWithEmail()
        {
            // Arrange
            var (service, context, pickupDir) = CreateSut();

            var order = new Order
            {
                OrderNumber = "ORD-123",
                CustomerName = "Test User",
                CustomerEmail = "customer@test.local",
                TotalPrice = 150m
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var beforeCount = GetEmailFileCount(pickupDir);

            // Act
            await service.SendOrderConfirmationAsync(order.Id);

            // Assert
            var afterCount = GetEmailFileCount(pickupDir);
            Assert.True(afterCount > beforeCount);
        }

        [Fact]
        public async Task SendOrderConfirmationAsync_ShouldDoNothing_WhenOrderHasNoEmail()
        {
            // Arrange
            var (service, context, pickupDir) = CreateSut();

            var order = new Order
            {
                OrderNumber = "ORD-124",
                CustomerName = "No Email User",
                CustomerEmail = string.Empty,
                TotalPrice = 200m
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var beforeCount = GetEmailFileCount(pickupDir);

            // Act
            await service.SendOrderConfirmationAsync(order.Id);

            // Assert
            var afterCount = GetEmailFileCount(pickupDir);
            Assert.Equal(beforeCount, afterCount);
        }

        [Fact]
        public async Task SendShipmentNotificationAsync_ShouldSendEmail_WhenOrderExistsWithEmail()
        {
            // Arrange
            var (service, context, pickupDir) = CreateSut();

            var order = new Order
            {
                OrderNumber = "ORD-200",
                CustomerName = "Shipment User",
                CustomerEmail = "ship@test.local",
                TotalPrice = 300m
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var beforeCount = GetEmailFileCount(pickupDir);

            // Act
            await service.SendShipmentNotificationAsync(order.Id, "TRACK123");

            // Assert
            var afterCount = GetEmailFileCount(pickupDir);
            Assert.True(afterCount > beforeCount);
        }

        [Fact]
        public async Task SendShipmentNotificationAsync_ShouldDoNothing_WhenOrderNotFound()
        {
            // Arrange
            var (service, _, pickupDir) = CreateSut();

            var beforeCount = GetEmailFileCount(pickupDir);

            // Act
            await service.SendShipmentNotificationAsync(999, "TRACK999");

            // Assert
            var afterCount = GetEmailFileCount(pickupDir);
            Assert.Equal(beforeCount, afterCount);
        }
    }
}

