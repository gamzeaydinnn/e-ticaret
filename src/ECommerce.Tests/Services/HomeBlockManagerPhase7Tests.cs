using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class HomeBlockManagerPhase7Tests
    {
        private static ECommerceDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ECommerceDbContext(options);
        }

        private static HomeBlockManager CreateManager(
            ECommerceDbContext context,
            IMemoryCache cache,
            Mock<IHomeBlockRepository>? repositoryMock = null)
        {
            repositoryMock ??= new Mock<IHomeBlockRepository>();
            var loggerMock = new Mock<ILogger<HomeBlockManager>>();
            return new HomeBlockManager(repositoryMock.Object, context, cache, loggerMock.Object);
        }

        // ====================================================================
        // 7.1.1: Poster Linki Öncelik Testi (Banner.LinkUrl > PosterLinkUrl > null)
        // ====================================================================

        [Fact]
        public async Task GetPosterClickTargetAsync_ShouldPrioritizeBannerLinkUrl_WhenBothExist()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var banner = new Banner
            {
                Title = "Banner Kampanya",
                ImageUrl = "/uploads/banner.jpg",
                LinkUrl = "https://example.com/banner-campaign",
                IsActive = true
            };
            context.Banners.Add(banner);
            await context.SaveChangesAsync();

            var block = new HomeProductBlock
            {
                Name = "Test Bloğu",
                Slug = "test-blogu",
                BannerId = banner.Id,
                Banner = banner,
                PosterLinkUrl = "/category/direct-link",
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var target = await manager.GetPosterClickTargetAsync(block.Id);

            Assert.Equal("https://example.com/banner-campaign", target);
        }

        [Fact]
        public async Task GetPosterClickTargetAsync_ShouldUsePosterLinkUrl_WhenBannerHasNoLink()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var banner = new Banner
            {
                Title = "Link Olmayan Banner",
                ImageUrl = "/uploads/banner.jpg",
                LinkUrl = "",
                IsActive = true
            };
            context.Banners.Add(banner);
            await context.SaveChangesAsync();

            var block = new HomeProductBlock
            {
                Name = "Test Bloğu",
                Slug = "test-blogu",
                BannerId = banner.Id,
                Banner = banner,
                PosterLinkUrl = "/category/sut-urunleri",
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var target = await manager.GetPosterClickTargetAsync(block.Id);

            Assert.Equal("/category/sut-urunleri", target);
        }

        [Fact]
        public async Task GetPosterClickTargetAsync_ShouldUsePosterLinkUrl_WhenBannerIdIsNull()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var block = new HomeProductBlock
            {
                Name = "Test Bloğu",
                Slug = "test-blogu",
                BannerId = null,
                PosterLinkUrl = "https://golkoygurme.com/firsatlar",
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var target = await manager.GetPosterClickTargetAsync(block.Id);

            Assert.Equal("https://golkoygurme.com/firsatlar", target);
        }

        [Fact]
        public async Task GetPosterClickTargetAsync_ShouldReturnNull_WhenNeitherExists()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var block = new HomeProductBlock
            {
                Name = "Hedefsiz Blok",
                Slug = "hedefsiz-blok",
                BannerId = null,
                PosterLinkUrl = null,
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var target = await manager.GetPosterClickTargetAsync(block.Id);

            Assert.Null(target);
        }

        // ====================================================================
        // 7.1.2 & 7.1.3: Hiyerarşik Kategori Ürün Sorgulama Testleri
        // ====================================================================

        [Fact]
        public async Task GetBlockProductsAsync_ShouldIncludeParentAndSubcategoryProducts_WhenParentCategorySelected()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Üst kategori (Parent)
            var parentCat = new Category
            {
                Name = "Gıda",
                Slug = "gida",
                ParentId = null,
                IsActive = true
            };
            context.Categories.Add(parentCat);
            await context.SaveChangesAsync();

            // Alt kategori (Subcategory)
            var subCat = new Category
            {
                Name = "Süt & Kahvaltılık",
                Slug = "sut-kahvaltilik",
                ParentId = parentCat.Id,
                IsActive = true
            };
            // Başka bağımsız kategori
            var otherCat = new Category
            {
                Name = "Temizlik",
                Slug = "temizlik",
                ParentId = null,
                IsActive = true
            };
            context.Categories.AddRange(subCat, otherCat);
            await context.SaveChangesAsync();

            // Ürünler
            var prodInParent = new Product
            {
                Name = "Gıda Genel Ürün",
                Price = 50m,
                CategoryId = parentCat.Id,
                IsActive = true,
                StockQuantity = 10
            };
            var prodInSub = new Product
            {
                Name = "Organik Süt",
                Price = 30m,
                CategoryId = subCat.Id,
                IsActive = true,
                StockQuantity = 20
            };
            var prodInOther = new Product
            {
                Name = "Deterjan",
                Price = 80m,
                CategoryId = otherCat.Id,
                IsActive = true,
                StockQuantity = 15
            };
            context.Products.AddRange(prodInParent, prodInSub, prodInOther);
            await context.SaveChangesAsync();

            // Kategori bazlı blok: TargetCategoryId = parentCat.Id
            var block = new HomeProductBlock
            {
                Name = "Gıda Bloğu",
                Slug = "gida-blogu",
                BlockType = "category",
                TargetCategoryId = parentCat.Id,
                MaxProductCount = 10,
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var products = await manager.GetBlockProductsAsync(block.Id, block.MaxProductCount);

            Assert.Equal(2, products.Count);
            Assert.Contains(products, p => p.Id == prodInParent.Id);
            Assert.Contains(products, p => p.Id == prodInSub.Id);
            Assert.DoesNotContain(products, p => p.Id == prodInOther.Id);
        }

        [Fact]
        public async Task GetBlockProductsAsync_ShouldOnlyIncludeSubcategoryProducts_WhenSubcategorySelected()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var parentCat = new Category
            {
                Name = "Gıda",
                Slug = "gida",
                ParentId = null,
                IsActive = true
            };
            context.Categories.Add(parentCat);
            await context.SaveChangesAsync();

            var subCat = new Category
            {
                Name = "Süt Ürünleri",
                Slug = "sut-urunleri",
                ParentId = parentCat.Id,
                IsActive = true
            };
            context.Categories.Add(subCat);
            await context.SaveChangesAsync();

            var prodInParent = new Product
            {
                Name = "Bakliyat Genel",
                Price = 45m,
                CategoryId = parentCat.Id,
                IsActive = true,
                StockQuantity = 10
            };
            var prodInSub = new Product
            {
                Name = "Taze Kaşar",
                Price = 120m,
                CategoryId = subCat.Id,
                IsActive = true,
                StockQuantity = 10
            };
            context.Products.AddRange(prodInParent, prodInSub);
            await context.SaveChangesAsync();

            // Kategori bazlı blok: TargetCategoryId = subCat.Id (Alt Kategori)
            var block = new HomeProductBlock
            {
                Name = "Süt Ürünleri Bloğu",
                Slug = "sut-urunleri-blogu",
                BlockType = "category",
                TargetCategoryId = subCat.Id,
                MaxProductCount = 10,
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var products = await manager.GetBlockProductsAsync(block.Id, block.MaxProductCount);

            Assert.Single(products);
            Assert.Equal(prodInSub.Id, products[0].Id);
        }

        // ====================================================================
        // 7.1.4: TargetCategoryId Validasyon Testleri
        // ====================================================================

        [Fact]
        public async Task ValidateTargetCategoryAsync_ShouldReturnFalse_WhenCategoryNotFound()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var manager = CreateManager(context, memoryCache);

            var (isValid, errorMessage) = await manager.ValidateTargetCategoryAsync(99999);

            Assert.False(isValid);
            Assert.Equal("Seçilen kategori bulunamadı.", errorMessage);
        }

        [Fact]
        public async Task ValidateTargetCategoryAsync_ShouldReturnFalse_WhenCategoryIsInactive()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var inactiveCategory = new Category
            {
                Name = "Pasif Kategori",
                Slug = "pasif-kategori",
                IsActive = false
            };
            context.Categories.Add(inactiveCategory);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var (isValid, errorMessage) = await manager.ValidateTargetCategoryAsync(inactiveCategory.Id);

            Assert.False(isValid);
            Assert.Equal("Seçilen kategori pasif durumda.", errorMessage);
        }

        [Fact]
        public async Task ValidateTargetCategoryAsync_ShouldReturnTrue_WhenCategoryNullOrActive()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var activeCategory = new Category
            {
                Name = "Aktif Kategori",
                Slug = "aktif-kategori",
                IsActive = true
            };
            context.Categories.Add(activeCategory);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            var nullResult = await manager.ValidateTargetCategoryAsync(null);
            Assert.True(nullResult.IsValid);
            Assert.Null(nullResult.ErrorMessage);

            var activeResult = await manager.ValidateTargetCategoryAsync(activeCategory.Id);
            Assert.True(activeResult.IsValid);
            Assert.Null(activeResult.ErrorMessage);
        }

        // ====================================================================
        // 7.1.5 & 7.3: Analitik Sayaçları ve Önbellekleme (Caching & Invalidation)
        // ====================================================================

        [Fact]
        public async Task Analytics_TrackPosterClick_And_TrackBlockView_ShouldIncrementCounters()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var block = new HomeProductBlock
            {
                Name = "Analitik Blok",
                Slug = "analitik-blok",
                ClickCount = 0,
                ViewCount = 0,
                IsActive = true
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            await manager.TrackPosterClickAsync(block.Id);
            await manager.TrackPosterClickAsync(block.Id);
            await manager.TrackBlockViewAsync(block.Id);

            var updatedBlock = await context.HomeProductBlocks.FindAsync(block.Id);
            Assert.NotNull(updatedBlock);
            Assert.Equal(2, updatedBlock!.ClickCount);
            Assert.Equal(1, updatedBlock.ViewCount);
        }

        [Fact]
        public async Task GetAllActiveBlocksAsync_ShouldCacheResults_AndInvalidateOnInteraction()
        {
            using var context = GetInMemoryDbContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var block = new HomeProductBlock
            {
                Name = "Cache Test Bloğu",
                Slug = "cache-test-blogu",
                IsActive = true,
                DisplayOrder = 1
            };
            context.HomeProductBlocks.Add(block);
            await context.SaveChangesAsync();

            var manager = CreateManager(context, memoryCache);

            // 1. Çağrı: Cache'te yok, DB'den yüklenir ve cache'e yazılır
            var firstCall = await manager.GetAllActiveBlocksAsync();
            Assert.Single(firstCall);

            // Cache kontrolü
            Assert.True(memoryCache.TryGetValue("active_home_blocks", out _));

            // DB'ye yeni blok ekleyelim ama cache temizlenmediği sürece dönmemeli
            var newBlock = new HomeProductBlock
            {
                Name = "İkinci Blok",
                Slug = "ikinci-blok",
                IsActive = true,
                DisplayOrder = 2
            };
            context.HomeProductBlocks.Add(newBlock);
            await context.SaveChangesAsync();

            // 2. Çağrı: Cache'ten döneceği için hala 1 eleman olmalı
            var cachedCall = await manager.GetAllActiveBlocksAsync();
            Assert.Single(cachedCall);

            // Tıklama yapıldığında InvalidateCache() tetiklenir
            await manager.TrackPosterClickAsync(block.Id);

            // Cache artık temizlendi
            Assert.False(memoryCache.TryGetValue("active_home_blocks", out _));

            // 3. Çağrı: Cache boş olduğu için güncel DB'den çeker ve 2 eleman döner
            var freshCall = await manager.GetAllActiveBlocksAsync();
            Assert.Equal(2, freshCall.Count);
        }
    }
}
