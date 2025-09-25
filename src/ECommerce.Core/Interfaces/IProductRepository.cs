using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);

        // Sync ve Async için eklemeler
        Task<Product?> GetByIdAsync(int id); 
        Product GetById(int id);
        Task UpdateAsync(Product product);
        void Update(Product product); 
        

        IEnumerable<Product> GetAll();
        
    }
}
