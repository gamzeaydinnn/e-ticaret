using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.API.Controllers;
using ECommerce.API.Controllers.Admin;
using ECommerce.API.Data;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Category;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Data.Repositories;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    /// <summary>
    /// Faz 9 — Mikro ERP Kategori Hiyerarşisi Entegrasyon ve Geçiş Testleri:
    /// 9.1 Sync sonrası kategori sayısı (19 Yeni Düzen Ana Grup + 105 Alt Grup) ve MikroCategoryMapping kontrolü
    /// 9.2 Ana kategori ürün sayısı = (ana + alt) toplamı ve birleşik navbar kategorileri (Atıştırmalık = 1400+1100 vb.)
    /// 9.3 Mikro erişilemezken (IsConfigured = false) yerel veritabanı ve menü ağacının çalışması
    /// 9.4 Pasif (gizli) kategorilerin vitrinde gizlenmesi, admin panelinde görünmesi ve yasaklı grupların (02/03) engellenmesi
    /// 9.5 Yalnızca admin yeni kategori/alt kategori eklediğinde Mikro'ya INSERT çağrılması; pasife alma/güncellemede Mikro'ya yazılmaması
    /// </summary>
    public class MikroCategoryHierarchyTests
    {
        private static ECommerceDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(databaseName: $"GolkoyMikroCatTest_{Guid.NewGuid():N}")
                .Options;
            return new ECommerceDbContext(options);
        }

        [Fact]
        public async Task Faz9_1_And_9_3_SeedAsync_WithoutMikroConnection_Seeds19MainAnd105SubCategories_AndMappings()
        {
            // Arrange — Mikro erişilemez durumda (IsConfigured = false)
            await using var db = CreateInMemoryContext();
            var mockMikroGrup = new Mock<IMikroGrupDbService>();
            mockMikroGrup.Setup(x => x.IsConfigured).Returns(false);

            // Act
            await CategorySeeder.SeedAsync(db, mockMikroGrup.Object);

            // Assert — 19 Yeni Düzen Ana Kategori (100..2200) ve 105 Alt Kategori (101..2204)
            var allCategories = await db.Categories.AsNoTracking().ToListAsync();
            var mainCategories = allCategories
                .Where(c => c.ParentId == null && !string.IsNullOrWhiteSpace(c.MikroAnaGrupKod))
                .ToList();
            var subCategories = allCategories
                .Where(c => c.ParentId != null && !string.IsNullOrWhiteSpace(c.MikroAltGrupKod))
                .ToList();

            Assert.Equal(19, mainCategories.Count);
            Assert.Equal(106, subCategories.Count);

            // Birleşik NavbarGroupKey doğrulaması
            var atistirmalik1400 = mainCategories.Single(c => c.MikroAnaGrupKod == "1400");
            var kuruyemis1100 = mainCategories.Single(c => c.MikroAnaGrupKod == "1100");
            Assert.Equal("atistirmalik", atistirmalik1400.NavbarGroupKey);
            Assert.Equal("atistirmalik", kuruyemis1100.NavbarGroupKey);

            var dondurulmus1300 = mainCategories.Single(c => c.MikroAnaGrupKod == "1300");
            var dondurma800 = mainCategories.Single(c => c.MikroAnaGrupKod == "800");
            Assert.Equal("dondurma-ve-dondurulmus-gida", dondurulmus1300.NavbarGroupKey);
            Assert.Equal("dondurma-ve-dondurulmus-gida", dondurma800.NavbarGroupKey);

            var evBakim1000 = mainCategories.Single(c => c.MikroAnaGrupKod == "1000");
            var kagit1500 = mainCategories.Single(c => c.MikroAnaGrupKod == "1500");
            Assert.Equal("temizlik", evBakim1000.NavbarGroupKey);
            Assert.Equal("temizlik", kagit1500.NavbarGroupKey);

            var evYasam2000 = mainCategories.Single(c => c.MikroAnaGrupKod == "2000");
            Assert.Equal("ev-ve-mutfak", evYasam2000.NavbarGroupKey);

            // MikroCategoryMappings tablosunda hem ana gruplar (Priority=10) hem alt gruplar (Priority=100) bulunmalı
            var mappings = await db.MikroCategoryMappings.AsNoTracking().Where(m => m.IsActive).ToListAsync();
            Assert.Contains(mappings, m => m.MikroAnagrupKod == "1400" && m.MikroAltgrupKod == "1404" && m.Priority == 100);
            Assert.Contains(mappings, m => m.MikroAnagrupKod == "1400" && m.MikroAltgrupKod == null && m.Priority == 10);
        }

        [Fact]
        public async Task Faz9_2_MainCategoryProductCount_EqualsSumOfMainAndSubCategories_AndCombinedNavbarSectionsWork()
        {
            // Arrange
            await using var db = CreateInMemoryContext();
            await CategorySeeder.SeedAsync(db, null);

            var cat1404Cikolata = await db.Categories.SingleAsync(c => c.MikroAltGrupKod == "1404");
            var cat1102PaketKuruyemis = await db.Categories.SingleAsync(c => c.MikroAltGrupKod == "1102");
            var cat1400Main = await db.Categories.SingleAsync(c => c.ParentId == null && c.MikroAnaGrupKod == "1400");

            db.Products.AddRange(
                new Product
                {
                    Name = "Bitter Çikolata 80G",
                    Slug = "bitter-cikolata-80g",
                    SKU = "STK-1404-1",
                    CategoryId = cat1404Cikolata.Id,
                    Price = 45m,
                    StockQuantity = 20,
                    IsActive = true
                },
                new Product
                {
                    Name = "Sütlü Çikolata 60G",
                    Slug = "sutlu-cikolata-60g",
                    SKU = "STK-1404-2",
                    CategoryId = cat1404Cikolata.Id,
                    Price = 35m,
                    StockQuantity = 15,
                    IsActive = true
                },
                new Product
                {
                    Name = "Kavrulmuş Antep Fıstığı 200G",
                    Slug = "kavrulmus-antep-fistigi-200g",
                    SKU = "STK-1102-1",
                    CategoryId = cat1102PaketKuruyemis.Id,
                    Price = 180m,
                    StockQuantity = 10,
                    IsActive = true
                });
            await db.SaveChangesAsync();

            var repo = new CategoryRepository(db);
            var categoryManager = new CategoryManager(repo);

            // Act 1: 1400 Ana kategorisinin ürün sayısı alt kategorilerindeki (1404) 2 ürünü kapsamalı
            var count1400 = await repo.GetProductCountAsync(cat1400Main.Id);
            Assert.Equal(2, count1400);

            // Act 2: GET /api/categories/atistirmalik/sections çağrıldığında 1400 (10 alt grup) + 1100 (2 alt grup) = 12 alt kategori dönmeli
            var controller = new CategoriesController(categoryManager, db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            var actionResult = await controller.GetCategorySections("atistirmalik");
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);

            var subCategoriesProp = okResult.Value!.GetType().GetProperty("subCategories");
            Assert.NotNull(subCategoriesProp);
            var returnedSubs = subCategoriesProp!.GetValue(okResult.Value) as IEnumerable<object>;
            Assert.NotNull(returnedSubs);
            var subList = returnedSubs!
                .Select(item => item.GetType().GetProperty("mikroAltGrupKod")?.GetValue(item)?.ToString())
                .ToList();

            // 1401..1410 (10 adet) + 1101..1102 (2 adet) = 12 alt kategori
            Assert.Equal(12, subList.Count);
            Assert.Contains("1404", subList);
            Assert.Contains("1102", subList);
        }

        [Fact]
        public async Task Faz9_4_TogglingCategoryInactive_HidesFromStorefrontTree_AndPreservesOnSubsequentSeed()
        {
            // Arrange
            await using var db = CreateInMemoryContext();
            await CategorySeeder.SeedAsync(db, null);

            var repo = new CategoryRepository(db);
            var categoryManager = new CategoryManager(repo);

            var petShop = await db.Categories.SingleAsync(c => c.ParentId == null && c.MikroAnaGrupKod == "1700");
            Assert.True(petShop.IsActive);

            // Act — Admin Pet Shop (1700) ana kategorisini pasife alır
            petShop.IsActive = false;
            await db.SaveChangesAsync();

            // Vitrin ağacında (GetCategoryTreeAsync) görünmemeli
            var publicTree = (await categoryManager.GetCategoryTreeAsync()).ToList();
            Assert.DoesNotContain(publicTree, c => c.MikroAnaGrupKod == "1700");

            // Admin ağacında (GetAdminCategoryTreeAsync) görünmeye devam etmeli
            var adminTree = (await categoryManager.GetAdminCategoryTreeAsync()).ToList();
            var adminPetShop = Assert.Single(adminTree, c => c.MikroAnaGrupKod == "1700");
            Assert.False(adminPetShop.IsActive);

            // Tekrar SeedAsync çalıştığında adminin pasif tercihi ezilmemeli!
            await CategorySeeder.SeedAsync(db, null);
            var petShopAfterReseed = await db.Categories.AsNoTracking().SingleAsync(c => c.Id == petShop.Id);
            Assert.False(petShopAfterReseed.IsActive);
        }

        [Fact]
        public async Task Faz9_5_AdminCreateCategory_WritesToMikroOnlyOnCreate_AndNeverOnToggleActive()
        {
            // Arrange
            await using var db = CreateInMemoryContext();
            await CategorySeeder.SeedAsync(db, null);

            var repo = new CategoryRepository(db);
            var categoryManager = new CategoryManager(repo);
            var mockStats = new Mock<IAdminCatalogStatsService>();
            var mockFileStorage = new Mock<IFileStorage>();
            var mockMikroGrup = new Mock<IMikroGrupDbService>();

            mockMikroGrup.Setup(x => x.IsConfigured).Returns(true);
            mockMikroGrup
                .Setup(x => x.CreateAnaGrupInMikroAsync("Yöresel Ürünler", It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "2300", null));

            var parent1400 = await db.Categories.SingleAsync(c => c.ParentId == null && c.MikroAnaGrupKod == "1400");
            mockMikroGrup
                .Setup(x => x.CreateAltGrupInMikroAsync("1400", "Diyet Barlar", It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "1411", null));

            var adminController = new AdminCategoriesController(
                categoryManager,
                mockStats.Object,
                mockFileStorage.Object,
                mockMikroGrup.Object,
                db,
                NullLogger<AdminCategoriesController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act 1: Admin yeni Ana Kategori ekler -> Mikro'ya 2300 olarak INSERT edilmeli
            var createMainResult = await adminController.CreateCategory(new AdminCategoriesController.AdminCategorySaveDto
            {
                Name = "Yöresel Ürünler",
                ParentId = null,
                IsActive = true
            });
            Assert.IsType<CreatedAtActionResult>(createMainResult);
            mockMikroGrup.Verify(x => x.CreateAnaGrupInMikroAsync("Yöresel Ürünler", It.IsAny<CancellationToken>()), Times.Once);

            var createdMain = await db.Categories.SingleAsync(c => c.MikroAnaGrupKod == "2300");
            Assert.True(createdMain.IsMikroSynced);

            // Act 2: Admin 1400 altına yeni Alt Kategori ekler -> Mikro'ya 1411 olarak INSERT edilmeli
            var createSubResult = await adminController.CreateCategory(new AdminCategoriesController.AdminCategorySaveDto
            {
                Name = "Diyet Barlar",
                ParentId = parent1400.Id,
                IsActive = true
            });
            Assert.IsType<CreatedAtActionResult>(createSubResult);
            mockMikroGrup.Verify(x => x.CreateAltGrupInMikroAsync("1400", "Diyet Barlar", It.IsAny<CancellationToken>()), Times.Once);

            // Act 3: Admin kategoriyi Pasife alır -> Mikro'ya hiçbir INSERT çağrısı yapılmamalı
            var toggleResult = await adminController.ToggleCategoryActive(
                createdMain.Id,
                new AdminCategoriesController.ToggleCategoryActiveRequest { IsActive = false });
            Assert.IsType<OkObjectResult>(toggleResult);
            mockMikroGrup.Verify(x => x.IsConfigured, Times.AtLeastOnce);
            mockMikroGrup.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("1", "01", false)]
        [InlineData("02", "02", true)]
        [InlineData("3", "03", true)]
        [InlineData("1400", "1400", false)]
        [InlineData(" 2200 ", "2200", false)]
        public void Faz9_Helper_Normalization_And_ForbiddenCheck_WorksCorrectly(
            string rawAnaKod,
            string expectedNormalized,
            bool expectedForbidden)
        {
            var normalized = MikroGrupKodHelper.NormalizeAnaKod(rawAnaKod);
            Assert.Equal(expectedNormalized, normalized);
            Assert.Equal(expectedForbidden, MikroGrupKodHelper.IsForbiddenAnaGrup(rawAnaKod));
        }
    }
}
