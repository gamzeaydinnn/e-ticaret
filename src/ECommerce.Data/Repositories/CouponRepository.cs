using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class CouponRepository : BaseRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(ECommerceDbContext context) : base(context) { }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
        }
    }
}
