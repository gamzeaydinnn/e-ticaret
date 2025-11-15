using System;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class InventoryManagerTests
    {
        private static ECommerceDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ECommerceDbContext(options);
        }

        private static InventoryManager CreateManager(ECommerceDbContext context)
        {
            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);

            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);

            var emailSender = new EmailSender(options, envMock.Object);

            var inventorySettings = Options.Create(new InventorySettings
            {
                CriticalStockThreshold = 1
            });

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c["Admin:Email"]).Returns((string?)null);

            return new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);
        }

        [Fact]
        public async Task IncreaseStockAsync_ShouldAddQuantity_AndUpdateProduct()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var manager = CreateManager(context);

            var product = new Product
            {
                Name = "Stock Product",
                StockQuantity = 5
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();
            productRepositoryMock
                .Setup(r => r.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            productRepositoryMock
                .Setup(r => r.UpdateAsync(product))
                .Returns(Task.CompletedTask);

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);
            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(options, envMock.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configurationMock = new Mock<IConfiguration>();

            var managerWithRepoMock = new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);

            // Act
            var result = await managerWithRepoMock.IncreaseStockAsync(product.Id, 3);

            // Assert
            Assert.True(result);
            Assert.Equal(8, product.StockQuantity);
            productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);
        }

        [Fact]
        public async Task DecreaseStockAsync_ShouldDecreaseQuantity_AndUpdateProduct_WhenEnoughStock()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var product = new Product
            {
                Name = "Decrement Product",
                StockQuantity = 10
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();
            productRepositoryMock
                .Setup(r => r.GetByIdAsync(product.Id))
                .ReturnsAsync(product);
            productRepositoryMock
                .Setup(r => r.UpdateAsync(product))
                .Returns(Task.CompletedTask);

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);
            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(options, envMock.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configurationMock = new Mock<IConfiguration>();

            var manager = new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);

            // Act
            var result = await manager.DecreaseStockAsync(product.Id, 4);

            // Assert
            Assert.True(result);
            Assert.Equal(6, product.StockQuantity);
            productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);
        }

        [Fact]
        public async Task DecreaseStockAsync_ShouldReturnFalse_WhenProductNotFound()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();

            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();
            productRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);
            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(options, envMock.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configurationMock = new Mock<IConfiguration>();

            var manager = new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);

            // Act
            var result = await manager.DecreaseStockAsync(999, 5);

            // Assert
            Assert.False(result);
            productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DecreaseStockAsync_ShouldReturnFalse_WhenStockIsInsufficient()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var product = new Product
            {
                Name = "Low Stock",
                StockQuantity = 2
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();
            productRepositoryMock
                .Setup(r => r.GetByIdAsync(product.Id))
                .ReturnsAsync(product);

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);
            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(options, envMock.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configurationMock = new Mock<IConfiguration>();

            var manager = new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);

            // Act
            var result = await manager.DecreaseStockAsync(product.Id, 5);

            // Assert
            Assert.False(result);
            Assert.Equal(2, product.StockQuantity);
            productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetStockLevelAsync_ShouldReturnCurrentQuantity()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var product = new Product
            {
                Name = "Query Stock",
                StockQuantity = 15
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var productRepositoryMock = new Mock<ECommerce.Core.Interfaces.IProductRepository>();
            productRepositoryMock
                .Setup(r => r.GetByIdAsync(product.Id))
                .ReturnsAsync(product);

            var emailSettings = new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test Sender",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            };
            var options = Options.Create(emailSettings);
            var envMock = new Mock<IHostEnvironment>();
            envMock.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(options, envMock.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configurationMock = new Mock<IConfiguration>();

            var manager = new InventoryManager(
                productRepositoryMock.Object,
                context,
                emailSender,
                inventorySettings,
                configurationMock.Object);

            // Act
            var stock = await manager.GetStockLevelAsync(product.Id);

            // Assert
            Assert.Equal(15, stock);
        }
    }
}

