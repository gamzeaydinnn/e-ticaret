using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Category
using ECommerce.Core.DTOs.Category; // CategoryTreeDto

namespace ECommerce.Business.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<IEnumerable<Category>> GetAllAdminAsync();
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        Task<Category?> GetBySlugAsync(string slug);
        Task<int> GetCategoryCountAsync();
        
        // ✨ YENİ: Hiyerarşik kategori ağacı
        Task<IEnumerable<CategoryTreeDto>> GetCategoryTreeAsync();
        
        // ✨ YENİ: Sadece ana kategoriler (üst kategorisi olmayanlar)
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        
        // ✨ YENİ: Belirli kategorinin alt kategorileri
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
        
        // ✨ YENİ: Kategori yolu (breadcrumb için)
        Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId);
        
        // ✨ YENİ: Circular reference kontrolü
        Task<bool> WouldCreateCircularReferenceAsync(int categoryId, int? newParentId);
        
        // ✨ YENİ: Alt kategori sayısı kontrolü
        Task<bool> HasSubCategoriesAsync(int categoryId);

        // ✨ YENİ: Kategoriye bağlı ürün sayısı
        Task<int> GetProductCountAsync(int categoryId);
    }
}
