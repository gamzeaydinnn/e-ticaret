using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        // 🔹 Ürün sorgulama
        Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);
        Task<Product> GetBySkuAsync(string sku);

        // 🔹 CRUD işlemleri (senkron + asenkron)
        Task<Product?> GetByIdAsync(int id);
        Product GetById(int id);

        Task<Product> AddAsync(Product product);
        Task UpdateAsync(Product product);
        void Update(Product product);
        Task DeleteAsync(Product product);
        Task Delete(Product product);

        IEnumerable<Product> GetAll();

        // 🔹 Mikro senkronizasyon loglama işlemleri
        void LogSync(MicroSyncLog log);
        Task LogSyncAsync(MicroSyncLog log);
    }
}
