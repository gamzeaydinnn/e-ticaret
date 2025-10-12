using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // ICouponService
using ECommerce.Entities.Concrete;            // Coupon
using ECommerce.Core.Interfaces;              // ICouponRepository


namespace ECommerce.Business.Services.Managers
{
    public class CouponManager : ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponManager(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        public async Task<IEnumerable<Coupon>> GetAllAsync() => await _couponRepository.GetAllAsync();
        public async Task<Coupon?> GetByIdAsync(int id) => await _couponRepository.GetByIdAsync(id);
        public async Task AddAsync(Coupon coupon) => await _couponRepository.AddAsync(coupon);
        public async Task UpdateAsync(Coupon coupon) => await _couponRepository.UpdateAsync(coupon);
        public async Task DeleteAsync(int id)
        {
            var existing = await _couponRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _couponRepository.DeleteAsync(existing);
        }

        public async Task<bool> ValidateCouponAsync(string code)
        {
            var coupon = await _couponRepository.GetByCodeAsync(code);
            // DİKKAT: entity'de tarih alanının adı "ExpirationDate" olarak kullanıldı
            return coupon != null && coupon.IsActive && coupon.ExpirationDate > DateTime.UtcNow;
        }
    }
}
