using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.API.Controllers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private static ECommerceDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ECommerceDbContext(options);
        }

        private static ProductsController CreateController(ECommerceDbContext context, List<MikroUnifiedProductDto> unifiedProducts)
        {
            var productService = new Mock<IProductService>();
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var fileStorage = new Mock<IFileStorage>();
            var logger = new Mock<ILogger<ProductsController>>();
            var mikroDbService = new Mock<IMikroDbService>();
            var productRepository = new Mock<IProductRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();

            mikroDbService.SetupGet(x => x.IsConfigured).Returns(true);
            mikroDbService
                .Setup(x => x.GetUnifiedProductsAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unifiedProducts);

            var controller = new ProductsController(
                productService.Object,
                environment.Object,
                fileStorage.Object,
                logger.Object,
                mikroDbService.Object,
                productRepository.Object,
                context,
                httpClientFactory.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        [Fact]
        public async Task GetProducts_ShouldMatchLegacyCategoryRow_ByCanonicalCategorySlug()
        {
            await using var context = CreateContext();

            var legacyCategory = new Category
            {
                Name = "Ev ve Mutfak",
                Slug = "diger",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var canonicalCategory = new Category
            {
                Name = "Ev ve Mutfak",
                Slug = "ev-ve-mutfak",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.AddRange(legacyCategory, canonicalCategory);
            await context.SaveChangesAsync();

            var unifiedProducts = new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "TAVA-001",
                    StokAd = "Granit Tava 26 cm",
                    Fiyat = 249.90m,
                    StokMiktar = 10,
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                }
            };

            var controller = CreateController(context, unifiedProducts);

            var actionResult = await controller.GetProducts(categoryId: legacyCategory.Id);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal("TAVA-001", product.Sku);
            Assert.Equal("Ev ve Mutfak", product.CategoryName);
            Assert.Equal(canonicalCategory.Id, product.CategoryId);
        }

        [Fact]
        public async Task GetProducts_ShouldFallbackToLocalDb_WhenMikroReturnsNoProducts()
        {
            await using var context = CreateContext();

            var category = new Category
            {
                Name = "Kategori",
                Slug = "kategori",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var fallbackProducts = new List<ProductListDto>
            {
                new()
                {
                    Id = 99,
                    Name = "Yerel Ürün",
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Price = 123.45m,
                    StockQuantity = 7
                }
            };

            var productService = new Mock<IProductService>();
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var fileStorage = new Mock<IFileStorage>();
            var logger = new Mock<ILogger<ProductsController>>();
            var mikroDbService = new Mock<IMikroDbService>();
            var productRepository = new Mock<IProductRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();

            mikroDbService.SetupGet(x => x.IsConfigured).Returns(true);
            mikroDbService
                .Setup(x => x.GetUnifiedProductsAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MikroUnifiedProductDto>());
            productService
                .Setup(x => x.GetActiveProductsWithCampaignAsync(1, 100, category.Id))
                .ReturnsAsync(fallbackProducts);

            var controller = new ProductsController(
                productService.Object,
                environment.Object,
                fileStorage.Object,
                logger.Object,
                mikroDbService.Object,
                productRepository.Object,
                context,
                httpClientFactory.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var actionResult = await controller.GetProducts(categoryId: category.Id);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal(99, product.Id);
            Assert.Equal("Yerel Ürün", product.Name);
        }
    }
}