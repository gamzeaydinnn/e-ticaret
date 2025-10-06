using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface ICouponRepository : IRepository<Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);
    }
}
