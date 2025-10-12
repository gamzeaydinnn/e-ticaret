using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Category

namespace ECommerce.Business.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        Task<Category?> GetBySlugAsync(string slug);
        Task<int> GetCategoryCountAsync();
    }
}
