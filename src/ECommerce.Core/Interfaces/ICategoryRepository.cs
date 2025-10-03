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

        // Eksik metodlar eklendi
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
    }
}
