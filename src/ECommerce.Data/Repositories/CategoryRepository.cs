using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

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

        public override async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _dbSet
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
    }
}