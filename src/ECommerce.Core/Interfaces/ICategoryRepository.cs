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

        /// <summary>
        /// Toplu ID sorgulama - N+1 query problemini önler ve ID varlık kontrolü için kullanılır
        /// </summary>
        Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids);

        // Eksik metodlar eklendi
        new Task UpdateAsync(Category category);
        new Task DeleteAsync(Category category);

        /// <summary>
        /// Kategori yolu (breadcrumb) için üst kategorileri döndürür
        /// </summary>
        Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId);

        /// <summary>
        /// Kategorinin alt kategorisi olup olmadığını kontrol eder
        /// </summary>
        Task<bool> HasSubCategoriesAsync(int categoryId);

        /// <summary>
        /// Kategoriye bağlı ürün sayısını döndürür
        /// </summary>
        Task<int> GetProductCountAsync(int categoryId);

        /// <summary>
        /// Tüm kategorileri Parent ve SubCategories ilişkileri ile birlikte döndürür (sadece aktif)
        /// </summary>
        Task<IEnumerable<Category>> GetAllWithRelationsAsync();

        /// <summary>
        /// Tüm kategorileri Parent ve SubCategories ilişkileri ile birlikte döndürür (pasifler dahil - admin için)
        /// </summary>
        Task<IEnumerable<Category>> GetAllWithRelationsIncludingInactiveAsync();
    }
}
