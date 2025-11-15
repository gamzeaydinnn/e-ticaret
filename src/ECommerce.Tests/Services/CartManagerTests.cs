using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class CartManagerTests
    {
        private static CartManager CreateCartManager(Mock<ICartRepository> cartRepositoryMock)
        {
            return new CartManager(cartRepositoryMock.Object);
        }

        [Fact]
        public async Task AddItemToCartAsync_ShouldAddNewItem_WhenNotInCart()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            var dto = new CartItemDto
            {
                ProductId = 10,
                Quantity = 2
            };

            cartRepositoryMock
                .Setup(r => r.GetByUserAndProductAsync(userId, dto.ProductId))
                .ReturnsAsync((CartItem?)null);

            // Act
            await manager.AddItemToCartAsync(userId, dto);

            // Assert
            cartRepositoryMock.Verify(
                r => r.AddAsync(It.Is<CartItem>(c =>
                    c.UserId == userId &&
                    c.ProductId == dto.ProductId &&
                    c.Quantity == dto.Quantity &&
                    c.CartToken == userId.ToString())),
                Times.Once);

            cartRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<CartItem>()),
                Times.Never);
        }

        [Fact]
        public async Task AddItemToCartAsync_ShouldIncreaseQuantity_WhenItemAlreadyExists()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            var dto = new CartItemDto
            {
                ProductId = 10,
                Quantity = 3
            };

            var existingItem = new CartItem
            {
                Id = 5,
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = 2
            };

            cartRepositoryMock
                .Setup(r => r.GetByUserAndProductAsync(userId, dto.ProductId))
                .ReturnsAsync(existingItem);

            // Act
            await manager.AddItemToCartAsync(userId, dto);

            // Assert
            Assert.Equal(5, existingItem.Id);
            Assert.Equal(5, existingItem.Quantity); // 2 + 3

            cartRepositoryMock.Verify(
                r => r.UpdateAsync(It.Is<CartItem>(c => c == existingItem)),
                Times.Once);

            cartRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<CartItem>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateCartItemAsync_ShouldUpdateQuantity_WhenItemExistsAndBelongsToUser()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            const int cartItemId = 100;
            const int newQuantity = 7;

            var existingItem = new CartItem
            {
                Id = cartItemId,
                UserId = userId,
                ProductId = 10,
                Quantity = 2
            };

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync(existingItem);

            // Act
            await manager.UpdateCartItemAsync(userId, cartItemId, newQuantity);

            // Assert
            Assert.Equal(newQuantity, existingItem.Quantity);

            cartRepositoryMock.Verify(
                r => r.UpdateAsync(It.Is<CartItem>(c => c == existingItem)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCartItemAsync_ShouldDoNothing_WhenItemNotFound()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            const int cartItemId = 100;

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync((CartItem?)null);

            // Act
            await manager.UpdateCartItemAsync(userId, cartItemId, 5);

            // Assert
            cartRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<CartItem>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateCartItemAsync_ShouldDoNothing_WhenItemBelongsToAnotherUser()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int requestedUserId = 1;
            const int actualUserId = 2;
            const int cartItemId = 100;

            var existingItem = new CartItem
            {
                Id = cartItemId,
                UserId = actualUserId,
                ProductId = 10,
                Quantity = 2
            };

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync(existingItem);

            // Act
            await manager.UpdateCartItemAsync(requestedUserId, cartItemId, 5);

            // Assert
            Assert.Equal(2, existingItem.Quantity); // unchanged

            cartRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<CartItem>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveCartItemAsync_ShouldRemoveItem_WhenExistsAndBelongsToUser()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            const int cartItemId = 50;

            var existingItem = new CartItem
            {
                Id = cartItemId,
                UserId = userId,
                ProductId = 10,
                Quantity = 1
            };

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync(existingItem);

            // Act
            await manager.RemoveCartItemAsync(userId, cartItemId);

            // Assert
            cartRepositoryMock.Verify(
                r => r.RemoveCartItemAsync(cartItemId),
                Times.Once);
        }

        [Fact]
        public async Task RemoveCartItemAsync_ShouldDoNothing_WhenItemNotFound()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;
            const int cartItemId = 50;

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync((CartItem?)null);

            // Act
            await manager.RemoveCartItemAsync(userId, cartItemId);

            // Assert
            cartRepositoryMock.Verify(
                r => r.RemoveCartItemAsync(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveCartItemAsync_ShouldDoNothing_WhenItemBelongsToAnotherUser()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int requestedUserId = 1;
            const int actualUserId = 2;
            const int cartItemId = 50;

            var existingItem = new CartItem
            {
                Id = cartItemId,
                UserId = actualUserId,
                ProductId = 10,
                Quantity = 1
            };

            cartRepositoryMock
                .Setup(r => r.GetByIdAsync(cartItemId))
                .ReturnsAsync(existingItem);

            // Act
            await manager.RemoveCartItemAsync(requestedUserId, cartItemId);

            // Assert
            cartRepositoryMock.Verify(
                r => r.RemoveCartItemAsync(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task GetCartAsync_ShouldReturnSummaryWithItemsAndTotal()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;

            var items = new List<CartItem>
            {
                new CartItem
                {
                    UserId = userId,
                    ProductId = 10,
                    Quantity = 2,
                    Product = new Product { Id = 10, Price = 100m }
                },
                new CartItem
                {
                    UserId = userId,
                    ProductId = 20,
                    Quantity = 1,
                    Product = new Product { Id = 20, Price = 50m }
                }
            };

            cartRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var summary = await manager.GetCartAsync(userId);

            // Assert
            Assert.NotNull(summary);
            Assert.Equal(2, summary.Items.Count);

            var first = summary.Items.First(i => i.ProductId == 10);
            Assert.Equal(2, first.Quantity);

            var second = summary.Items.First(i => i.ProductId == 20);
            Assert.Equal(1, second.Quantity);

            Assert.Equal(250m, summary.Total); // (2 * 100) + (1 * 50)
        }

        [Fact]
        public async Task ClearCartAsync_ShouldCallRepositoryClearCart()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            const int userId = 1;

            // Act
            await manager.ClearCartAsync(userId);

            // Assert
            cartRepositoryMock.Verify(
                r => r.ClearCartAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task GetCartCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var cartRepositoryMock = new Mock<ICartRepository>();
            var manager = CreateCartManager(cartRepositoryMock);

            cartRepositoryMock
                .Setup(r => r.GetCartCountAsync())
                .ReturnsAsync(5);

            // Act
            var count = await manager.GetCartCountAsync();

            // Assert
            Assert.Equal(5, count);
            cartRepositoryMock.Verify(r => r.GetCartCountAsync(), Times.Once);
        }
    }
}

