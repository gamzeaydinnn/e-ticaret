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

    }
}
