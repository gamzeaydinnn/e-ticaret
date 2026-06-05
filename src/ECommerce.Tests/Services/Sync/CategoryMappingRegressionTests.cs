using ECommerce.Business.Services.Mapping;
using ECommerce.Business.Services.Sync;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services.Sync
{
    public class CategoryMappingRegressionTests
    {
        private static ECommerceDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ECommerceDbContext(options);
        }

        private static IConfiguration CreateConfiguration(bool autoCreateCategories)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CategoryMapping:AutoCreateCategories"] = autoCreateCategories.ToString()
                })
                .Build();
        }

        [Fact]
        public async Task SyncBatchProductInfoAsync_UsesAnagrupKodForCategoryLookup()
        {
            using var context = CreateInMemoryContext();

            context.Products.Add(new Product
            {
                Id = 1,
                Name = "Dana Kuşbaşı",
                SKU = "SKU-1",
                CategoryId = 1,
                IsActive = true
            });

            context.MikroProductCaches.Add(new MikroProductCache
            {
                StokKod = "SKU-1",
                StokAd = "Dana Kuşbaşı",
                LocalProductId = 1,
                AnagrupKod = "ET URUNLERI",
                GrupKod = "DANA",
                Aktif = true
            });

            context.Set<MikroCategoryMapping>().Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "ET URUNLERI",
                CategoryId = 7,
                Priority = 10,
                IsActive = true
            });

            await context.SaveChangesAsync();

            var service = new ProductInfoSyncService(
                context,
                Mock.Of<ILogger<ProductInfoSyncService>>());

            var result = await service.SyncBatchProductInfoAsync(new[] { "SKU-1" });

            var product = await context.Products.SingleAsync(p => p.Id == 1);
            Assert.True(result.Success);
            Assert.Equal(1, result.CategoriesUpdated);
            Assert.Equal(7, product.CategoryId);
        }

        [Fact]
        public async Task SuggestMappingAsync_UsesKeywordHintBeforeFuzzyMatching()
        {
            using var context = CreateInMemoryContext();
            context.Categories.AddRange(
                new Category { Name = "Süt Ürünleri", Slug = "sut-ve-sut-urunleri", IsActive = true },
                new Category { Name = "Temel Gıda", Slug = "temel-gida", IsActive = true });
            await context.SaveChangesAsync();

            var mappingServiceMock = new Mock<IMikroCategoryMappingService>();
            var engine = new AutoCategoryMappingEngine(
                context,
                mappingServiceMock.Object,
                Mock.Of<ILogger<AutoCategoryMappingEngine>>(),
                CreateConfiguration(autoCreateCategories: false));

            var suggestions = await engine.SuggestMappingAsync("PEYNIR");

            var best = Assert.Single(suggestions);
            Assert.Equal("sut-ve-sut-urunleri", best.CategorySlug);
            Assert.Equal("keyword_hint", best.MatchType);
            Assert.Equal(0.95, best.Score);
        }

        [Fact]
        public async Task SuggestMappingAsync_ShouldRouteSuperfreshToFrozenCategory()
        {
            using var context = CreateInMemoryContext();
            context.Categories.AddRange(
                new Category { Name = "Dondurma & Dondurulmuş Gıda", Slug = "dondurma-ve-dondurulmus-gida", IsActive = true },
                new Category { Name = "Temel Gıda", Slug = "temel-gida", IsActive = true });
            await context.SaveChangesAsync();

            var mappingServiceMock = new Mock<IMikroCategoryMappingService>();
            var engine = new AutoCategoryMappingEngine(
                context,
                mappingServiceMock.Object,
                Mock.Of<ILogger<AutoCategoryMappingEngine>>(),
                CreateConfiguration(autoCreateCategories: false));

            var suggestions = await engine.SuggestMappingAsync("TEMEL GIDA", "SUPERFRESH");

            var best = Assert.Single(suggestions);
            Assert.Equal("dondurma-ve-dondurulmus-gida", best.CategorySlug);
            Assert.Equal("keyword_hint", best.MatchType);
        }

        [Fact]
        public async Task ResolveOrCreateMappingAsync_FallsBackToGrupKod_AndPreservesTurkishCasing()
        {
            using var context = CreateInMemoryContext();
            var digerCategory = new Category
            {
                Id = 91,
                Name = "Diğer",
                Slug = "diger",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Categories.Add(digerCategory);
            await context.SaveChangesAsync();

            var mappingServiceMock = new Mock<IMikroCategoryMappingService>();
            mappingServiceMock
                .Setup(service => service.GetMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MikroCategoryMapping?)null);
            mappingServiceMock
                .Setup(service => service.AddMappingAsync(It.IsAny<MikroCategoryMapping>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MikroCategoryMapping mapping, CancellationToken _) => mapping);

            var engine = new AutoCategoryMappingEngine(
                context,
                mappingServiceMock.Object,
                Mock.Of<ILogger<AutoCategoryMappingEngine>>(),
                CreateConfiguration(autoCreateCategories: true));

            var categoryId = await engine.ResolveOrCreateMappingAsync(string.Empty, "SÜT_ÜRÜNLERİ");

            Assert.Equal(digerCategory.Id, categoryId);
            Assert.Single(await context.Categories.ToListAsync());
            mappingServiceMock.Verify(
                service => service.AddMappingAsync(
                    It.Is<MikroCategoryMapping>(mapping =>
                        mapping.MikroAnagrupKod == "SÜT_ÜRÜNLERİ" &&
                        mapping.MikroAltgrupKod == null &&
                        mapping.CategoryId == digerCategory.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
