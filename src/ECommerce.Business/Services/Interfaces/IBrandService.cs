using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete; 


namespace ECommerce.Business.Services.Interfaces
{
    public interface IBrandService
    {
        Task<IEnumerable<Brand>> GetAllAsync();
        Task<Brand?> GetByIdAsync(int id);
        Task AddAsync(Brand brand);
        Task UpdateAsync(Brand brand);
        Task DeleteAsync(int id);
    }
}
