using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ECommerce.Data.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(ECommerceDbContext context) : base(context)
        {
            
        }
        public async Task UpdateAsync(Product product)
    {
        _dbSet.Update(product);
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(Product product)
    {
        _dbSet.Remove(product);
        await _context.SaveChangesAsync();
    }
        public async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }
        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

            /*public async Task<Product> UpdateAsync(Product product)
            {
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                return product;
            }

            public async Task DeleteAsync(Product product)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }*/

        public void Update(Product product)
        {
            _dbSet.Update(product);
            _context.SaveChanges();
        }
        public Product GetById(int id)
        {
            return _dbSet.Include(p => p.Categories)
                .FirstOrDefault(p => p.Id == id && p.IsActive)!;
        }

        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _dbSet.Include(p => p.Categories)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToListAsync();
        }
        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            return await _dbSet.Include(p => p.Categories)
                .Where(p => p.IsActive &&
                (p.Name.Contains(searchTerm) ||
                    p.Description.Contains(searchTerm) ||
                (p.Brand != null && p.Brand.Contains(searchTerm))))
                    .ToListAsync();
        }
        public IEnumerable<Product> GetAll()
        {
            throw new NotImplementedException();
        }

        Task IProductRepository.Delete(Product product)
        {
            throw new NotImplementedException();
        }

        public Task<Product> GetBySkuAsync(string sku)
        {
            throw new NotImplementedException();
        }
    }
}
