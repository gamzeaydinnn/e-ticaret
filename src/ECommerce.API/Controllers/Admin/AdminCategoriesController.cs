using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Category;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.API.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = Roles.AllStaff)]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IAdminCatalogStatsService _adminCatalogStatsService;

        public AdminCategoriesController(
            ICategoryService categoryService,
            IAdminCatalogStatsService adminCatalogStatsService)
        {
            _categoryService = categoryService;
            _adminCatalogStatsService = adminCatalogStatsService;
        }

        // GET /api/admin/categories
        [HttpGet]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetAllAdminAsync();
            var productCounts = await _adminCatalogStatsService.GetActiveProductCountsByCategoryAsync(
                HttpContext.RequestAborted);
            var result = new List<object>();
            foreach (var c in categories)
            {
                productCounts.TryGetValue(c.Id, out var productCount);
                result.Add(new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Description,
                    c.ImageUrl,
                    c.ParentId,
                    c.SortOrder,
                    c.IsActive,
                    productCount
                });
            }
            return Ok(result);
        }

        // GET /api/admin/categories/{id}
        [HttpGet("{id}")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        // POST /api/admin/categories
        [HttpPost]
        [HasPermission(Permissions.Categories.Create)]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            try
            {
                await _categoryService.AddAsync(category);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT /api/admin/categories/{id}
        [HttpPut("{id}")]
        [HasPermission(Permissions.Categories.Update)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            var existing = await _categoryService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.ImageUrl = category.ImageUrl;
            existing.ParentId = category.ParentId;
            existing.SortOrder = category.SortOrder;
            existing.Slug = category.Slug;
            existing.IsActive = category.IsActive;

            try
            {
                await _categoryService.UpdateAsync(existing);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE /api/admin/categories/{id}
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Categories.Delete)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();

            try
            {
                // Hard delete: Kategoriyı tamamen sil
                await _categoryService.DeleteAsync(category);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✨ YENİ: GET /api/admin/categories/tree - Hiyerarşik kategori ağacı
        [HttpGet("tree")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategoryTree()
        {
            var tree = await _categoryService.GetCategoryTreeAsync();
            var productCounts = await _adminCatalogStatsService.GetActiveProductCountsByCategoryAsync(
                HttpContext.RequestAborted);
            ApplyCatalogProductCounts(tree, productCounts);
            return Ok(tree);
        }

        private static void ApplyCatalogProductCounts(
            IEnumerable<CategoryTreeDto> nodes,
            IReadOnlyDictionary<int, int> productCounts)
        {
            foreach (var node in nodes)
            {
                node.ProductCount = productCounts.TryGetValue(node.Id, out var count) ? count : 0;
                if (node.Children?.Count > 0)
                {
                    ApplyCatalogProductCounts(node.Children, productCounts);
                }
            }
        }

        // ✨ YENİ: GET /api/admin/categories/root - Ana kategoriler
        [HttpGet("root")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetRootCategories()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(categories);
        }

        // ✨ YENİ: GET /api/admin/categories/{id}/subcategories - Alt kategoriler
        [HttpGet("{id}/subcategories")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetSubCategories(int id)
        {
            var subCategories = await _categoryService.GetSubCategoriesAsync(id);
            return Ok(subCategories);
        }

        // ✨ YENİ: GET /api/admin/categories/{id}/path - Kategori yolu (breadcrumb)
        [HttpGet("{id}/path")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategoryPath(int id)
        {
            var path = await _categoryService.GetCategoryPathAsync(id);
            return Ok(path);
        }
    }
}
