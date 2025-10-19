// BrandManager.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Brand; // DTO'lar için eklendi
using System.Linq; // LINQ için eklendi

namespace ECommerce.Business.Services.Managers
{
    public class BrandManager : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandManager(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }
        
        // --- Entity Dönüşü Olan Metotlar (Mevcut metotların isimleri güncellendi) ---
        
        public async Task<IEnumerable<Brand>> GetAllBrandsEntityAsync() => 
            await _brandRepository.GetAllAsync();

        public async Task<Brand?> GetBrandEntityByIdAsync(int id) => 
            await _brandRepository.GetByIdAsync(id);

        public async Task AddAsync(Brand brand) => await _brandRepository.AddAsync(brand);

        public async Task UpdateAsync(Brand brand) => await _brandRepository.UpdateAsync(brand);

        public async Task DeleteAsync(int id)
        {
            var existing = await _brandRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _brandRepository.DeleteAsync(existing);
        }

        // --- Yeni Eklenen: Public API için DTO Dönüşü Olan Metotlar ---

        public async Task<IEnumerable<BrandDto>> GetAllBrandsDtoAsync()
        {
            // İlerde sadece IsActive olanları getirmek için repository'de IsActive filtresi eklenmelidir.
            var brands = await _brandRepository.GetAllAsync();
            return brands.Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                Slug = b.Slug // Slug alanını da DTO'ya ekledik
            });
        }

        public async Task<BrandDto?> GetBrandDtoByIdAsync(int id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null) return null;

            return new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                LogoUrl = brand.LogoUrl,
                Slug = brand.Slug
            };
        }

        // ✅ Slug ile Marka Getirme Metodu (IBrandRepository'e bu metodu eklememiz gerekiyor)
        public async Task<BrandDto?> GetBrandBySlugAsync(string slug)
        {
            // **IBrandRepository'de GetBySlugAsync metodunun olduğunu varsayıyoruz.**
            var brand = await _brandRepository.GetBySlugAsync(slug); 

            if (brand == null) return null;

            return new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                LogoUrl = brand.LogoUrl,
                Slug = brand.Slug
            };
        }

        public async Task<IEnumerable<Brand>> GetAllAsync()
        {
            return await _brandRepository.GetAllAsync();
        }

        public async Task<Brand?> GetByIdAsync(int id)
        {
            return await _brandRepository.GetByIdAsync(id);
        }
    }
}
