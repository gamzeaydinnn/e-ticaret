using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // ICategoryService
using ECommerce.Entities.Concrete;            // Category
using ECommerce.Core.Interfaces;              // ICategoryRepository
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace ECommerce.Business.Services.Managers
{
    public class CategoryManager : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryManager(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task AddAsync(Category category)
        {
            NormalizeAndValidate(category);
            await _categoryRepository.AddAsync(category);
        }

        public async Task DeleteAsync(Category category)
        {
            await _categoryRepository.DeleteAsync(category);
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }
        public async Task<IEnumerable<Category>> GetAllAdminAsync()
        {
            return await _categoryRepository.GetAllIncludingInactiveAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _categoryRepository.GetBySlugAsync(slug);
        }

        public async Task<int> GetCategoryCountAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Count();
        }

        public async Task UpdateAsync(Category category)
        {
            NormalizeAndValidate(category, category.Id);
            await _categoryRepository.UpdateAsync(category);
        }

        private void NormalizeAndValidate(Category category, int? excludeId = null)
        {
            // Slug: name'den üret veya verilen değeri normalize et
            var slug = string.IsNullOrWhiteSpace(category.Slug)
                ? category.Name
                : category.Slug;
            slug = Slugify(slug);
            category.Slug = slug;

            // Benzersizlik kontrolü
            var exists = _categoryRepository.ExistsSlugAsync(slug, excludeId).GetAwaiter().GetResult();
            if (exists)
            {
                throw new InvalidOperationException($"Slug '{slug}' zaten kullanılıyor.");
            }
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string lower = input.Trim().ToLowerInvariant();
            // Türkçe karakter dönüşümü ve genel normalize
            lower = lower
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i").Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Replace("Ç", "c").Replace("Ğ", "g").Replace("İ", "i").Replace("Ö", "o").Replace("Ş", "s").Replace("Ü", "u");
            lower = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
            lower = Regex.Replace(lower, @"\s+", "-");
            lower = Regex.Replace(lower, "-+", "-");
            return lower.Trim('-');
        }
    }
}
