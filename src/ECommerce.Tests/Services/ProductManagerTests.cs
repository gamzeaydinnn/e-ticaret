using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Data.Context;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class ProductManagerTests
    {
        private static ProductManager CreateManager(Mock<IProductRepository> productRepositoryMock)
        {
            var reviewRepositoryMock = new Mock<IReviewRepository>();
            var inventoryLogMock = new Mock<IInventoryLogService>();
            return new ProductManager(productRepositoryMock.Object, reviewRepositoryMock.Object, inventoryLogMock.Object);
        }

        private static ECommerceDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnMappedDtoList()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            var category = new Category { Id = 1, Name = "Kategori" };
            var brand = new Brand { Id = 2, Name = "Marka" };

            var products = new List<Product>
            {
                new Product
                {
                    Id = 10,
                    Name = "Product A",
                    Description = "Desc A",
                    Price = 100m,
                    SpecialPrice = 90m,
                    StockQuantity = 5,
                    ImageUrl = "a.jpg",
                    Category = category,
                    Brand = brand
                },
                new Product
                {
                    Id = 11,
                    Name = "Product B",
                    Description = "Desc B",
                    Price = 200m,
                    StockQuantity = 3,
                    Category = category,
                    Brand = brand
                }
            };

            productRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(products);

            // Act
            var result = await manager.GetProductsAsync(query: null, categoryId: null, page: 1, pageSize: 20);

            // Assert
            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(2, list.Count);

            var first = list[0];
            Assert.Equal(10, first.Id);
            Assert.Equal("Product A", first.Name);
            Assert.Equal("Desc A", first.Description);
            Assert.Equal(100m, first.Price);
            Assert.Equal(90m, first.SpecialPrice);
            Assert.Equal(5, first.StockQuantity);
            Assert.Equal("a.jpg", first.ImageUrl);
            Assert.Equal("Marka", first.Brand);
            Assert.Equal("Kategori", first.CategoryName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenProductExists()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            var product = new Product
            {
                Id = 5,
                Name = "Test Product",
                Description = "Test Desc",
                Price = 50m,
                SpecialPrice = 45m,
                StockQuantity = 10,
                ImageUrl = "image.jpg",
                Brand = new Brand { Id = 1, Name = "Brand X" },
                Category = new Category { Id = 2, Name = "Cat Y" }
            };

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(product);

            // Act
            var dto = await manager.GetByIdAsync(5);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(product.Id, dto!.Id);
            Assert.Equal(product.Name, dto.Name);
            Assert.Equal(product.Description, dto.Description);
            Assert.Equal(product.Price, dto.Price);
            Assert.Equal(product.SpecialPrice, dto.SpecialPrice);
            Assert.Equal(product.StockQuantity, dto.StockQuantity);
            Assert.Equal(product.ImageUrl, dto.ImageUrl);
            Assert.Equal("Brand X", dto.Brand);
            Assert.Equal("Cat Y", dto.CategoryName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            var dto = await manager.GetByIdAsync(999);

            // Assert
            Assert.Null(dto);
        }

        [Fact]
        public async Task CreateProductAsync_ShouldCallAddAsync_AndReturnDtoWithDefaults()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            Product? addedProduct = null;
            productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p => addedProduct = p)
                .ReturnsAsync((Product p) => p);

            var dto = new ProductCreateDto
            {
                Name = "Created Product",
                Description = "Created Desc",
                Price = 120m,
                SpecialPrice = null,
                StockQuantity = 8,
                CategoryId = 3,
                ImageUrl = null,
                BrandId = 4
            };

            // Act
            var result = await manager.CreateProductAsync(dto);

            // Assert
            Assert.NotNull(addedProduct);
            Assert.Equal(dto.Name, addedProduct!.Name);
            Assert.Equal(dto.Description, addedProduct.Description);
            Assert.Equal(dto.Price, addedProduct.Price);
            Assert.Equal(dto.SpecialPrice, addedProduct.SpecialPrice);
            Assert.Equal(dto.StockQuantity, addedProduct.StockQuantity);
            Assert.Equal(dto.CategoryId, addedProduct.CategoryId);
            Assert.Equal(string.Empty, addedProduct.ImageUrl);
            Assert.Equal(dto.BrandId, addedProduct.BrandId);
            Assert.True(addedProduct.AdminOverrideCategory);

            productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);

            Assert.Equal(addedProduct.Id, result.Id);
            Assert.Equal(addedProduct.Name, result.Name);
            Assert.Equal(addedProduct.Description, result.Description);
            Assert.Equal(addedProduct.Price, result.Price);
            Assert.Equal(addedProduct.SpecialPrice, result.SpecialPrice);
            Assert.Equal(addedProduct.StockQuantity, result.StockQuantity);
            Assert.Equal(addedProduct.ImageUrl, result.ImageUrl);
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldUpdateFields_WhenProductExists()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            var existing = new Product
            {
                Id = 10,
                Name = "Old",
                Description = "Old Desc",
                Price = 10m,
                SpecialPrice = null,
                StockQuantity = 1,
                CategoryId = 1,
                ImageUrl = "old.jpg",
                BrandId = 1
            };

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(existing);

            productRepositoryMock
                .Setup(r => r.UpdateAsync(existing))
                .Returns(Task.CompletedTask);

            var dto = new ProductUpdateDto
            {
                Name = "New Name",
                Description = "New Desc",
                Price = 20m,
                SpecialPrice = 18m,
                StockQuantity = 5,
                CategoryId = 2,
                ImageUrl = "new.jpg",
                BrandId = 3,
                AdminOverrideName = true,
                AdminOverridePrice = true
            };

            // Act
            await manager.UpdateProductAsync(10, dto);

            // Assert
            Assert.Equal("New Name", existing.Name);
            Assert.Equal("New Desc", existing.Description);
            Assert.Equal(20m, existing.Price);
            Assert.Equal(18m, existing.SpecialPrice);
            Assert.Equal(5, existing.StockQuantity);
            Assert.Equal(2, existing.CategoryId);
            Assert.Equal("new.jpg", existing.ImageUrl);
            Assert.Equal(3, existing.BrandId);
            Assert.True(existing.AdminOverrideCategory);
            Assert.True(existing.AdminOverrideName);
            Assert.True(existing.AdminOverridePrice);
            Assert.True(existing.AdminOverrideCategory);

            productRepositoryMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldDoNothing_WhenProductDoesNotExist()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            var dto = new ProductUpdateDto
            {
                Name = "New Name",
                Description = "New Desc",
                Price = 20m,
                SpecialPrice = 18m,
                StockQuantity = 5,
                CategoryId = 2,
                ImageUrl = "new.jpg",
                BrandId = 3
            };

            // Act
            await manager.UpdateProductAsync(999, dto);

            // Assert
            productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldCallDelete_WhenProductExists()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            var existing = new Product { Id = 7, Name = "ToDelete" };

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(7))
                .ReturnsAsync(existing);

            productRepositoryMock
                .Setup(r => r.DeleteAsync(existing))
                .Returns(Task.CompletedTask);

            // Act
            await manager.DeleteProductAsync(7);

            // Assert
            productRepositoryMock.Verify(r => r.DeleteAsync(existing), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldDoNothing_WhenProductDoesNotExist()
        {
            // Arrange
            var productRepositoryMock = new Mock<IProductRepository>();
            var manager = CreateManager(productRepositoryMock);

            productRepositoryMock
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            // Act
            await manager.DeleteProductAsync(999);

            // Assert
            productRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetProductsByCategoryPagedAsync_ShouldFilterSortAndPaginateAtDbLevel()
        {
            await using var context = CreateContext();
            var category = new Category { Id = 5, Name = "Et", Slug = "et", IsActive = true };
            context.Categories.Add(category);
            context.Products.AddRange(
                new Product { Id = 1, Name = "B Ürün", CategoryId = 5, Category = category, Price = 80m, StockQuantity = 3, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1), SKU = "A" },
                new Product { Id = 2, Name = "A Ürün", CategoryId = 5, Category = category, Price = 120m, StockQuantity = 0, IsActive = true, CreatedAt = DateTime.UtcNow, SKU = "B" },
                new Product { Id = 3, Name = "C Ürün", CategoryId = 5, Category = category, Price = 95m, StockQuantity = 8, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2), SKU = "C" },
                new Product { Id = 4, Name = "Diğer", CategoryId = 8, Price = 50m, StockQuantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow, SKU = "D" });
            await context.SaveChangesAsync();

            var productRepositoryMock = new Mock<IProductRepository>();
            var reviewRepositoryMock = new Mock<IReviewRepository>();
            var inventoryLogMock = new Mock<IInventoryLogService>();
            var manager = new ProductManager(
                productRepositoryMock.Object,
                reviewRepositoryMock.Object,
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                inventoryLogMock.Object,
                context);

            var result = await manager.GetProductsByCategoryPagedAsync(5, page: 1, size: 2, sort: "price", direction: "desc", inStock: true);

            Assert.Equal(2, result.Total);
            Assert.Equal(0, result.Skip);
            Assert.Equal(2, result.Take);
            Assert.Equal(new[] { 3, 1 }, result.Items.Select(item => item.Id).ToArray());
        }

        [Fact]
        public async Task GetProductsByCategoryPagedAsync_ShouldExcludeZeroPriceProducts()
        {
            using var context = CreateContext();

            var category = new Category { Id = 5, Name = "Et", Slug = "et", IsActive = true };
            context.Categories.Add(category);
            context.Products.AddRange(
                new Product { Id = 1, Name = "Gecerli", Price = 100m, StockQuantity = 4, CategoryId = category.Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Id = 2, Name = "Sifir Fiyat", Price = 0m, StockQuantity = 8, CategoryId = category.Id, IsActive = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var repo = new Mock<IProductRepository>();
            var reviewRepo = new Mock<IReviewRepository>();
            var inventoryLog = new Mock<IInventoryLogService>();
            var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var manager = new ProductManager(repo.Object, reviewRepo.Object, cache, inventoryLog.Object, context);

            var result = await manager.GetProductsByCategoryPagedAsync(category.Id, 1, 10, "name", "asc", null);

            var item = Assert.Single(result.Items);
            Assert.Equal(1, item.Id);
            Assert.Equal(1, result.Total);
        }
    }
}
