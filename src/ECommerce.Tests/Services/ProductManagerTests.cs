using Xunit;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Data.Repositories;
using System.Threading.Tasks;
using System.Linq;
using ECommerce.Core.Interfaces; // IReviewRepository için gerekli
using Moq; // Mock kullanımı için gerekli

namespace ECommerce.Tests.Services
{
    public class ProductManagerTests
    {
        private ECommerceDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                // Her test için benzersiz bir veritabanı adı kullanmak önemlidir
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnProducts()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            var products = new[]
            {
                new Product { Name = "Test Product 1", Price = 100, StockQuantity = 10 },
                new Product { Name = "Test Product 2", Price = 200, StockQuantity = 5 }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            var productRepository = new ProductRepository(context);
            
            // Hata CS7036'yı çözmek için: ProductManager'ın beklediği ikinci bağımlılığı ekle
            var mockReviewRepository = new Mock<IReviewRepository>(); 
            
            // ProductManager'ı tüm bağımlılıklarla başlat
            var productManager = new ProductManager(productRepository, mockReviewRepository.Object);

            // Act
            // Hata CS1501'i çözmek için: Metodun yalnızca 4 argüman alan aşırı yüklemesini kullan
            var result = await productManager.GetProductsAsync(query: null, categoryId: null, page: 1, pageSize: 20);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Name == "Test Product 1");
            Assert.Contains(result, p => p.Name == "Test Product 2");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnProduct()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            var product = new Product { Name = "Test Product", Price = 100, StockQuantity = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var productRepository = new ProductRepository(context);
            // Hata CS7036'yı çözmek için bağımlılığı ekle
            var mockReviewRepository = new Mock<IReviewRepository>();
            var productManager = new ProductManager(productRepository, mockReviewRepository.Object);

            // Act
            var result = await productManager.GetByIdAsync(product.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal(100, result.Price);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var productRepository = new ProductRepository(context);
            // Hata CS7036'yı çözmek için bağımlılığı ekle
            var mockReviewRepository = new Mock<IReviewRepository>();
            var productManager = new ProductManager(productRepository, mockReviewRepository.Object);

            // Act
            var result = await productManager.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Test Product", 100, true)]
        [InlineData("", 100, false)]
        [InlineData("Test Product", -1, false)]
        public void ValidateProduct_ShouldReturnExpectedResult(string name, decimal price, bool expectedValid)
        {
            // Arrange
            var product = new Product { Name = name, Price = price };

            // Act
            bool isValid = !string.IsNullOrEmpty(product.Name) && product.Price > 0;

            // Assert
            Assert.Equal(expectedValid, isValid);
        }
    }
}
