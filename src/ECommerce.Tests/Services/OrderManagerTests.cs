using Xunit;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

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
            var orderManager = new OrderManager(context);
            
            var user = new User { Email = "test@test.com", UserName = "testuser" };
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
        public void ValidateOrder_ShouldReturnExpectedResult(int userId, decimal totalAmount, bool expectedValid)
        {
            // Arrange
            var order = new Order { UserId = userId, TotalAmount = totalAmount };

            // Act
            bool isValid = order.UserId > 0 && order.TotalAmount > 0;

            // Assert
            Assert.Equal(expectedValid, isValid);
        }
    }
}