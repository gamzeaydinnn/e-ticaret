using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
// using ECommerce.Core.Entities.Concrete; // removed: entities live in ECommerce.Entities.Concrete

namespace ECommerce.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        // 🔹 Ürün sorgulama
        Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);
        Task<Product?> GetBySkuAsync(string sku);

        // 🔹 CRUD işlemleri (senkron + asenkron)
        new Task<Product?> GetByIdAsync(int id);
        Product GetById(int id);

        new Task<Product> AddAsync(Product product);
        new Task UpdateAsync(Product product);
        void Update(Product product);
        new Task DeleteAsync(Product product);
        Task Delete(Product product);

        IEnumerable<Product> GetAll();

        // 🔹 Mikro senkronizasyon loglama işlemleri
        void LogSync(MicroSyncLog log);
        Task LogSyncAsync(MicroSyncLog log);
    }
}
