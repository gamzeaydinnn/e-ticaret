using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // ICategoryService
using ECommerce.Entities.Concrete;            // Category
using ECommerce.Core.Interfaces;              // ICategoryRepository
using ECommerce.Core.DTOs.Category;           // CategoryTreeDto
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;

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
            // 1. Alt kategori kontrolü
            var hasSubCategories = await HasSubCategoriesAsync(category.Id);
            if (hasSubCategories)
            {
                throw new InvalidOperationException(
                    "Alt kategorisi olan kategori silinemez. Önce alt kategorileri silin veya taşıyın."
                );
            }
            
            // 2. Bğ ürün kontrolü — FK_Products_Categories_CategoryId constraint
            var productCount = await _categoryRepository.GetProductCountAsync(category.Id);
            if (productCount > 0)
            {
                throw new InvalidOperationException(
                    $"{productCount} adet ürün bu kategoriye bağlı. " +
                    "Kategoriyi silmeden önce bu ürünleri başka bir kategoriye taşıyın veya silin."
                );
            }
            
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
            // Circular reference kontrolü
            if (category.ParentId.HasValue)
            {
                var wouldCreateCircular = await WouldCreateCircularReferenceAsync(
                    category.Id, 
                    category.ParentId.Value
                );
                
                if (wouldCreateCircular)
                {
                    throw new InvalidOperationException(
                        "Bu işlem döngüsel kategori ilişkisi oluşturur. Kategori kendi alt kategorisi olamaz."
                    );
                }
            }
            
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

        // ✨ YENİ METODLAR

        /// <summary>
        /// Hiyerarşik kategori ağacını döndürür
        /// Sadece aktif kategorileri içerir
        /// </summary>
        public async Task<IEnumerable<CategoryTreeDto>> GetCategoryTreeAsync()
        {
            var allCategories = await _categoryRepository.GetAllWithRelationsAsync();
            var categoryList = allCategories.ToList();
            
            // Root kategorileri bul (ParentId == null)
            var rootCategories = categoryList.Where(c => c.ParentId == null).ToList();
            
            // DTO'ya dönüştür ve ağacı oluştur
            var tree = new List<CategoryTreeDto>();
            foreach (var root in rootCategories)
            {
                var dto = await MapToCategoryTreeDto(root, categoryList);
                tree.Add(dto);
            }
            
            return tree.OrderBy(c => c.SortOrder);
        }

        /// <summary>
        /// Category entity'sini CategoryTreeDto'ya dönüştürür (recursive)
        /// </summary>
        private async Task<CategoryTreeDto> MapToCategoryTreeDto(Category category, List<Category> allCategories)
        {
            var dto = new CategoryTreeDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                ParentId = category.ParentId,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive,
                ProductCount = await _categoryRepository.GetProductCountAsync(category.Id),
                Children = new List<CategoryTreeDto>()
            };

            // Alt kategorileri bul ve recursive olarak ekle
            var children = allCategories.Where(c => c.ParentId == category.Id).OrderBy(c => c.SortOrder).ToList();
            foreach (var child in children)
            {
                var childDto = await MapToCategoryTreeDto(child, allCategories);
                dto.Children.Add(childDto);
            }

            return dto;
        }

        /// <summary>
        /// Sadece ana kategorileri döndürür (ParentId == null)
        /// </summary>
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            return await _categoryRepository.GetMainCategoriesAsync();
        }

        /// <summary>
        /// Belirli kategorinin alt kategorilerini döndürür
        /// </summary>
        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId)
        {
            return await _categoryRepository.GetSubCategoriesAsync(parentId);
        }

        /// <summary>
        /// Kategori yolunu döndürür (breadcrumb için)
        /// Ana kategoriden başlayarak seçili kategoriye kadar tüm üst kategoriler
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId)
        {
            return await _categoryRepository.GetCategoryPathAsync(categoryId);
        }

        /// <summary>
        /// Circular reference kontrolü yapar
        /// Kategori kendi alt kategorisi olamaz
        /// </summary>
        public async Task<bool> WouldCreateCircularReferenceAsync(int categoryId, int? newParentId)
        {
            if (!newParentId.HasValue)
            {
                return false; // Root kategori olacak, sorun yok
            }

            if (categoryId == newParentId.Value)
            {
                return true; // Kendini parent yapamaz
            }

            // newParentId'nin tüm üst kategorilerini kontrol et
            // Eğer categoryId bu zincirde varsa, circular reference oluşur
            var path = await _categoryRepository.GetCategoryPathAsync(newParentId.Value);
            return path.Any(c => c.Id == categoryId);
        }

        /// <summary>
        /// Kategorinin alt kategorisi olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> HasSubCategoriesAsync(int categoryId)
        {
            return await _categoryRepository.HasSubCategoriesAsync(categoryId);
        }

        /// <summary>
        /// Kategoriye bağlı ürün sayısını döndürür
        /// </summary>
        public async Task<int> GetProductCountAsync(int categoryId)
        {
            return await _categoryRepository.GetProductCountAsync(categoryId);
        }
    }
}
