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
    }
}
