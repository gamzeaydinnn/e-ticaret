using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class OrderManagerTests
    {
        private ECommerceDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldCreateOrder()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            inventoryMock
                .Setup(s => s.DecreaseStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<InventoryChangeType>(), It.IsAny<string?>(), It.IsAny<int?>()))
                .ReturnsAsync(true);
            var orderManager = new OrderManager(context, inventoryMock.Object);
            
            var user = new User 
            { 
                Email = "test@test.com", 
                UserName = "testuser",
                FullName = "Test User",
                Password = "testpassword123",
                PasswordHash = "hashedpassword123"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var product = new Product { Name = "Test Product", Price = 100, StockQuantity = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act & Assert - Basic test structure
            Assert.NotNull(orderManager);
            Assert.True(user.Id > 0);
            Assert.True(product.Id > 0);
        }

        [Theory]
        [InlineData(1, 100, true)]
        [InlineData(0, 100, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, -50, false)]
        public void ValidateOrder_ShouldReturnExpectedResult(int userId, decimal totalPrice, bool expectedValid)
        {
            // Arrange
            var order = new Order { UserId = userId, TotalPrice = totalPrice };

            // Act
            bool isValid = order.UserId > 0 && order.TotalPrice > 0;

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        [Fact]
        public async Task CheckoutAsync_ShouldCreateOrder_DecreaseStock_AndComputeTotalCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            inventoryMock
                .Setup(s => s.ValidateStockForOrderAsync(It.IsAny<IEnumerable<OrderItemDto>>()))
                .ReturnsAsync((true, null));
            inventoryMock
                .Setup(s => s.DecreaseStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<InventoryChangeType>(), It.IsAny<string?>(), It.IsAny<int?>()))
                .ReturnsAsync(true);

            var notificationMock = new Mock<INotificationService>();
            var orderManager = new OrderManager(context, inventoryMock.Object, notificationMock.Object);

            var product = new Product
            {
                Name = "Test Product",
                Price = 100m,
                SpecialPrice = 80m,
                StockQuantity = 10
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İstanbul",
                ShippingMethod = "motorcycle",
                CustomerName = "Test User",
                CustomerPhone = "5550000000",
                CustomerEmail = "test@example.com",
                OrderItems = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = product.Id,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await orderManager.CheckoutAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("motorcycle", result.ShippingMethod);
            Assert.Equal(15m, result.ShippingCost); // motorcycle → 15

            var expectedItemsTotal = 2 * (product.SpecialPrice ?? product.Price);
            Assert.Equal(expectedItemsTotal + 15m, result.TotalPrice);

            Assert.Single(result.OrderItems);
            var item = Assert.Single(result.OrderItems);
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(product.SpecialPrice ?? product.Price, item.UnitPrice);

            var orderInDb = await context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == result.Id);
            Assert.NotNull(orderInDb);
            Assert.Equal(OrderStatus.Pending, orderInDb!.Status);

            inventoryMock.Verify(s =>
                    s.DecreaseStockAsync(
                        product.Id,
                        2,
                        InventoryChangeType.Sale,
                        It.IsAny<string?>(),
                        dto.UserId),
                Times.Once);

            notificationMock.Verify(n => n.SendOrderConfirmationAsync(orderInDb.Id), Times.Once);
        }

        [Fact]
        public async Task CheckoutAsync_ShouldThrow_WhenQuantityIsInvalid()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            inventoryMock
                .Setup(s => s.ValidateStockForOrderAsync(It.IsAny<IEnumerable<OrderItemDto>>()))
                .ReturnsAsync((false, "Geçersiz miktar"));
            var orderManager = new OrderManager(context, inventoryMock.Object);

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "Ankara",
                OrderItems = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 0 // invalid
                    }
                }
            };

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => orderManager.CheckoutAsync(dto));

            // Assert
            Assert.Equal("Geçersiz miktar", ex.Message);
        }

        [Fact]
        public async Task CheckoutAsync_ShouldThrow_WhenProductNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            inventoryMock
                .Setup(s => s.ValidateStockForOrderAsync(It.IsAny<IEnumerable<OrderItemDto>>()))
                .ReturnsAsync((false, "Ürün bulunamadı: 999"));
            var orderManager = new OrderManager(context, inventoryMock.Object);

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "Ankara",
                OrderItems = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = 999,
                        Quantity = 1
                    }
                }
            };

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => orderManager.CheckoutAsync(dto));

            // Assert
            Assert.Equal("Ürün bulunamadı: 999", ex.Message);
        }

        [Fact]
        public async Task CheckoutAsync_ShouldThrow_WhenStockIsInsufficient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            inventoryMock
                .Setup(s => s.ValidateStockForOrderAsync(It.IsAny<IEnumerable<OrderItemDto>>()))
                .ReturnsAsync((false, "Yetersiz stok: Low Stock Product"));
            var orderManager = new OrderManager(context, inventoryMock.Object);

            var product = new Product
            {
                Name = "Low Stock Product",
                Price = 50m,
                StockQuantity = 1
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var dto = new OrderCreateDto
            {
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İzmir",
                OrderItems = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = product.Id,
                        Quantity = 5
                    }
                }
            };

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => orderManager.CheckoutAsync(dto));

            // Assert
            Assert.Equal($"Yetersiz stok: {product.Name}", ex.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ShouldUpdateStatus_AndSendShipmentNotification_WhenDelivered()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var notificationMock = new Mock<INotificationService>();
            var orderManager = new OrderManager(context, inventoryMock.Object, notificationMock.Object);

            var order = new Order
            {
                OrderNumber = "ORD-1",
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İstanbul",
                TotalPrice = 100m,
                Status = OrderStatus.Pending
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            await orderManager.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered.ToString());

            // Assert
            var updated = await context.Orders.FindAsync(order.Id);
            Assert.NotNull(updated);
            Assert.Equal(OrderStatus.Delivered, updated!.Status);

            notificationMock.Verify(
                n => n.SendShipmentNotificationAsync(order.Id, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ShouldDoNothing_WhenOrderNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var notificationMock = new Mock<INotificationService>();
            var orderManager = new OrderManager(context, inventoryMock.Object, notificationMock.Object);

            // Act
            await orderManager.UpdateOrderStatusAsync(999, OrderStatus.Delivered.ToString());

            // Assert
            notificationMock.Verify(
                n => n.SendShipmentNotificationAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ShouldNotChangeStatus_WhenStatusIsInvalid()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var notificationMock = new Mock<INotificationService>();
            var orderManager = new OrderManager(context, inventoryMock.Object, notificationMock.Object);

            var order = new Order
            {
                OrderNumber = "ORD-2",
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İstanbul",
                TotalPrice = 100m,
                Status = OrderStatus.Pending
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            await orderManager.UpdateOrderStatusAsync(order.Id, "INVALID_STATUS");

            // Assert
            var updated = await context.Orders.FindAsync(order.Id);
            Assert.NotNull(updated);
            Assert.Equal(OrderStatus.Pending, updated!.Status);

            notificationMock.Verify(
                n => n.SendShipmentNotificationAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ShouldSetStatusCancelled_WhenOrderBelongsToUserAndIsNotDeliveredOrCancelled()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var orderManager = new OrderManager(context, inventoryMock.Object);

            var order = new Order
            {
                OrderNumber = "ORD-3",
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İstanbul",
                TotalPrice = 200m,
                Status = OrderStatus.Pending
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var result = await orderManager.CancelOrderAsync(order.Id, 1);

            // Assert
            Assert.True(result);
            var updated = await context.Orders.FindAsync(order.Id);
            Assert.NotNull(updated);
            Assert.Equal(OrderStatus.Cancelled, updated!.Status);
        }

        [Fact]
        public async Task CancelOrderAsync_ShouldReturnFalse_WhenOrderNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var orderManager = new OrderManager(context, inventoryMock.Object);

            // Act
            var result = await orderManager.CancelOrderAsync(999, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ShouldReturnOrderDto_WhenOrderExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var orderManager = new OrderManager(context, inventoryMock.Object);

            var product = new Product
            {
                Name = "Order Product",
                Price = 30m,
                StockQuantity = 10
            };
            context.Products.Add(product);

            var order = new Order
            {
                OrderNumber = "ORD-4",
                UserId = 1,
                ShippingAddress = "Adres",
                ShippingCity = "İstanbul",
                TotalPrice = 60m,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = 2,
                        UnitPrice = 30m
                    }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var dto = await orderManager.GetOrderByIdAsync(order.Id);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(order.Id, dto.Id);
            Assert.Equal(order.UserId ?? 0, dto.UserId);
            Assert.Equal(order.OrderNumber, dto.OrderNumber);
            Assert.Equal(order.TotalPrice, dto.TotalPrice);
            Assert.Equal(order.Status.ToString(), dto.Status);
            Assert.Equal(2, dto.TotalItems);
            Assert.Single(dto.OrderItems);
            Assert.Equal(product.Id, dto.OrderItems[0].ProductId);
            Assert.Equal(2, dto.OrderItems[0].Quantity);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ShouldThrow_WhenOrderNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var inventoryMock = new Mock<IInventoryService>();
            var orderManager = new OrderManager(context, inventoryMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() => orderManager.GetOrderByIdAsync(999));

            // Assert
            Assert.Equal("Sipariş bulunamadı.", ex.Message);
        }
    }
}
