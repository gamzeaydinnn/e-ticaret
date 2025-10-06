using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Concrete
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
    }
}
