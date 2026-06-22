using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.API.Data;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests.Data
{
    public class CategorySeederTests
    {
        private static ECommerceDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task SeedAsync_ShouldRemoveNumericCategories_AndMoveProductsToDiger()
        {
            await using var context = CreateContext();

            var numericCategory = new Category
            {
                Id = 42,
                Name = "1001",
                Slug = "1001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(numericCategory);
            context.Products.Add(new Product
            {
                Id = 5,
                Name = "Test Ürün",
                SKU = "TEST-5",
                CategoryId = numericCategory.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroCategoryMappings.Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "1001",
                CategoryId = numericCategory.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await CategorySeeder.SeedAsync(context);

            var digerCategory = await context.Categories.SingleAsync(category => category.Slug == CategorySeeder.UncategorizedSlug);
            var product = await context.Products.SingleAsync(item => item.Id == 5);

            Assert.Equal(digerCategory.Id, product.CategoryId);
            Assert.DoesNotContain(context.Categories, category => category.Slug == "1001");
            Assert.DoesNotContain(context.MikroCategoryMappings, mapping => mapping.MikroAnagrupKod == "1001");
        }

        [Fact]
        public async Task SeedAsync_ShouldCreateFrozenCategory_AndRouteFrozenMappingsThere()
        {
            await using var context = CreateContext();

            await CategorySeeder.SeedAsync(context);

            var frozenCategory = await context.Categories.SingleAsync(category => category.Slug == "dondurma-ve-dondurulmus-gida");
            var mappings = await context.MikroCategoryMappings
                .Where(mapping => mapping.MikroAnagrupKod == "DONDURMA" ||
                                  mapping.MikroAnagrupKod == "DONMUS URUN" ||
                                  mapping.MikroAnagrupKod == "1300")
                .ToListAsync();

            Assert.Equal(6, frozenCategory.SortOrder);
            Assert.Equal(3, mappings.Count);
            Assert.All(mappings, mapping => Assert.Equal(frozenCategory.Id, mapping.CategoryId));
        }

        [Fact]
        public async Task SeedAsync_ShouldMoveSuperfreshProducts_ToFrozenCategory()
        {
            await using var context = CreateContext();

            var temelGida = new Category
            {
                Id = 50,
                Name = "Temel Gıda",
                Slug = "temel-gida",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(temelGida);
            context.Products.Add(new Product
            {
                Id = 51,
                Name = "Superfresh Mantı",
                SKU = "SF-001",
                CategoryId = temelGida.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroProductCaches.Add(new MikroProductCache
            {
                LocalProductId = 51,
                StokKod = "SF-001",
                StokAd = "SUPERFRESH MANTI",
                AnagrupKod = "TEMEL GIDA",
                Aktif = true,
                GuncellemeTarihi = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await CategorySeeder.SeedAsync(context);

            var frozenCategory = await context.Categories.SingleAsync(category => category.Slug == "dondurma-ve-dondurulmus-gida");
            var product = await context.Products.SingleAsync(item => item.Id == 51);

            Assert.Equal(frozenCategory.Id, product.CategoryId);
        }

        [Fact]
        public async Task SeedAsync_ShouldMoveAlgidaProducts_FromMilkCategory_ToFrozenCategory()
        {
            await using var context = CreateContext();

            var sutKategori = new Category
            {
                Id = 61,
                Name = "Süt Ürünleri",
                Slug = "sut-ve-sut-urunleri",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(sutKategori);
            context.Products.Add(new Product
            {
                Id = 62,
                Name = "Algida Cornetto Classico 110 ML",
                SKU = "ALG-001",
                CategoryId = sutKategori.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroProductCaches.Add(new MikroProductCache
            {
                LocalProductId = 62,
                StokKod = "ALG-001",
                StokAd = "ALGIDA CORNETTO CLASSICO 110 ML",
                AnagrupKod = "800",
                Aktif = true,
                GuncellemeTarihi = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await CategorySeeder.SeedAsync(context);

            var frozenCategory = await context.Categories.SingleAsync(category => category.Slug == "dondurma-ve-dondurulmus-gida");
            var product = await context.Products.SingleAsync(item => item.Id == 62);
            var legacyMapping = await context.MikroCategoryMappings.SingleAsync(mapping => mapping.MikroAnagrupKod == "800");

            Assert.Equal(frozenCategory.Id, product.CategoryId);
            Assert.Equal(frozenCategory.Id, legacyMapping.CategoryId);
        }

        [Fact]
        public async Task SeedAsync_ShouldMoveSucukProducts_FromMilkCategory_ToMeatCategory()
        {
            await using var context = CreateContext();

            var sutKategori = new Category
            {
                Id = 71,
                Name = "Süt Ürünleri",
                Slug = "sut-ve-sut-urunleri",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(sutKategori);
            context.Products.Add(new Product
            {
                Id = 72,
                Name = "Dana Sucuk 250 GR",
                SKU = "ET-001",
                CategoryId = sutKategori.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroProductCaches.Add(new MikroProductCache
            {
                LocalProductId = 72,
                StokKod = "ET-001",
                StokAd = "DANA SUCUK 250 GR",
                AnagrupKod = "SUT URUNLERI",
                Aktif = true,
                GuncellemeTarihi = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await CategorySeeder.SeedAsync(context);

            var etKategori = await context.Categories.SingleAsync(category => category.Slug == "et-ve-et-urunleri");
            var product = await context.Products.SingleAsync(item => item.Id == 72);

            Assert.Equal(etKategori.Id, product.CategoryId);
        }

        [Fact]
        public async Task SeedAsync_ShouldMoveMilkNamedProducts_FromBeverageCategory_ToMilkCategory()
        {
            await using var context = CreateContext();

            var icecekKategori = new Category
            {
                Id = 81,
                Name = "İçecekler",
                Slug = "icecekler",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(icecekKategori);
            context.Products.Add(new Product
            {
                Id = 82,
                Name = "Hindistan Cevizi Sütü 1 LT",
                SKU = "SUT-002",
                CategoryId = icecekKategori.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.MikroProductCaches.Add(new MikroProductCache
            {
                LocalProductId = 82,
                StokKod = "SUT-002",
                StokAd = "HINDISTAN CEVIZI SUTU 1 LT",
                AnagrupKod = "ICECEKLER",
                Aktif = true,
                GuncellemeTarihi = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            await CategorySeeder.SeedAsync(context);

            var sutKategori = await context.Categories.SingleAsync(category => category.Slug == "sut-ve-sut-urunleri");
            var product = await context.Products.SingleAsync(item => item.Id == 82);

            Assert.Equal(sutKategori.Id, product.CategoryId);
        }
    }
}
