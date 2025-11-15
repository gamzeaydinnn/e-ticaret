using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class MicroSyncManagerTests
    {
        private static MicroSyncManager CreateManager(
            Mock<IMicroService> microServiceMock,
            Mock<IProductRepository> productRepositoryMock)
        {
            return new MicroSyncManager(microServiceMock.Object, productRepositoryMock.Object);
        }

        [Fact]
        public void SyncProductsToMikro_ShouldCallUpdateProductForEachProduct_AndLogSuccess()
        {
            // Arrange
            var microServiceMock = new Mock<IMicroService>();
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(microServiceMock, productRepositoryMock);

            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    SKU = "SKU-1",
                    Name = "Product 1",
                    StockQuantity = 10,
                    Price = 100m
                },
                new Product
                {
                    Id = 2,
                    SKU = "SKU-2",
                    Name = "Product 2",
                    StockQuantity = 5,
                    Price = 50m
                }
            };

            productRepositoryMock
                .Setup(r => r.GetAll())
                .Returns(products);

            MicroSyncLog? logged = null;
            productRepositoryMock
                .Setup(r => r.LogSync(It.IsAny<MicroSyncLog>()))
                .Callback<MicroSyncLog>(log => logged = log);

            // Act
            manager.SyncProductsToMikro();

            // Assert
            microServiceMock.Verify(
                m => m.UpdateProduct(It.Is<MicroProductDto>(p =>
                    p.Id == 1 &&
                    p.Sku == "SKU-1" &&
                    p.Name == "Product 1" &&
                    p.Stock == 10 &&
                    p.Price == 100m)),
                Times.Once);

            microServiceMock.Verify(
                m => m.UpdateProduct(It.Is<MicroProductDto>(p =>
                    p.Id == 2 &&
                    p.Sku == "SKU-2" &&
                    p.Name == "Product 2" &&
                    p.Stock == 5 &&
                    p.Price == 50m)),
                Times.Once);

            productRepositoryMock.Verify(r => r.LogSync(It.IsAny<MicroSyncLog>()), Times.Once);
            Assert.NotNull(logged);
            Assert.Equal("Product", logged!.EntityType);
            Assert.Equal("Success", logged.Status);
            Assert.Contains("2", logged.Message);
        }

        [Fact]
        public void SyncProductsToMikro_ShouldLogFailure_AndRethrow_OnException()
        {
            // Arrange
            var microServiceMock = new Mock<IMicroService>();
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(microServiceMock, productRepositoryMock);

            var products = new List<Product>
            {
                new Product { Id = 1, SKU = "SKU-1", Name = "Product 1", StockQuantity = 10, Price = 100m }
            };

            productRepositoryMock
                .Setup(r => r.GetAll())
                .Returns(products);

            var ex = new InvalidOperationException("sync failed");
            microServiceMock
                .Setup(m => m.UpdateProduct(It.IsAny<MicroProductDto>()))
                .Throws(ex);

            MicroSyncLog? logged = null;
            productRepositoryMock
                .Setup(r => r.LogSync(It.IsAny<MicroSyncLog>()))
                .Callback<MicroSyncLog>(log => logged = log);

            // Act
            var thrown = Assert.Throws<InvalidOperationException>(() => manager.SyncProductsToMikro());

            // Assert
            Assert.Equal("sync failed", thrown.Message);
            productRepositoryMock.Verify(r => r.LogSync(It.IsAny<MicroSyncLog>()), Times.Once);
            Assert.NotNull(logged);
            Assert.Equal("Product", logged!.EntityType);
            Assert.Equal("Failed", logged.Status);
            Assert.Equal("sync failed", logged.Message);
        }

        [Fact]
        public async Task SyncStocksFromMikroAsync_ShouldUpdateLocalStock_AndLogSuccess()
        {
            // Arrange
            var microServiceMock = new Mock<IMicroService>();
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(microServiceMock, productRepositoryMock);

            var stocks = new List<MicroStockDto>
            {
                new MicroStockDto { Sku = "SKU-1", Quantity = 10, Stock = 0 },
                new MicroStockDto { Sku = "  ", Quantity = 5, Stock = 5 },    // skipped due to empty SKU
                new MicroStockDto { Sku = "SKU-2", Quantity = 0, Stock = 7 }  // product not found
            };

            microServiceMock
                .Setup(m => m.GetStocksAsync())
                .ReturnsAsync(stocks);

            var product = new Product
            {
                Id = 1,
                SKU = "SKU-1",
                Name = "Product 1",
                StockQuantity = 3
            };

            productRepositoryMock
                .Setup(r => r.GetBySkuAsync("SKU-1"))
                .ReturnsAsync(product);

            productRepositoryMock
                .Setup(r => r.GetBySkuAsync("SKU-2"))
                .ReturnsAsync((Product?)null);

            MicroSyncLog? logged = null;
            productRepositoryMock
                .Setup(r => r.LogSyncAsync(It.IsAny<MicroSyncLog>()))
                .Callback<MicroSyncLog>(log => logged = log)
                .Returns(Task.CompletedTask);

            // Act
            await manager.SyncStocksFromMikroAsync();

            // Assert
            Assert.Equal(10, product.StockQuantity); // Quantity > 0 used
            productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);

            productRepositoryMock.Verify(r => r.LogSyncAsync(It.IsAny<MicroSyncLog>()), Times.Once);
            Assert.NotNull(logged);
            Assert.Equal("Stock", logged!.EntityType);
            Assert.Equal("FromERP", logged.Direction);
            Assert.Equal("Success", logged.Status);
            Assert.Contains("1", logged.Message);
        }

        [Fact]
        public async Task SyncPricesFromMikroAsync_ShouldUpdateLocalPrices_AndLogSuccess()
        {
            // Arrange
            var microServiceMock = new Mock<IMicroService>();
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(microServiceMock, productRepositoryMock);

            var prices = new List<MicroPriceDto>
            {
                new MicroPriceDto { Sku = "SKU-1", Price = 99m },
                new MicroPriceDto { Sku = "  ", Price = 10m },   // skipped due to empty SKU
                new MicroPriceDto { Sku = "SKU-2", Price = 50m } // product not found
            };

            microServiceMock
                .Setup(m => m.GetPricesAsync())
                .ReturnsAsync(prices);

            var product = new Product
            {
                Id = 1,
                SKU = "SKU-1",
                Name = "Product 1",
                Price = 120m
            };

            productRepositoryMock
                .Setup(r => r.GetBySkuAsync("SKU-1"))
                .ReturnsAsync(product);

            productRepositoryMock
                .Setup(r => r.GetBySkuAsync("SKU-2"))
                .ReturnsAsync((Product?)null);

            MicroSyncLog? logged = null;
            productRepositoryMock
                .Setup(r => r.LogSyncAsync(It.IsAny<MicroSyncLog>()))
                .Callback<MicroSyncLog>(log => logged = log)
                .Returns(Task.CompletedTask);

            // Act
            await manager.SyncPricesFromMikroAsync();

            // Assert
            Assert.Equal(99m, product.Price);
            productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);

            productRepositoryMock.Verify(r => r.LogSyncAsync(It.IsAny<MicroSyncLog>()), Times.Once);
            Assert.NotNull(logged);
            Assert.Equal("Price", logged!.EntityType);
            Assert.Equal("FromERP", logged.Direction);
            Assert.Equal("Success", logged.Status);
            Assert.Contains("1", logged.Message);
        }
    }
}

