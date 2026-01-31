using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu Repository Implementasyonu
    /// ------------------------------------------------
    /// HomeProductBlock ve HomeBlockProduct tabloları için CRUD işlemleri.
    /// 
    /// Performans Optimizasyonları:
    /// - Eager loading ile tek sorguda ilişkili veriler çekilir
    /// - AsNoTracking read-only sorgularda kullanılır
    /// - Toplu işlemler için batch operasyonları
    /// </summary>
    public class HomeBlockRepository : IHomeBlockRepository
    {
        private readonly ECommerceDbContext _context;

        public HomeBlockRepository(ECommerceDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region HomeProductBlock CRUD

        /// <summary>
        /// Tüm blokları getirir - Admin listesi için
        /// Banner ve Category navigation'ları dahil edilir
        /// </summary>
        public async Task<IEnumerable<HomeProductBlock>> GetAllAsync()
        {
            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .Include(b => b.BlockProducts)
                .OrderBy(b => b.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Aktif blokları getirir - Tarih kontrolü yapılır
        /// StartDate/EndDate aralığında olanlar döner
        /// </summary>
        public async Task<IEnumerable<HomeProductBlock>> GetActiveBlocksAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .Where(b => b.IsActive)
                .Where(b => !b.StartDate.HasValue || b.StartDate.Value <= now)
                .Where(b => !b.EndDate.HasValue || b.EndDate.Value >= now)
                .OrderBy(b => b.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Aktif blokları ürünleriyle birlikte getirir - Ana sayfa için ana metod
        /// Manuel bloklar: HomeBlockProduct tablosundan ürünler yüklenir
        /// Diğer tipler (category, discounted, newest): Service katmanında filtrelenir
        /// </summary>
        public async Task<IEnumerable<HomeProductBlock>> GetActiveBlocksWithProductsAsync()
        {
            var now = DateTime.UtcNow;

            // Manuel bloklar için BlockProducts ve Product navigation'ları dahil edilir
            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .Include(b => b.BlockProducts)
                    .ThenInclude(bp => bp.Product)
                        .ThenInclude(p => p.Category) // Ürünün kategorisi de lazım
                .Where(b => b.IsActive)
                .Where(b => !b.StartDate.HasValue || b.StartDate.Value <= now)
                .Where(b => !b.EndDate.HasValue || b.EndDate.Value >= now)
                .OrderBy(b => b.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// ID'ye göre tek blok getirir
        /// </summary>
        public async Task<HomeProductBlock?> GetByIdAsync(int id)
        {
            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <summary>
        /// ID'ye göre blok ve ürünlerini getirir
        /// </summary>
        public async Task<HomeProductBlock?> GetByIdWithProductsAsync(int id)
        {
            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .Include(b => b.BlockProducts)
                    .ThenInclude(bp => bp.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <summary>
        /// Slug'a göre blok getirir - "Tümünü Gör" sayfası için
        /// </summary>
        public async Task<HomeProductBlock?> GetBySlugAsync(string slug)
        {
            return await _context.HomeProductBlocks
                .Include(b => b.Banner)
                .Include(b => b.Category)
                .Include(b => b.BlockProducts)
                    .ThenInclude(bp => bp.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        }

        /// <summary>
        /// Yeni blok ekler
        /// </summary>
        public async Task<HomeProductBlock> AddAsync(HomeProductBlock block)
        {
            block.CreatedAt = DateTime.UtcNow;
            
            await _context.HomeProductBlocks.AddAsync(block);
            await _context.SaveChangesAsync();
            
            return block;
        }

        /// <summary>
        /// Mevcut bloğu günceller
        /// </summary>
        public async Task UpdateAsync(HomeProductBlock block)
        {
            block.UpdatedAt = DateTime.UtcNow;
            
            _context.HomeProductBlocks.Update(block);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Bloğu siler - Hard delete, cascade ile ilişkiler de silinir
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var block = await _context.HomeProductBlocks
                .Include(b => b.BlockProducts)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (block != null)
            {
                // Önce ilişkili ürünleri sil
                if (block.BlockProducts.Any())
                {
                    _context.HomeBlockProducts.RemoveRange(block.BlockProducts);
                }
                
                // Sonra bloğu sil
                _context.HomeProductBlocks.Remove(block);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region HomeBlockProduct (Blok-Ürün İlişkisi)

        /// <summary>
        /// Bloğa ürün ekler - Duplicate kontrolü yapar
        /// </summary>
        public async Task AddProductToBlockAsync(int blockId, int productId, int displayOrder = 0)
        {
            // Duplicate kontrolü
            var exists = await _context.HomeBlockProducts
                .AnyAsync(bp => bp.BlockId == blockId && bp.ProductId == productId);

            if (!exists)
            {
                var blockProduct = new HomeBlockProduct
                {
                    BlockId = blockId,
                    ProductId = productId,
                    DisplayOrder = displayOrder,
                    IsActive = true,
                    AddedAt = DateTime.UtcNow
                };

                await _context.HomeBlockProducts.AddAsync(blockProduct);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Bloğa birden fazla ürün ekler - Toplu işlem
        /// </summary>
        public async Task AddProductsToBlockAsync(int blockId, IEnumerable<(int productId, int displayOrder)> products)
        {
            // Mevcut ürünleri al (duplicate önlemek için)
            var existingProductIds = await _context.HomeBlockProducts
                .Where(bp => bp.BlockId == blockId)
                .Select(bp => bp.ProductId)
                .ToListAsync();

            var newProducts = products
                .Where(p => !existingProductIds.Contains(p.productId))
                .Select(p => new HomeBlockProduct
                {
                    BlockId = blockId,
                    ProductId = p.productId,
                    DisplayOrder = p.displayOrder,
                    IsActive = true,
                    AddedAt = DateTime.UtcNow
                })
                .ToList();

            if (newProducts.Any())
            {
                await _context.HomeBlockProducts.AddRangeAsync(newProducts);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Bloktan ürün çıkarır
        /// </summary>
        public async Task RemoveProductFromBlockAsync(int blockId, int productId)
        {
            var blockProduct = await _context.HomeBlockProducts
                .FirstOrDefaultAsync(bp => bp.BlockId == blockId && bp.ProductId == productId);

            if (blockProduct != null)
            {
                _context.HomeBlockProducts.Remove(blockProduct);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Bloktaki tüm ürünleri çıkarır
        /// </summary>
        public async Task ClearBlockProductsAsync(int blockId)
        {
            var blockProducts = await _context.HomeBlockProducts
                .Where(bp => bp.BlockId == blockId)
                .ToListAsync();

            if (blockProducts.Any())
            {
                _context.HomeBlockProducts.RemoveRange(blockProducts);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Bloktaki ürün sıralamasını günceller
        /// </summary>
        public async Task UpdateProductOrderAsync(int blockId, int productId, int newDisplayOrder)
        {
            var blockProduct = await _context.HomeBlockProducts
                .FirstOrDefaultAsync(bp => bp.BlockId == blockId && bp.ProductId == productId);

            if (blockProduct != null)
            {
                blockProduct.DisplayOrder = newDisplayOrder;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Bloktaki ürünlerin sıralamasını toplu günceller
        /// </summary>
        public async Task UpdateBlockProductsOrderAsync(int blockId, IEnumerable<(int productId, int displayOrder, bool isActive)> products)
        {
            var blockProducts = await _context.HomeBlockProducts
                .Where(bp => bp.BlockId == blockId)
                .ToListAsync();

            foreach (var product in products)
            {
                var bp = blockProducts.FirstOrDefault(x => x.ProductId == product.productId);
                if (bp != null)
                {
                    bp.DisplayOrder = product.displayOrder;
                    bp.IsActive = product.isActive;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Bloktaki ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetBlockProductCountAsync(int blockId)
        {
            return await _context.HomeBlockProducts
                .CountAsync(bp => bp.BlockId == blockId && bp.IsActive);
        }

        /// <summary>
        /// Ürünün hangi bloklarda olduğunu getirir
        /// </summary>
        public async Task<IEnumerable<HomeProductBlock>> GetBlocksByProductIdAsync(int productId)
        {
            var blockIds = await _context.HomeBlockProducts
                .Where(bp => bp.ProductId == productId)
                .Select(bp => bp.BlockId)
                .ToListAsync();

            return await _context.HomeProductBlocks
                .Where(b => blockIds.Contains(b.Id))
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Slug benzersiz mi kontrol eder
        /// </summary>
        public async Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null)
        {
            var query = _context.HomeProductBlocks.Where(b => b.Slug == slug);
            
            if (excludeId.HasValue)
            {
                query = query.Where(b => b.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Blok sıralamasını yeniden düzenler - Gap'leri kapatır
        /// Örnek: 0, 5, 10 → 0, 1, 2
        /// </summary>
        public async Task ReorderBlocksAsync()
        {
            var blocks = await _context.HomeProductBlocks
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].DisplayOrder = i;
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
