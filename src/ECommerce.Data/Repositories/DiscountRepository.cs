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
    public class DiscountRepository : BaseRepository<Discount>, IDiscountRepository
    {
        public DiscountRepository(ECommerceDbContext context) : base(context) { }

        // ✅ Aktif ve geçerli tarih aralığındaki indirimleri getirir
        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();
        }

        // ✅ Belirli bir ürüne ait aktif indirimleri getirir
        public async Task<IEnumerable<Discount>> GetByProductIdAsync(int productId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(d => d.Products)
                .Where(d =>
                    d.IsActive &&
                    d.StartDate <= now &&
                    d.EndDate >= now &&
                    d.Products.Any(p => p.Id == productId))
                .ToListAsync();
        }
    }
}
