using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Category;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.API.Authorization;
using ECommerce.API.Data;
using ECommerce.Data.Context;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = Roles.AllStaff)]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IAdminCatalogStatsService _adminCatalogStatsService;
        private readonly IFileStorage _fileStorage;
        private readonly IMikroGrupDbService _mikroGrupDbService;
        private readonly ECommerceDbContext _dbContext;
        private readonly ILogger<AdminCategoriesController> _logger;

        private static readonly CultureInfo TrCulture = new("tr-TR");
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedMimeTypes =
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        private const long MaxFileSize = 10 * 1024 * 1024;

        public AdminCategoriesController(
            ICategoryService categoryService,
            IAdminCatalogStatsService adminCatalogStatsService,
            IFileStorage fileStorage,
            IMikroGrupDbService mikroGrupDbService,
            ECommerceDbContext dbContext,
            ILogger<AdminCategoriesController> logger)
        {
            _categoryService = categoryService;
            _adminCatalogStatsService = adminCatalogStatsService;
            _fileStorage = fileStorage;
            _mikroGrupDbService = mikroGrupDbService;
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET /api/admin/categories
        [HttpGet]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = (await _categoryService.GetAllAdminAsync()).ToList();

            // Ürün sayımı opsiyonel — asla listeyi bloke etmesin (VPN/DB gecikmesi)
            IReadOnlyDictionary<int, int> productCounts = new Dictionary<int, int>();
            try
            {
                using var countCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                countCts.CancelAfter(TimeSpan.FromSeconds(4));
                productCounts = await _adminCatalogStatsService.GetLocalActiveProductCountsByCategoryAsync(
                    countCts.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
            {
                _logger.LogWarning("Admin kategori ürün sayımı atlandı (timeout/iptal).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin kategori ürün sayımı başarısız; liste sayısız dönüyor.");
            }

            var childCategoriesByParent = categories
                .Where(c => c.ParentId.HasValue)
                .GroupBy(c => c.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<object>();
            foreach (var c in categories)
            {
                productCounts.TryGetValue(c.Id, out var directCount);
                var totalProductCount = directCount;

                if (c.ParentId == null && childCategoriesByParent.TryGetValue(c.Id, out var subCats))
                {
                    foreach (var sc in subCats)
                    {
                        if (productCounts.TryGetValue(sc.Id, out var scCount))
                        {
                            totalProductCount += scCount;
                        }
                    }
                }

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
                    c.MikroAnaGrupKod,
                    c.MikroAltGrupKod,
                    c.MikroIsim,
                    c.NavbarGroupKey,
                    c.IsMikroSynced,
                    c.LastMikroSyncAt,
                    productCount = totalProductCount,
                    directProductCount = directCount
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
        // YALNIZCA admin yeni bir ana kategori veya alt kategori eklediğinde Mikro ERP'ye INSERT yapılır.
        [HttpPost]
        [HasPermission(Permissions.Categories.Create)]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest(new { message = "Kategori adı zorunludur." });
            }

            try
            {
                if (category.ParentId == null)
                {
                    // Yeni Ana Kategori — Mikro STOK_ANA_GRUPLARI'na INSERT
                    if (_mikroGrupDbService.IsConfigured && string.IsNullOrWhiteSpace(category.MikroAnaGrupKod))
                    {
                        var (mikroOk, yeniAnaKod, mikroErr) = await _mikroGrupDbService.CreateAnaGrupInMikroAsync(
                            category.Name,
                            HttpContext.RequestAborted);

                        if (!mikroOk || string.IsNullOrWhiteSpace(yeniAnaKod))
                        {
                            return StatusCode(502, new { message = mikroErr ?? "Ana kategori Mikro ERP'ye kaydedilemedi." });
                        }

                        category.MikroAnaGrupKod = yeniAnaKod;
                        category.MikroAltGrupKod = null;
                        category.MikroIsim = category.Name.Trim().ToUpper(TrCulture);
                        category.IsMikroSynced = true;
                        category.LastMikroSyncAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Yeni Alt Kategori — Üst kategorinin MikroAnaGrupKod'u altında STOK_ALT_GRUPLARI'na INSERT
                    var parent = await _categoryService.GetByIdAsync(category.ParentId.Value);
                    if (parent == null)
                    {
                        return BadRequest(new { message = "Seçilen üst kategori bulunamadı." });
                    }

                    category.MikroAnaGrupKod = parent.MikroAnaGrupKod;

                    if (_mikroGrupDbService.IsConfigured &&
                        !string.IsNullOrWhiteSpace(parent.MikroAnaGrupKod) &&
                        string.IsNullOrWhiteSpace(category.MikroAltGrupKod))
                    {
                        var (mikroOk, yeniAltKod, mikroErr) = await _mikroGrupDbService.CreateAltGrupInMikroAsync(
                            parent.MikroAnaGrupKod,
                            category.Name,
                            HttpContext.RequestAborted);

                        if (!mikroOk || string.IsNullOrWhiteSpace(yeniAltKod))
                        {
                            return StatusCode(502, new { message = mikroErr ?? "Alt kategori Mikro ERP'ye kaydedilemedi." });
                        }

                        category.MikroAltGrupKod = yeniAltKod;
                        category.MikroIsim = category.Name.Trim().ToUpper(TrCulture);
                        category.IsMikroSynced = true;
                        category.LastMikroSyncAt = DateTime.UtcNow;
                    }
                }

                await _categoryService.AddAsync(category);

                // MikroCategoryMapping kaydını da oluştur ki Mikro'da bu gruba eklenecek ürünler otomatik eşleşsin
                if (!string.IsNullOrWhiteSpace(category.MikroAnaGrupKod))
                {
                    var existingMapping = await _dbContext.MikroCategoryMappings
                        .FirstOrDefaultAsync(m =>
                            m.MikroAnagrupKod == category.MikroAnaGrupKod &&
                            m.MikroAltgrupKod == category.MikroAltGrupKod &&
                            m.MikroMarkaKod == null,
                            HttpContext.RequestAborted);

                    if (existingMapping == null)
                    {
                        _dbContext.MikroCategoryMappings.Add(new MikroCategoryMapping
                        {
                            MikroAnagrupKod = category.MikroAnaGrupKod,
                            MikroAltgrupKod = category.MikroAltGrupKod,
                            MikroMarkaKod = null,
                            CategoryId = category.Id,
                            Priority = string.IsNullOrWhiteSpace(category.MikroAltGrupKod) ? 50 : 100,
                            IsActive = true,
                            Notes = $"Admin tarafından oluşturuldu: {category.Name}",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _dbContext.SaveChangesAsync(HttpContext.RequestAborted);
                    }
                }

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT /api/admin/categories/{id}
        // Güncelleme ve aktif/pasif yapma işlemleri yalnızca yerel DB'de uygulanır, Mikro'ya dokunmaz.
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

            if (category.NavbarGroupKey != null)
            {
                existing.NavbarGroupKey = string.IsNullOrWhiteSpace(category.NavbarGroupKey)
                    ? null
                    : category.NavbarGroupKey.Trim();
            }

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

        public sealed class ToggleCategoryActiveRequest
        {
            public bool? IsActive { get; set; }
        }

        // PATCH /api/admin/categories/{id}/toggle-active
        // Kategoriyi veya alt kategoriyi sitede aktif/pasif yapar (Mikro'ya dokunmaz).
        [HttpPatch("{id}/toggle-active")]
        [HasPermission(Permissions.Categories.Update)]
        public async Task<IActionResult> ToggleCategoryActive(int id, [FromBody] ToggleCategoryActiveRequest? request = null)
        {
            var existing = await _categoryService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.IsActive = request?.IsActive ?? !existing.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _categoryService.UpdateAsync(existing);

            return Ok(new
            {
                id = existing.Id,
                name = existing.Name,
                isActive = existing.IsActive,
                message = existing.IsActive
                    ? $"'{existing.Name}' kategorisi aktif edildi."
                    : $"'{existing.Name}' kategorisi pasife alındı."
            });
        }

        // POST /api/admin/categories/sync-mikro
        // Mikro ERP'deki stok ana/alt gruplarını yerel kategori ağacıyla senkronize eder.
        [HttpPost("sync-mikro")]
        [HasPermission(Permissions.Categories.Update)]
        public async Task<IActionResult> SyncCategoriesFromMikro()
        {
            var mikroDbService = HttpContext?.RequestServices?.GetService(typeof(IMikroDbService)) as IMikroDbService;
            await CategorySeeder.SeedAsync(_dbContext, _mikroGrupDbService, mikroDbService, HttpContext?.RequestAborted ?? default);
            return Ok(new { message = "Mikro ERP kategori ve alt kategori hiyerarşisi başarıyla senkronize edildi." });
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
                // Hard delete: Kategoriyi tamamen sil (yerel DB'den)
                await _categoryService.DeleteAsync(category);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✨ GET /api/admin/categories/tree - Hiyerarşik kategori ağacı (aktif + pasif tümü)
        [HttpGet("tree")]
        [HasPermission(Permissions.Categories.View)]
        public async Task<IActionResult> GetCategoryTree()
        {
            var tree = (await _categoryService.GetAdminCategoryTreeAsync()).ToList();

            IReadOnlyDictionary<int, int> productCounts = new Dictionary<int, int>();
            try
            {
                using var countCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                countCts.CancelAfter(TimeSpan.FromSeconds(4));
                productCounts = await _adminCatalogStatsService.GetLocalActiveProductCountsByCategoryAsync(
                    countCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin kategori ağacı ürün sayımı atlandı.");
            }

            ApplyCatalogProductCounts(tree, productCounts);
            return Ok(tree);
        }

        private static void ApplyCatalogProductCounts(
            IEnumerable<CategoryTreeDto> nodes,
            IReadOnlyDictionary<int, int> productCounts)
        {
            foreach (var node in nodes)
            {
                var directCount = productCounts.TryGetValue(node.Id, out var count) ? count : 0;
                if (node.Children?.Count > 0)
                {
                    ApplyCatalogProductCounts(node.Children, productCounts);
                    node.ProductCount = directCount + node.Children.Sum(c => c.ProductCount);
                }
                else
                {
                    node.ProductCount = directCount;
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

        /// <summary>
        /// Kategori görseli yükler (ana sayfa keşif grid'i için).
        /// </summary>
        [HttpPost("upload-image")]
        [HasPermission(Permissions.Categories.Create)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UploadCategoryImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Lütfen bir görsel dosyası seçin." });
                }

                if (image.Length > MaxFileSize)
                {
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir." });
                }

                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                }

                var mimeType = (image.ContentType ?? string.Empty).ToLowerInvariant();
                if (!AllowedMimeTypes.Contains(mimeType))
                {
                    return BadRequest(new { message = "Geçersiz dosya türü. Sadece resim dosyaları kabul edilir." });
                }

                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    var safeName = $"category_{Path.GetFileNameWithoutExtension(image.FileName)}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                    imageUrl = await _fileStorage.UploadAsync(stream, safeName, mimeType);
                }

                _logger.LogInformation("Kategori görseli yüklendi: {ImageUrl}", imageUrl);
                return Ok(new { success = true, imageUrl, message = "Görsel başarıyla yüklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori görseli yüklenirken hata oluştu");
                return StatusCode(500, new { message = "Görsel yüklenirken bir hata oluştu." });
            }
        }
    }
}
