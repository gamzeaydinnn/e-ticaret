using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static Mock<IProductAdminOverrideSettingsService> CreateOverrideSettingsService()
        {
            var service = new Mock<IProductAdminOverrideSettingsService>();
            service
                .Setup(settings => settings.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductAdminOverrideSettingsDto());
            service
                .Setup(settings => settings.GetSettingsForAdminAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductAdminOverrideSettingsDto());
            return service;
        }

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
            var productAdminOverrideSettingsService = CreateOverrideSettingsService();

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
                httpClientFactory.Object,
                productAdminOverrideSettingsService.Object);

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
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "EV VE MUTFAK",
                CategoryId = canonicalCategory.Id,
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
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
                    AnagrupKod = "EV VE MUTFAK",
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
            var productAdminOverrideSettingsService = CreateOverrideSettingsService();

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
                httpClientFactory.Object,
                productAdminOverrideSettingsService.Object);

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
        [Fact]
        public async Task GetProductsByCategoryPaged_ShouldReturnPagedResultFromService()
        {
            await using var context = CreateContext();

            var productService = new Mock<IProductService>();
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var fileStorage = new Mock<IFileStorage>();
            var logger = new Mock<ILogger<ProductsController>>();
            var mikroDbService = new Mock<IMikroDbService>();
            var productRepository = new Mock<IProductRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var productAdminOverrideSettingsService = CreateOverrideSettingsService();

            var pagedResult = new ECommerce.Core.DTOs.PagedResult<ProductListDto>(
                new List<ProductListDto>
                {
                    new()
                    {
                        Id = 15,
                        Name = "Kategori Ürünü",
                        CategoryId = 3,
                        CategoryName = "Temel Gıda"
                    }
                },
                total: 27,
                skip: 25,
                take: 25);

            productService
                .Setup(service => service.GetProductsByCategoryPagedAsync(3, 2, 25, "price", "desc", true))
                .ReturnsAsync(pagedResult);

            var controller = new ProductsController(
                productService.Object,
                environment.Object,
                fileStorage.Object,
                logger.Object,
                mikroDbService.Object,
                productRepository.Object,
                context,
                httpClientFactory.Object,
                productAdminOverrideSettingsService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var actionResult = await controller.GetProductsByCategoryPaged(3, 2, 25, "price", "desc", true);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var model = Assert.IsType<ECommerce.Core.DTOs.PagedResult<ProductListDto>>(okResult.Value);

            Assert.Equal(27, model.Total);
            Assert.Single(model.Items);
            Assert.Equal(25, model.Skip);
            Assert.Equal(25, model.Take);
        }
        [Fact]
        public async Task GetProductsByCategoryPaged_ShouldPreferPositiveMikroPrice_OverLocalZeroPrice()
        {
            await using var context = CreateContext();

            var category = new Category
            {
                Id = 7,
                Name = "Ev ve Mutfak",
                Slug = "ev-ve-mutfak",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(category);
            context.Products.Add(new Product
            {
                Id = 70,
                Name = "Eski Yerel Ürün",
                SKU = "SP-001",
                Price = 0m,
                StockQuantity = 9,
                CategoryId = category.Id,
                IsActive = true,
                Slug = "eski-yerel-urun",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "EV VE MUTFAK",
                CategoryId = category.Id,
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var productService = new Mock<IProductService>();
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var fileStorage = new Mock<IFileStorage>();
            var logger = new Mock<ILogger<ProductsController>>();
            var mikroDbService = new Mock<IMikroDbService>();
            var productRepository = new Mock<IProductRepository>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var productAdminOverrideSettingsService = CreateOverrideSettingsService();

            mikroDbService.SetupGet(service => service.IsConfigured).Returns(true);
            mikroDbService
                .Setup(service => service.GetUnifiedProductsAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MikroUnifiedProductDto>
                {
                    new()
                    {
                        StokKod = "SP-001",
                        StokAd = "Spelly Ürün",
                        Fiyat = 149.90m,
                        StokMiktar = 9,
                        AnagrupKod = "EV VE MUTFAK",
                        GrupKod = "MAMA",
                        Birim = "ADET",
                        WebeGonderilecekFl = true
                    }
                });

            var controller = new ProductsController(
                productService.Object,
                environment.Object,
                fileStorage.Object,
                logger.Object,
                mikroDbService.Object,
                productRepository.Object,
                context,
                httpClientFactory.Object,
                productAdminOverrideSettingsService.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var actionResult = await controller.GetProductsByCategoryPaged(category.Id, 1, 25, "price", "asc", true);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var model = Assert.IsType<ECommerce.Core.DTOs.PagedResult<ProductListDto>>(okResult.Value);

            var product = Assert.Single(model.Items);
            Assert.Equal(149.90m, product.Price);
            Assert.Equal("SP-001", product.Sku);
        }
        [Fact]
        public async Task SearchProducts_ShouldReturnMikroBackedResults_WhenOnlyMikroHasTheProduct()
        {
            await using var context = CreateContext();

            var category = new Category
            {
                Id = 8,
                Name = "Temel Gıda",
                Slug = "temel-gida",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(category);
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "500",
                MikroAltgrupKod = "506",
                CategoryId = category.Id,
                Priority = 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "RECEL-42",
                    StokAd = "YENIGUN CILEK RECELI 380 GR",
                    Fiyat = 87.5m,
                    StokMiktar = 9,
                    Birim = "ADET",
                    AnagrupKod = "500",
                    GrupKod = "506",
                    WebeGonderilecekFl = true
                }
            });

            var actionResult = await controller.SearchProducts("recel", 1, 20);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal("RECEL-42", product.Sku);
            Assert.Equal("Temel Gıda", product.CategoryName);
        }

        [Fact]
        public async Task GetProducts_ShouldPreferSpecificAltgrupMapping_OverBroadAnagrupMapping()
        {
            await using var context = CreateContext();

            var sutKategori = new Category { Id = 4, Name = "Süt Ürünleri", Slug = "sut-ve-sut-urunleri", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var temelGidaKategori = new Category { Id = 9, Name = "Temel Gıda", Slug = "temel-gida", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            context.Categories.AddRange(sutKategori, temelGidaKategori);
            context.MikroCategoryMappings.AddRange(
                new MikroCategoryMapping { MikroAnagrupKod = "500", CategoryId = sutKategori.Id, Priority = 10, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new MikroCategoryMapping { MikroAnagrupKod = "500", MikroAltgrupKod = "506", CategoryId = temelGidaKategori.Id, Priority = 100, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var controller = CreateController(context, new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "RECEL-1",
                    StokAd = "YENIGUN AHUDUDU RECELI 380 GR",
                    Fiyat = 99m,
                    StokMiktar = 10,
                    AnagrupKod = "500",
                    GrupKod = "506",
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                }
            });

            var actionResult = await controller.GetProducts();
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal(temelGidaKategori.Id, product.CategoryId);
            Assert.Equal("Temel Gıda", product.CategoryName);
        }

        [Fact]
        public async Task GetProducts_ShouldUseNameHints_WhenWildcardWouldRouteToDiger()
        {
            await using var context = CreateContext();

            var digerKategori = new Category { Id = 1003, Name = "Diğer", Slug = "diger", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var evKategori = new Category { Id = 2003, Name = "Ev & Mutfak", Slug = "ev-ve-mutfak", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            context.Categories.AddRange(digerKategori, evKategori);
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "*",
                CategoryId = digerKategori.Id,
                Priority = 999,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "KAGIT-1",
                    StokAd = "KOROPLAST PISIRME KAGIDI 10 LU",
                    Fiyat = 49m,
                    StokMiktar = 5,
                    AnagrupKod = string.Empty,
                    GrupKod = string.Empty,
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                }
            });

            var actionResult = await controller.GetProducts();
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal(evKategori.Id, product.CategoryId);
            Assert.Equal("Ev & Mutfak", product.CategoryName);
        }

        [Fact]
        public async Task GetProducts_ShouldPromoteSuperfreshProducts_ToFrozenCategoryFromGenericMapping()
        {
            await using var context = CreateContext();

            var frozenKategori = new Category { Id = 301, Name = "Dondurma & Dondurulmuş Gıda", Slug = "dondurma-ve-dondurulmus-gida", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var temelGidaKategori = new Category { Id = 302, Name = "Temel Gıda", Slug = "temel-gida", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            context.Categories.AddRange(frozenKategori, temelGidaKategori);
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "TEMEL GIDA",
                CategoryId = temelGidaKategori.Id,
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "SUPERFRESH-1",
                    StokAd = "SUPERFRESH MANTI 1 KG",
                    Fiyat = 149m,
                    StokMiktar = 8,
                    AnagrupKod = "TEMEL GIDA",
                    GrupKod = string.Empty,
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                }
            });

            var actionResult = await controller.GetProducts();
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var products = Assert.IsAssignableFrom<IEnumerable<ProductListDto>>(okResult.Value).ToList();

            var product = Assert.Single(products);
            Assert.Equal(frozenKategori.Id, product.CategoryId);
            Assert.Equal("Dondurma & Dondurulmuş Gıda", product.CategoryName);
        }

        [Fact]
        public async Task GetAllProductsForAdmin_ShouldIncludeMikroOnlyProducts_AndExcludeStaleLocalSkuRows()
        {
            await using var context = CreateContext();

            var category = new Category
            {
                Id = 77,
                Name = "Temel Gıda",
                Slug = "temel-gida",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(category);
            context.Products.AddRange(
                new Product
                {
                    Id = 701,
                    Name = "Eski Senkron Kayıt",
                    SKU = "STALE-001",
                    CategoryId = category.Id,
                    Price = 20m,
                    StockQuantity = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 702,
                    Name = "Yerel Eşleşen Kayıt",
                    SKU = "MATCH-001",
                    CategoryId = category.Id,
                    Price = 25m,
                    StockQuantity = 6,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = 703,
                    Name = "Elle Eklenen Yerel Ürün",
                    SKU = null,
                    CategoryId = category.Id,
                    Price = 30m,
                    StockQuantity = 4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "TEMEL GIDA",
                CategoryId = category.Id,
                Priority = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, new List<MikroUnifiedProductDto>
            {
                new()
                {
                    StokKod = "MATCH-001",
                    StokAd = "Mikro Eşleşen Ürün",
                    Fiyat = 99m,
                    StokMiktar = 9,
                    AnagrupKod = "TEMEL GIDA",
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                },
                new()
                {
                    StokKod = "MIKRO-ONLY-001",
                    StokAd = "Sadece Mikro Ürünü",
                    Fiyat = 109m,
                    StokMiktar = 7,
                    AnagrupKod = "TEMEL GIDA",
                    Birim = "ADET",
                    WebeGonderilecekFl = true
                }
            });

            var actionResult = await controller.GetAllProductsForAdmin(page: 1, size: 50);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var payload = Assert.NotNull(okResult.Value);

            var itemsProperty = payload.GetType().GetProperty("items", BindingFlags.Public | BindingFlags.Instance);
            var totalProperty = payload.GetType().GetProperty("total", BindingFlags.Public | BindingFlags.Instance);

            var items = Assert.IsAssignableFrom<IEnumerable<object>>(itemsProperty?.GetValue(payload)).ToList();
            var total = Assert.IsType<int>(totalProperty?.GetValue(payload));

            Assert.Equal(3, total);
            Assert.Contains(items, item => string.Equals((string?)item.GetType().GetProperty("sku")?.GetValue(item), "MATCH-001", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, item => string.Equals((string?)item.GetType().GetProperty("sku")?.GetValue(item), "MIKRO-ONLY-001", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, item => string.IsNullOrWhiteSpace((string?)item.GetType().GetProperty("sku")?.GetValue(item)));
            Assert.DoesNotContain(items, item => string.Equals((string?)item.GetType().GetProperty("sku")?.GetValue(item), "STALE-001", StringComparison.OrdinalIgnoreCase));
        }
    }
}

