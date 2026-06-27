using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ECommerce.Data.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetMainCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId)
        {
            return await _dbSet
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
        public override async Task UpdateAsync(Category category)
        {
            _dbSet.Update(category); // EF Core Update
            await _context.SaveChangesAsync();
        }
        

    public override async Task DeleteAsync(Category category)
    {
        _dbSet.Remove(category); // EF Core remove
        await _context.SaveChangesAsync();
    }

        public override async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _dbSet
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Category>> GetAllIncludingInactiveAsync()
        {
            return await _dbSet
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        }

        public async Task<bool> ExistsSlugAsync(string slug, int? excludeId = null)
        {
            var query = _dbSet.AsQueryable();
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            return await query.AnyAsync(c => c.Slug == slug);
        }

        /// <summary>
        /// Toplu ID sorgulama - N+1 query problemini önler ve ID varlık kontrolü için kullanılır
        /// Tek seferde tüm ID'leri sorgular, sadece aktif kategorileri döner
        /// </summary>
        public async Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<Category>();
            }

            return await _dbSet
                .Where(c => ids.Contains(c.Id) && c.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Kategori yolu (breadcrumb) için üst kategorileri döndürür
        /// Verilen kategoriden başlayarak root'a kadar tüm üst kategorileri getirir
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId)
        {
            var path = new List<Category>();
            var currentCategory = await _dbSet
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            while (currentCategory != null)
            {
                path.Insert(0, currentCategory); // Başa ekle (root → child sırası)
                if (currentCategory.ParentId.HasValue)
                {
                    currentCategory = await _dbSet
                        .Include(c => c.Parent)
                        .FirstOrDefaultAsync(c => c.Id == currentCategory.ParentId.Value);
                }
                else
                {
                    break;
                }
            }

            return path;
        }

        /// <summary>
        /// Kategorinin alt kategorisi olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> HasSubCategoriesAsync(int categoryId)
        {
            return await _dbSet.AnyAsync(c => c.ParentId == categoryId);
        }

        /// <summary>
        /// Kategoriye bağlı ürün sayısını döndürür (sadece aktif ürünler)
        /// </summary>
        public async Task<int> GetProductCountAsync(int categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .CountAsync();
        }

        /// <summary>
        /// Tüm kategorileri Parent ve SubCategories ilişkileri ile birlikte döndürür (sadece aktif)
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllWithRelationsAsync()
        {
            return await _dbSet
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Tüm kategorileri Parent ve SubCategories ilişkileri ile birlikte döndürür (pasifler dahil - admin için)
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllWithRelationsIncludingInactiveAsync()
        {
            return await _dbSet
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }

    }
}
