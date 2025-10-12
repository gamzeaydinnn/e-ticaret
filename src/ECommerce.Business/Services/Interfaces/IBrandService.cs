using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Brand;
using ECommerce.Entities.Concrete; 


namespace ECommerce.Business.Services.Interfaces
{
    public interface IBrandService
    {
        // Markaları Entity olarak getiren (Admin için veya dahili kullanım)
        Task<IEnumerable<Brand>> GetAllBrandsEntityAsync(); // İsim değiştirildi: entity döndürdüğünü belirtmek için
        Task<Brand?> GetBrandEntityByIdAsync(int id); // İsim değiştirildi: entity döndürdüğünü belirtmek için
// ✅ Public API için DTO döndüren metotlar
        Task<IEnumerable<BrandDto>> GetAllBrandsDtoAsync();
        Task<BrandDto?> GetBrandDtoByIdAsync(int id);
        Task<BrandDto?> GetBrandBySlugAsync(string slug);
        
        Task<IEnumerable<Brand>> GetAllAsync();
        Task<Brand?> GetByIdAsync(int id);
        Task AddAsync(Brand brand);
        Task UpdateAsync(Brand brand);
        Task DeleteAsync(int id);
    }
}
