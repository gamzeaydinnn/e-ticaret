using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IDiscountService
using ECommerce.Entities.Concrete;            // Discount
using ECommerce.Core.Interfaces;              // IDiscountRepository


namespace ECommerce.Business.Services.Managers
{
    public class DiscountManager : IDiscountService
    {
        private readonly IDiscountRepository _discountRepository;

        public DiscountManager(IDiscountRepository discountRepository)
        {
            _discountRepository = discountRepository;
        }

        public async Task<IEnumerable<Discount>> GetAllAsync() => await _discountRepository.GetAllAsync();
        public async Task<Discount?> GetByIdAsync(int id) => await _discountRepository.GetByIdAsync(id);
        public async Task AddAsync(Discount discount) => await _discountRepository.AddAsync(discount);
        public async Task UpdateAsync(Discount discount) => await _discountRepository.UpdateAsync(discount);

        public async Task DeleteAsync(int id)
        {
            var existing = await _discountRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _discountRepository.DeleteAsync(existing);
        }

        // 👇 Yeni metotlar

        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            var allDiscounts = await _discountRepository.GetAllAsync();
            var now = DateTime.UtcNow;

            return allDiscounts
                .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now)
                .OrderByDescending(d => d.StartDate)
                .ToList();
        }

        public async Task<IEnumerable<Discount>> GetByProductIdAsync(int productId)
        {
            var allDiscounts = await _discountRepository.GetAllAsync();
            var now = DateTime.UtcNow;

            return allDiscounts
                .Where(d =>
                    d.IsActive &&
                    d.StartDate <= now &&
                    d.EndDate >= now &&
                    d.Products.Any(p => p.Id == productId))
                .ToList();
        }
    }
}
