using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.HomeBlock;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu Servis Manager'ı
    /// ------------------------------------------------
    /// Business logic katmanı - Blok yönetimi ve ürün filtreleme işlemleri.
    /// 
    /// Blok Tipleri ve Ürün Kaynakları:
    /// - manual: HomeBlockProduct tablosundan (admin seçimi)
    /// - category: Product tablosu CategoryId filtresi
    /// - discounted: Product tablosu SpecialPrice != null && SpecialPrice < Price
    /// - newest: Product tablosu CreatedAt DESC
    /// - bestseller: OrderItems sayısına göre (future)
    /// 
    /// Slug Oluşturma:
    /// - Türkçe karakterler çevrilir (ğ→g, ü→u, ş→s vb.)
    /// - Boşluklar tire ile değiştirilir
    /// - Özel karakterler kaldırılır
    /// </summary>
    public class HomeBlockManager : IHomeBlockService
    {
        private readonly IHomeBlockRepository _repository;
        private readonly ECommerceDbContext _context;
        private readonly ILogger<HomeBlockManager> _logger;

        public HomeBlockManager(
            IHomeBlockRepository repository,
            ECommerceDbContext context,
            ILogger<HomeBlockManager> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Public Endpoints (Ana Sayfa İçin)

        /// <summary>
        /// Ana sayfa için aktif blokları ürünleriyle birlikte getirir
        /// Her blok tipine göre farklı ürün kaynağı kullanılır
        /// </summary>
        public async Task<IEnumerable<HomeProductBlockDto>> GetActiveBlocksForHomepageAsync()
        {
            try
            {
                _logger.LogInformation("🏠 Ana sayfa blokları getiriliyor...");

                var blocks = await _repository.GetActiveBlocksWithProductsAsync();
                var result = new List<HomeProductBlockDto>();

                foreach (var block in blocks)
                {
                    var dto = MapToDto(block);
                    
                    // Blok tipine göre ürünleri doldur
                    dto.Products = await GetProductsForBlockAsync(block);
                    
                    result.Add(dto);
                }

                _logger.LogInformation("✅ {Count} aktif blok döndürüldü", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ana sayfa blokları getirilirken hata oluştu");
                throw;
            }
        }

        /// <summary>
        /// Slug'a göre tek blok getirir - Tümünü Gör sayfası için
        /// MaxProductCount sınırı uygulanmaz, tüm ürünler döner
        /// </summary>
        public async Task<HomeProductBlockDto?> GetBlockBySlugAsync(string slug)
        {
            try
            {
                _logger.LogInformation("🔍 Blok getiriliyor: {Slug}", slug);

                var block = await _repository.GetBySlugAsync(slug);
                if (block == null)
                {
                    _logger.LogWarning("⚠️ Blok bulunamadı: {Slug}", slug);
                    return null;
                }

                var dto = MapToDto(block);
                
                // Tümünü Gör sayfası için tüm ürünleri getir (limit yok)
                dto.Products = await GetProductsForBlockAsync(block, includeAll: true);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok getirilirken hata: {Slug}", slug);
                throw;
            }
        }

        #endregion

        #region Admin CRUD Operations

        /// <summary>
        /// Tüm blokları getirir - Admin listesi için
        /// </summary>
        public async Task<IEnumerable<HomeProductBlockDto>> GetAllBlocksAsync()
        {
            var blocks = await _repository.GetAllAsync();
            return blocks.Select(MapToDto);
        }

        /// <summary>
        /// ID'ye göre blok detayı getirir
        /// </summary>
        public async Task<HomeProductBlockDto?> GetBlockByIdAsync(int id)
        {
            var block = await _repository.GetByIdWithProductsAsync(id);
            if (block == null) return null;

            var dto = MapToDto(block);
            dto.Products = await GetProductsForBlockAsync(block, includeAll: true);
            
            return dto;
        }

        /// <summary>
        /// Yeni blok oluşturur
        /// Slug otomatik oluşturulur ve benzersizlik kontrol edilir
        /// </summary>
        public async Task<HomeProductBlockDto> CreateBlockAsync(CreateHomeBlockDto dto)
        {
            try
            {
                _logger.LogInformation("➕ Yeni blok oluşturuluyor: {Name}", dto.Name);

                var block = new HomeProductBlock
                {
                    Name = dto.Name,
                    Slug = await GenerateUniqueSlugAsync(dto.Name),
                    Description = dto.Description,
                    BlockType = dto.BlockType,
                    CategoryId = dto.CategoryId,
                    BannerId = dto.BannerId,
                    PosterImageUrl = dto.PosterImageUrl,
                    BackgroundColor = dto.BackgroundColor,
                    DisplayOrder = dto.DisplayOrder,
                    MaxProductCount = dto.MaxProductCount,
                    ViewAllUrl = dto.ViewAllUrl,
                    ViewAllText = dto.ViewAllText ?? "Tümünü Gör",
                    IsActive = dto.IsActive,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate
                };

                // ViewAllUrl otomatik oluştur (belirtilmemişse)
                if (string.IsNullOrEmpty(block.ViewAllUrl))
                {
                    block.ViewAllUrl = GenerateViewAllUrl(block);
                }

                var created = await _repository.AddAsync(block);
                _logger.LogInformation("✅ Blok oluşturuldu: #{Id} - {Name}", created.Id, created.Name);

                return MapToDto(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok oluşturulurken hata: {Name}", dto.Name);
                throw;
            }
        }

        /// <summary>
        /// Mevcut bloğu günceller
        /// </summary>
        public async Task<HomeProductBlockDto?> UpdateBlockAsync(int id, UpdateHomeBlockDto dto)
        {
            try
            {
                var block = await _repository.GetByIdAsync(id);
                if (block == null)
                {
                    _logger.LogWarning("⚠️ Güncellenecek blok bulunamadı: #{Id}", id);
                    return null;
                }

                _logger.LogInformation("✏️ Blok güncelleniyor: #{Id} - {Name}", id, dto.Name);

                // Ad değiştiyse slug'ı güncelle
                if (block.Name != dto.Name)
                {
                    block.Slug = await GenerateUniqueSlugAsync(dto.Name, id);
                }

                block.Name = dto.Name;
                block.Description = dto.Description;
                block.BlockType = dto.BlockType;
                block.CategoryId = dto.CategoryId;
                block.BannerId = dto.BannerId;
                block.PosterImageUrl = dto.PosterImageUrl;
                block.BackgroundColor = dto.BackgroundColor;
                block.DisplayOrder = dto.DisplayOrder;
                block.MaxProductCount = dto.MaxProductCount;
                block.ViewAllText = dto.ViewAllText ?? "Tümünü Gör";
                block.IsActive = dto.IsActive;
                block.StartDate = dto.StartDate;
                block.EndDate = dto.EndDate;

                // ViewAllUrl güncelle
                block.ViewAllUrl = string.IsNullOrEmpty(dto.ViewAllUrl) 
                    ? GenerateViewAllUrl(block) 
                    : dto.ViewAllUrl;

                await _repository.UpdateAsync(block);
                _logger.LogInformation("✅ Blok güncellendi: #{Id}", id);

                return MapToDto(block);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok güncellenirken hata: #{Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Bloğu siler
        /// </summary>
        public async Task<bool> DeleteBlockAsync(int id)
        {
            try
            {
                var block = await _repository.GetByIdAsync(id);
                if (block == null)
                {
                    _logger.LogWarning("⚠️ Silinecek blok bulunamadı: #{Id}", id);
                    return false;
                }

                _logger.LogInformation("🗑️ Blok siliniyor: #{Id} - {Name}", id, block.Name);
                
                await _repository.DeleteAsync(id);
                
                _logger.LogInformation("✅ Blok silindi: #{Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok silinirken hata: #{Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Blok sıralamasını değiştirir
        /// </summary>
        public async Task<bool> UpdateBlockOrderAsync(int id, int newDisplayOrder)
        {
            var block = await _repository.GetByIdAsync(id);
            if (block == null) return false;

            block.DisplayOrder = newDisplayOrder;
            await _repository.UpdateAsync(block);
            
            return true;
        }

        /// <summary>
        /// Blokları toplu sıralar
        /// </summary>
        public async Task UpdateBlocksOrderAsync(IEnumerable<(int id, int displayOrder)> orders)
        {
            foreach (var order in orders)
            {
                await UpdateBlockOrderAsync(order.id, order.displayOrder);
            }
        }

        #endregion

        #region Block Products (Ürün Yönetimi)

        /// <summary>
        /// Bloğa ürün ekler
        /// </summary>
        public async Task<bool> AddProductToBlockAsync(AddProductToBlockDto dto)
        {
            try
            {
                await _repository.AddProductToBlockAsync(dto.BlockId, dto.ProductId, dto.DisplayOrder);
                _logger.LogInformation("✅ Ürün bloğa eklendi: Block#{BlockId} - Product#{ProductId}", 
                    dto.BlockId, dto.ProductId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ürün bloğa eklenirken hata");
                return false;
            }
        }

        /// <summary>
        /// Bloğa birden fazla ürün ekler
        /// </summary>
        public async Task<bool> AddProductsToBlockAsync(int blockId, IEnumerable<int> productIds)
        {
            try
            {
                var productsWithOrder = productIds.Select((id, index) => (id, index)).ToList();
                await _repository.AddProductsToBlockAsync(blockId, productsWithOrder);
                _logger.LogInformation("✅ {Count} ürün bloğa eklendi: Block#{BlockId}", 
                    productsWithOrder.Count, blockId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ürünler bloğa eklenirken hata");
                return false;
            }
        }

        /// <summary>
        /// Bloktan ürün çıkarır
        /// </summary>
        public async Task<bool> RemoveProductFromBlockAsync(int blockId, int productId)
        {
            try
            {
                await _repository.RemoveProductFromBlockAsync(blockId, productId);
                _logger.LogInformation("✅ Ürün bloktan çıkarıldı: Block#{BlockId} - Product#{ProductId}", 
                    blockId, productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ürün bloktan çıkarılırken hata");
                return false;
            }
        }

        /// <summary>
        /// Bloktaki ürünleri günceller (sıralama, aktiflik)
        /// </summary>
        public async Task<bool> UpdateBlockProductsAsync(UpdateBlockProductsDto dto)
        {
            try
            {
                var products = dto.Products.Select(p => (p.ProductId, p.DisplayOrder, p.IsActive));
                await _repository.UpdateBlockProductsOrderAsync(dto.BlockId, products);
                _logger.LogInformation("✅ Blok ürünleri güncellendi: Block#{BlockId}", dto.BlockId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok ürünleri güncellenirken hata");
                return false;
            }
        }

        /// <summary>
        /// Bloktaki ürün listesini tamamen yeniler
        /// </summary>
        public async Task<bool> SetBlockProductsAsync(int blockId, IEnumerable<int> productIds)
        {
            try
            {
                // Önce tüm ürünleri temizle
                await _repository.ClearBlockProductsAsync(blockId);
                
                // Sonra yeni ürünleri ekle
                if (productIds.Any())
                {
                    await AddProductsToBlockAsync(blockId, productIds);
                }

                _logger.LogInformation("✅ Blok ürünleri yenilendi: Block#{BlockId}", blockId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Blok ürünleri yenilenirken hata");
                return false;
            }
        }

        #endregion

        #region Validasyon ve Yardımcı

        /// <summary>
        /// Slug benzersiz mi kontrol eder
        /// </summary>
        public async Task<bool> IsSlugAvailableAsync(string slug, int? excludeBlockId = null)
        {
            return await _repository.IsSlugUniqueAsync(slug, excludeBlockId);
        }

        /// <summary>
        /// Blok tipine göre ürünleri getirir - Preview için
        /// </summary>
        public async Task<IEnumerable<HomeBlockProductItemDto>> GetProductsByBlockTypeAsync(
            string blockType, int? categoryId, int maxCount)
        {
            var products = await GetProductsByTypeAsync(blockType, categoryId, maxCount);
            return products.Select((p, index) => MapProductToDto(p, index));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Blok için ürünleri getirir - Blok tipine göre farklı kaynak kullanır
        /// </summary>
        private async Task<List<HomeBlockProductItemDto>> GetProductsForBlockAsync(
            HomeProductBlock block, bool includeAll = false)
        {
            var maxCount = includeAll ? int.MaxValue : block.MaxProductCount;

            switch (block.BlockType.ToLower())
            {
                case "manual":
                    // Manuel seçim - HomeBlockProduct tablosundan
                    return block.BlockProducts
                        .Where(bp => bp.IsActive && bp.Product != null && bp.Product.IsActive)
                        .OrderBy(bp => bp.DisplayOrder)
                        .Take(maxCount)
                        .Select((bp, index) => MapProductToDto(bp.Product, index))
                        .ToList();

                case "category":
                    // Kategori bazlı
                    if (!block.CategoryId.HasValue) return new List<HomeBlockProductItemDto>();
                    var categoryProducts = await GetProductsByTypeAsync("category", block.CategoryId, maxCount);
                    return categoryProducts.Select((p, i) => MapProductToDto(p, i)).ToList();

                case "discounted":
                    // İndirimli ürünler
                    var discountedProducts = await GetProductsByTypeAsync("discounted", null, maxCount);
                    return discountedProducts.Select((p, i) => MapProductToDto(p, i)).ToList();

                case "newest":
                    // En yeni ürünler
                    var newestProducts = await GetProductsByTypeAsync("newest", null, maxCount);
                    return newestProducts.Select((p, i) => MapProductToDto(p, i)).ToList();

                case "bestseller":
                    // En çok satanlar - Şimdilik newest ile aynı (TODO: Order sayısına göre)
                    var bestsellerProducts = await GetProductsByTypeAsync("newest", null, maxCount);
                    return bestsellerProducts.Select((p, i) => MapProductToDto(p, i)).ToList();

                default:
                    _logger.LogWarning("⚠️ Bilinmeyen blok tipi: {BlockType}", block.BlockType);
                    return new List<HomeBlockProductItemDto>();
            }
        }

        /// <summary>
        /// Blok tipine göre ürünleri veritabanından çeker
        /// </summary>
        private async Task<List<Product>> GetProductsByTypeAsync(string blockType, int? categoryId, int maxCount)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            switch (blockType.ToLower())
            {
                case "category":
                    if (categoryId.HasValue)
                    {
                        // Alt kategorileri de dahil et
                        var categoryIds = await GetCategoryWithChildrenIdsAsync(categoryId.Value);
                        query = query.Where(p => categoryIds.Contains(p.CategoryId));
                    }
                    query = query.OrderBy(p => p.Name);
                    break;

                case "discounted":
                    // İndirimli ürünler: SpecialPrice var ve normal fiyattan düşük
                    query = query
                        .Where(p => p.SpecialPrice.HasValue && p.SpecialPrice.Value < p.Price)
                        .OrderByDescending(p => (p.Price - p.SpecialPrice!.Value) / p.Price); // En yüksek indirim önce
                    break;

                case "newest":
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;

                case "bestseller":
                    // TODO: OrderItems count'a göre sıralama eklenebilir
                    // Şimdilik en yeni ürünler
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;

                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            return await query.Take(maxCount).AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Kategori ve alt kategorilerinin ID'lerini getirir
        /// Recursive olarak tüm alt kategorileri bulur
        /// </summary>
        private async Task<List<int>> GetCategoryWithChildrenIdsAsync(int categoryId)
        {
            var result = new List<int> { categoryId };

            // Alt kategorileri bul
            var childCategories = await _context.Categories
                .Where(c => c.ParentId == categoryId && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in childCategories)
            {
                result.AddRange(await GetCategoryWithChildrenIdsAsync(childId));
            }

            return result;
        }

        /// <summary>
        /// Benzersiz slug oluşturur
        /// Aynı slug varsa sonuna numara ekler (slug-2, slug-3, ...)
        /// </summary>
        private async Task<string> GenerateUniqueSlugAsync(string name, int? excludeId = null)
        {
            var baseSlug = GenerateSlug(name);
            var slug = baseSlug;
            var counter = 2;

            while (!await _repository.IsSlugUniqueAsync(slug, excludeId))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Metinden URL dostu slug oluşturur
        /// Türkçe karakterler çevrilir, özel karakterler kaldırılır
        /// </summary>
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Türkçe karakterleri çevir
            var turkishChars = new Dictionary<char, char>
            {
                {'ğ', 'g'}, {'Ğ', 'g'},
                {'ü', 'u'}, {'Ü', 'u'},
                {'ş', 's'}, {'Ş', 's'},
                {'ı', 'i'}, {'I', 'i'},
                {'İ', 'i'},
                {'ö', 'o'}, {'Ö', 'o'},
                {'ç', 'c'}, {'Ç', 'c'}
            };

            var result = text.ToLower();
            foreach (var pair in turkishChars)
            {
                result = result.Replace(pair.Key, pair.Value);
            }

            // Özel karakterleri kaldır, boşlukları tire yap
            result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
            result = Regex.Replace(result, @"\s+", "-");
            result = Regex.Replace(result, @"-+", "-");
            result = result.Trim('-');

            return result;
        }

        /// <summary>
        /// "Tümünü Gör" URL'i oluşturur
        /// Blok tipine göre farklı URL formatı kullanılır
        /// </summary>
        private static string GenerateViewAllUrl(HomeProductBlock block)
        {
            return block.BlockType.ToLower() switch
            {
                "category" when block.CategoryId.HasValue => $"/kategori/{block.Category?.Slug ?? block.CategoryId.ToString()}",
                "discounted" => "/kampanya/indirimli-urunler",
                "newest" => "/urunler/yeni",
                "bestseller" => "/urunler/cok-satanlar",
                _ => $"/blok/{block.Slug}"
            };
        }

        #endregion

        #region Mapping Methods

        /// <summary>
        /// Entity'yi DTO'ya dönüştürür
        /// </summary>
        private static HomeProductBlockDto MapToDto(HomeProductBlock entity)
        {
            return new HomeProductBlockDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Slug = entity.Slug,
                Description = entity.Description,
                BlockType = entity.BlockType,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.Name,
                BannerId = entity.BannerId,
                PosterImageUrl = entity.PosterImageUrl ?? entity.Banner?.ImageUrl,
                BackgroundColor = entity.BackgroundColor,
                DisplayOrder = entity.DisplayOrder,
                MaxProductCount = entity.MaxProductCount,
                ViewAllUrl = entity.ViewAllUrl,
                ViewAllText = entity.ViewAllText,
                IsActive = entity.IsActive,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        /// <summary>
        /// Product entity'sini blok içi DTO'ya dönüştürür
        /// </summary>
        private static HomeBlockProductItemDto MapProductToDto(Product product, int displayOrder)
        {
            int? discountPercent = null;
            if (product.SpecialPrice.HasValue && product.Price > 0)
            {
                discountPercent = (int)Math.Round((1 - (product.SpecialPrice.Value / product.Price)) * 100);
            }

            return new HomeBlockProductItemDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                DiscountPercent = discountPercent,
                DisplayOrder = displayOrder
            };
        }

        #endregion
    }
}
