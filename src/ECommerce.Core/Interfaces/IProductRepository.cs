using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    }
}

