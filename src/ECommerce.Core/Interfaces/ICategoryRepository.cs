using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetMainCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
    }
}