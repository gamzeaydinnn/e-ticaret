using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetMainCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
        Task<Category?> GetBySlugAsync(string slug);
        Task<IEnumerable<Category>> GetAllIncludingInactiveAsync();
        Task<bool> ExistsSlugAsync(string slug, int? excludeId = null);

        // Eksik metodlar eklendi
        new Task UpdateAsync(Category category);
        new Task DeleteAsync(Category category);
    }
}
