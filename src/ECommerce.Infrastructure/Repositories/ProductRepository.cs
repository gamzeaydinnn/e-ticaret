using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(ECommerceDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Tüm aktif ürünleri getirir.
        /// </summary>
        public override async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _dbSet
                .Include(p => p.Categories)  // navigation property'i dahil et
                .Where(p => p.IsActive)      // sadece aktif ürünler
                .ToListAsync();
        }

        /// <summary>
        /// Id ile ürün getirir, sadece aktif ürünler.
        /// </summary>
        public override async Task<Product?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        /// <summary>
        /// Belirli kategoriye ait aktif ürünleri getirir.
        /// </summary>
        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _dbSet
                .Include(p => p.Categories)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Ürün ismi, açıklaması veya markasına göre arama yapar.
        /// </summary>
        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            return await _dbSet
                .Include(p => p.Categories)
                .Where(p => p.IsActive &&
                            (p.Name.Contains(searchTerm) ||
                             p.Description.Contains(searchTerm) ||
                             (!string.IsNullOrEmpty(p.Brand) && p.Brand.Contains(searchTerm))))
                .ToListAsync();
        }

        public Product GetById(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Product product)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}
