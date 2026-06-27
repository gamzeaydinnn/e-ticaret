// Konum: /Users/dilarasara/e-ticaret/src/ECommerce.API/Controllers/CategoriesController.cs

using ECommerce.API.Data;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Category; // Oluşturduğumuz DTO'ları ekliyoruz
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/categories
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Web sitesindeki navigasyon menüsü için tüm kategorileri listeler.
        /// </summary>
        /// <returns>Kategorilerin listesi.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryListDto>), 200)]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllAsync();
            var storefrontCategories = categories
                .Where(c => c.IsActive && c.Slug != CategorySeeder.UncategorizedSlug);

            // Entity'leri CategoryListDto'ya dönüştürüyoruz (map'liyoruz).
            var categoryDtos = storefrontCategories.Select(c => new CategoryListDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                ParentId = c.ParentId,
                IsActive = c.IsActive
            });

            return Ok(categoryDtos);
        }

        /// <summary>
        /// Slug (kısa isimlendirme) ile tek bir kategorinin detaylarını ve alt kategorilerini getirir.
        /// Örn: /api/categories/meyve-sebze
        /// </summary>
        /// <param name="slug">Kategorinin URL'de kullanılacak adı.</param>
        /// <returns>Kategori detayları.</returns>
        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(CategoryDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCategoryBySlug(string slug)
        {
            var category = await _categoryService.GetBySlugAsync(slug);

            if (category == null)
            {
                return NotFound($"Slug '{slug}' ile eşleşen bir kategori bulunamadı.");
            }

            // Entity'yi CategoryDetailDto'ya dönüştürüyoruz.
            // Alt kategorileri de CategoryListDto olarak map'liyoruz.
            var categoryDetailDto = new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                ImageUrl = category.ImageUrl,
                SubCategories = category.SubCategories.Select(sc => new CategoryListDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Slug = sc.Slug,
                    ImageUrl = sc.ImageUrl,
                    ParentId = sc.ParentId
                }).ToList()
            };

            return Ok(categoryDetailDto);
        }

        // ✨ YENİ: GET /api/categories/tree - Hiyerarşik kategori ağacı (sadece aktif)
        [HttpGet("tree")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCategoryTree()
        {
            var tree = await _categoryService.GetCategoryTreeAsync();
            // Sadece aktif kategorileri filtrele
            var activeTree = tree.Where(c => c.IsActive).ToList();
            return Ok(activeTree);
        }

        // ✨ YENİ: GET /api/categories/root - Ana kategoriler (sadece aktif)
        [HttpGet("root")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRootCategories()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(categories);
        }

        // ✨ YENİ: GET /api/categories/{id}/subcategories - Belirli kategorinin alt kategorileri
        [HttpGet("{id}/subcategories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryListDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSubCategories(int id)
        {
            var subCategories = await _categoryService.GetSubCategoriesAsync(id);
            
            var subCategoryDtos = subCategories.Select(c => new CategoryListDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                ParentId = c.ParentId,
                IsActive = c.IsActive
            });

            return Ok(subCategoryDtos);
        }
    }
}
