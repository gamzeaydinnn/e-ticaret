using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IBrandService
using ECommerce.Entities.Concrete;            // Brand
using ECommerce.Core.Interfaces;              // IBrandRepository


namespace ECommerce.Business.Services.Managers
{
    public class BrandManager : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandManager(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        public async Task<IEnumerable<Brand>> GetAllAsync() => await _brandRepository.GetAllAsync();

        public async Task<Brand?> GetByIdAsync(int id) => await _brandRepository.GetByIdAsync(id);

        public async Task AddAsync(Brand brand) => await _brandRepository.AddAsync(brand);

        public async Task UpdateAsync(Brand brand) => await _brandRepository.UpdateAsync(brand);

        public async Task DeleteAsync(int id)
        {
            var existing = await _brandRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _brandRepository.DeleteAsync(existing);
        }
    }
}
