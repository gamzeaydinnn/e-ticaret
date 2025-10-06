using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ECommerce.Business.Services.Interfaces
{
    public interface ICouponService
    {
        Task<IEnumerable<Coupon>> GetAllAsync();
        Task<Coupon?> GetByIdAsync(int id);
        Task AddAsync(Coupon coupon);
        Task UpdateAsync(Coupon coupon);
        Task DeleteAsync(int id);
        Task<bool> ValidateCouponAsync(string code);
    }
}
