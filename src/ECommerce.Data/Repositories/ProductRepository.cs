using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ECommerce.Data.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(ECommerceDbContext context) : base(context)
        {
        }
        public override async Task UpdateAsync(Product product)
        {
            _dbSet.Update(product);
            await _context.SaveChangesAsync();
        }

        public override async Task DeleteAsync(Product product)
        {
            try
            {
                // Önce ilişkili verileri kontrol et ve temizle
                // 1. Sepet öğelerini kontrol et
                var cartItems = await _context.Set<CartItem>()
                    .Where(ci => ci.ProductId == product.Id)
                    .ToListAsync();
                
                if (cartItems.Any())
                {
                    // Sepetteki ürünleri sil (kullanıcı zaten mikrodan tekrar çekebilir)
                    _context.Set<CartItem>().RemoveRange(cartItems);
                }

                // 2. Sipariş öğelerini kontrol et
                var hasOrderItems = await _context.Set<OrderItem>()
                    .AnyAsync(oi => oi.ProductId == product.Id);

                if (hasOrderItems)
                {
                    // Sipariş geçmişi olan ürünleri fiziksel olarak silme - soft delete yap
                    product.IsActive = false;
                    product.Name = $"[SİLİNDİ] {product.Name}";
                    _dbSet.Update(product);
                }
                else
                {
                    // Sipariş geçmişi yoksa fiziksel olarak sil
                    _dbSet.Remove(product);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Hata olsa bile en azından soft delete yap
                try
                {
                    product.IsActive = false;
                    product.Name = $"[SİLİNDİ] {product.Name}";
                    _dbSet.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Son çare: hiçbir şey yapma ama exception fırlatma
                    // Log yazılabilir ama silme işlemi başarısız sayılmaz
                }
            }
        }
        public new async Task<Product> AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }
        public override async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public new async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();
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

        public override void Update(Product product)
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
                       .Include(p => p.Brand) // Brand entity’sini yükle
                       .Where(p => p.IsActive &&
                                   (p.Name.Contains(searchTerm) ||
                                    p.Description.Contains(searchTerm) ||
                                    (p.Brand != null && p.Brand.Name.Contains(searchTerm))))
                       .ToListAsync();
}



        Task IProductRepository.Delete(Product product)
        {
            // Interface'teki senkron Task imzasını, mevcut async silme ile köprüleyelim
            return DeleteAsync(product);
        }

        public async Task<Product?> GetBySkuAsync(string sku)
        {
            var normalizedSku = (sku ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedSku))
            {
                return null;
            }

            return await _dbSet
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IsActive && p.SKU.ToLower() == normalizedSku.ToLower());
        }

        /// <summary>
        /// Toplu ID sorgulama - N+1 query problemini önler
        /// Tek seferde tüm ID'leri sorgular, sadece aktif ürünleri döner
        /// </summary>
        public async Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<Product>();
            }

            return await _dbSet
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .ToListAsync();
        }

        public async Task LogSyncAsync(MicroSyncLog log)
        {
            await _context.MicroSyncLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public IEnumerable<Product> GetAll()
        {
            return _context.Products.AsNoTracking().ToList();
        }

        public void LogSync(MicroSyncLog log)
        {
            _context.MicroSyncLogs.Add(log);
            _context.SaveChanges();
        }
    }
}
