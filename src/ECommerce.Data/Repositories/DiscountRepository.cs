using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    public class DiscountRepository : BaseRepository<Discount>, IDiscountRepository
    {
        public DiscountRepository(ECommerceDbContext context) : base(context) { }

        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            return await _dbSet
                .Where(d => d.IsActive && d.EndDate > DateTime.Now)
                .OrderByDescending(d => d.EndDate)
                .ToListAsync();
        }
    }
}
