using ECommerce.API.Data;
using ECommerce.Business.Helpers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Category;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/categories
    public class CategoriesController : ControllerBase
    {
        private static readonly Dictionary<string, string> CategorySlugAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sut-urunleri"] = "sut-ve-sut-urunleri",
            ["meyve-sebze"] = "meyve-ve-sebze",
            ["et-tavuk"] = "et-ve-et-urunleri",
            ["et-tavuk-balik"] = "et-ve-et-urunleri",
            ["dondurulmus-gida"] = "dondurma-ve-dondurulmus-gida",
            ["dondurma-dondurulmus"] = "dondurma-ve-dondurulmus-gida"
        };

        private static bool _initialProductClassificationCompleted;

        private readonly ICategoryService _categoryService;
        private readonly ECommerceDbContext? _dbContext;
        private readonly IOrderLimitResolver? _orderLimitResolver;
        private readonly IProductOrderLimitSettingsService? _limitSettingsService;

        public CategoriesController(
            ICategoryService categoryService,
            ECommerceDbContext? dbContext = null,
            IOrderLimitResolver? orderLimitResolver = null,
            IProductOrderLimitSettingsService? limitSettingsService = null)
        {
            _categoryService = categoryService;
            _dbContext = dbContext;
            _orderLimitResolver = orderLimitResolver;
            _limitSettingsService = limitSettingsService;
        }

        private static string NormalizeCategorySlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return string.Empty;
            var trimmed = slug.Trim().ToLowerInvariant();
            return CategorySlugAliases.TryGetValue(trimmed, out var alias) ? alias : trimmed;
        }

        /// <summary>
        /// Web sitesindeki navigasyon menüsü için tüm aktif kategorileri listeler.
        /// Üst kategorisi pasif olan alt kategoriler de gizlenir.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryListDto>), 200)]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = (await _categoryService.GetAllAsync()).ToList();
            var activeRootIds = categories
                .Where(c => c.ParentId == null && c.IsActive && !string.Equals(c.Slug, CategorySeeder.UncategorizedSlug, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToHashSet();

            var storefrontCategories = categories
                .Where(c =>
                    c.IsActive &&
                    !string.Equals(c.Slug, CategorySeeder.UncategorizedSlug, StringComparison.OrdinalIgnoreCase) &&
                    (!c.ParentId.HasValue || activeRootIds.Contains(c.ParentId.Value)))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToList();

            var subCategoriesByParent = storefrontCategories
                .Where(c => c.ParentId.HasValue)
                .GroupBy(c => c.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Name).ToList());

            var categoryDtos = storefrontCategories.Select(c => new CategoryListDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                ParentId = c.ParentId,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                MikroAnaGrupKod = c.MikroAnaGrupKod,
                MikroAltGrupKod = c.MikroAltGrupKod,
                NavbarGroupKey = c.NavbarGroupKey,
                SubCategories = c.ParentId == null && subCategoriesByParent.TryGetValue(c.Id, out var subs)
                    ? subs.Select(sc => new CategoryListDto
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Slug = sc.Slug,
                        ImageUrl = sc.ImageUrl,
                        ParentId = sc.ParentId,
                        IsActive = sc.IsActive,
                        SortOrder = sc.SortOrder,
                        MikroAnaGrupKod = sc.MikroAnaGrupKod,
                        MikroAltGrupKod = sc.MikroAltGrupKod,
                        NavbarGroupKey = sc.NavbarGroupKey
                    }).ToList()
                    : new List<CategoryListDto>()
            });

            return Ok(categoryDtos);
        }

        /// <summary>
        /// Slug ile tek bir kategorinin detaylarını ve aktif alt kategorilerini getirir.
        /// Birleşik navbar gruplarında (örn. atistirmalik = 1400 + 1100) ilgili tüm aktif alt kategorileri döner.
        /// </summary>
        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(CategoryDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCategoryBySlug(string slug)
        {
            var normalizedSlug = NormalizeCategorySlug(slug);
            var allActive = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Slug != CategorySeeder.UncategorizedSlug)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(HttpContext.RequestAborted);

            var category = allActive.FirstOrDefault(c =>
                string.Equals(c.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase))
                ?? allActive.FirstOrDefault(c =>
                    c.ParentId == null &&
                    !string.IsNullOrWhiteSpace(c.NavbarGroupKey) &&
                    string.Equals(c.NavbarGroupKey, normalizedSlug, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                return NotFound($"Slug '{slug}' ile eşleşen bir kategori bulunamadı.");
            }

            // Üst kategorisi pasif olan bir alt kategoriye doğrudan erişilmeye çalışılırsa 404 dön
            if (category.ParentId.HasValue && !allActive.Any(c => c.Id == category.ParentId.Value))
            {
                return NotFound($"Slug '{slug}' ile eşleşen aktif bir kategori bulunamadı.");
            }

            var rootIds = ResolveCombinedRootCategoryIds(category, allActive);
            var subCategories = allActive
                .Where(c => c.ParentId.HasValue && rootIds.Contains(c.ParentId.Value))
                .OrderBy(c => c.ParentId == category.Id ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(sc => new CategoryListDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Slug = sc.Slug,
                    ImageUrl = sc.ImageUrl,
                    ParentId = sc.ParentId,
                    IsActive = sc.IsActive,
                    SortOrder = sc.SortOrder,
                    MikroAnaGrupKod = sc.MikroAnaGrupKod,
                    MikroAltGrupKod = sc.MikroAltGrupKod,
                    NavbarGroupKey = sc.NavbarGroupKey
                })
                .ToList();

            var categoryDetailDto = new CategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                ImageUrl = category.ImageUrl,
                ParentId = category.ParentId,
                SortOrder = category.SortOrder,
                MikroAnaGrupKod = category.MikroAnaGrupKod,
                MikroAltGrupKod = category.MikroAltGrupKod,
                NavbarGroupKey = category.NavbarGroupKey,
                SubCategories = subCategories
            };

            return Ok(categoryDetailDto);
        }

        /// <summary>
        /// Kategori sayfası için üstteki yapışkan (sticky) alt kategori barını ve
        /// aşağı kaydırdıkça geçiş yapılan alt kategori ürün bölümlerini (scroll-spy sections) döner.
        /// Vitrinde ürün sayısı gösterilmez.
        /// </summary>
        [HttpGet("{slug}/sections")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCategorySections(string slug, CancellationToken cancellationToken = default)
        {
            if (_dbContext == null)
            {
                return NotFound(new { message = "Veritabanı bağlamı mevcut değil." });
            }

            var normalizedSlug = NormalizeCategorySlug(slug);
            var allActive = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Slug != CategorySeeder.UncategorizedSlug)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var matched = allActive.FirstOrDefault(c =>
                string.Equals(c.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase))
                ?? allActive.FirstOrDefault(c =>
                    c.ParentId == null &&
                    !string.IsNullOrWhiteSpace(c.NavbarGroupKey) &&
                    string.Equals(c.NavbarGroupKey, normalizedSlug, StringComparison.OrdinalIgnoreCase));

            if (matched == null)
            {
                return NotFound(new { message = $"'{slug}' kategorisi bulunamadı." });
            }

            string? initialSubCategorySlug = null;
            Category rootCategory = matched;

            if (matched.ParentId.HasValue)
            {
                var parent = allActive.FirstOrDefault(c => c.Id == matched.ParentId.Value);
                if (parent == null)
                {
                    return NotFound(new { message = $"'{slug}' kategorisi bulunamadı." });
                }
                rootCategory = parent;
                initialSubCategorySlug = matched.Slug;
            }

            var rootIds = ResolveCombinedRootCategoryIds(rootCategory, allActive);
            var rootCategories = allActive
                .Where(c => rootIds.Contains(c.Id))
                .OrderBy(c => c.Id == rootCategory.Id ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ToList();

            var subCategories = allActive
                .Where(c => c.ParentId.HasValue && rootIds.Contains(c.ParentId.Value))
                .OrderBy(c => c.ParentId == rootCategory.Id ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToList();

            var allTargetCategoryIds = new HashSet<int>(rootIds);
            foreach (var sc in subCategories)
            {
                allTargetCategoryIds.Add(sc.Id);
            }

            // Eğer ürünlerin bir kısmı hâlâ doğrudan ana kategoride (Genel) kalmışsa veya
            // uygulama açılışından beri alt kategori sınıflandırması henüz çalışmamışsa anında çalıştır.
            if (subCategories.Count > 0)
            {
                var hasDirectRootProducts = await _dbContext.Products
                    .AsNoTracking()
                    .AnyAsync(p => p.IsActive && rootIds.Contains(p.CategoryId), cancellationToken);

                if (!_initialProductClassificationCompleted || hasDirectRootProducts)
                {
                    _initialProductClassificationCompleted = true;
                    var mikroDb = HttpContext?.RequestServices?.GetService(
                        typeof(ECommerce.Infrastructure.Services.MicroServices.IMikroDbService))
                        as ECommerce.Infrastructure.Services.MicroServices.IMikroDbService;
                    await CategorySeeder.EnsureProductsClassifiedAsync(_dbContext, mikroDb, cancellationToken);
                }
            }

            // Bu kategori ve alt kategorilerine ait stokta olan, aktif ve fiyatlı ürünleri çek
            var products = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0 &&
                    p.Price > 0 &&
                    allTargetCategoryIds.Contains(p.CategoryId))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            var limitSettings = _limitSettingsService != null
                ? await _limitSettingsService.GetActiveSettingsAsync()
                : new ProductOrderLimitSettingsDto();

            var productsByCategoryId = products
                .GroupBy(p => p.CategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => MapProductToStorefrontDto(p, limitSettings)).ToList());

            var rootNameById = rootCategories.ToDictionary(r => r.Id, r => r.Name);

            var sections = new List<object>();
            var stickySubCategories = new List<object>();

            foreach (var sub in subCategories)
            {
                productsByCategoryId.TryGetValue(sub.Id, out var subProducts);
                subProducts ??= new List<ProductListDto>();

                // Sadece ürünü olan alt kategorileri veya seçili alt kategoriyi vitrin bölümlerinde göster;
                // eğer hiçbir alt kategoride ürün yoksa tüm aktif alt kategorileri başlık olarak döndür.
                stickySubCategories.Add(new
                {
                    id = sub.Id,
                    name = sub.Name,
                    slug = sub.Slug,
                    parentId = sub.ParentId,
                    parentName = sub.ParentId.HasValue && rootNameById.TryGetValue(sub.ParentId.Value, out var pName) ? pName : rootCategory.Name,
                    mikroAnaGrupKod = sub.MikroAnaGrupKod,
                    mikroAltGrupKod = sub.MikroAltGrupKod,
                    sortOrder = sub.SortOrder,
                    hasProducts = subProducts.Count > 0
                });

                sections.Add(new
                {
                    id = sub.Id,
                    name = sub.Name,
                    slug = sub.Slug,
                    parentId = sub.ParentId,
                    parentName = sub.ParentId.HasValue && rootNameById.TryGetValue(sub.ParentId.Value, out var rName) ? rName : rootCategory.Name,
                    mikroAnaGrupKod = sub.MikroAnaGrupKod,
                    mikroAltGrupKod = sub.MikroAltGrupKod,
                    sortOrder = sub.SortOrder,
                    products = subProducts
                });
            }

            // Doğrudan ana kategoriye bağlı (alt kategorisi olmayan) ürünler varsa en sona ekle
            foreach (var root in rootCategories)
            {
                if (productsByCategoryId.TryGetValue(root.Id, out var directProducts) && directProducts.Count > 0)
                {
                    var directSlug = subCategories.Count > 0 ? $"{root.Slug}-genel" : root.Slug;
                    var directName = subCategories.Count > 0 ? $"{root.Name} (Genel)" : root.Name;

                    stickySubCategories.Add(new
                    {
                        id = root.Id,
                        name = directName,
                        slug = directSlug,
                        parentId = (int?)root.Id,
                        parentName = root.Name,
                        mikroAnaGrupKod = root.MikroAnaGrupKod,
                        mikroAltGrupKod = (string?)null,
                        sortOrder = 9999,
                        hasProducts = true
                    });

                    sections.Add(new
                    {
                        id = root.Id,
                        name = directName,
                        slug = directSlug,
                        parentId = (int?)root.Id,
                        parentName = root.Name,
                        mikroAnaGrupKod = root.MikroAnaGrupKod,
                        mikroAltGrupKod = (string?)null,
                        sortOrder = 9999,
                        products = directProducts
                    });
                }
            }

            return Ok(new
            {
                category = new
                {
                    id = rootCategory.Id,
                    name = rootCategory.Name,
                    slug = rootCategory.Slug,
                    description = rootCategory.Description,
                    imageUrl = rootCategory.ImageUrl,
                    mikroAnaGrupKod = rootCategory.MikroAnaGrupKod,
                    navbarGroupKey = rootCategory.NavbarGroupKey,
                    initialSubCategorySlug
                },
                subCategories = stickySubCategories,
                sections
            });
        }

        private static List<int> ResolveCombinedRootCategoryIds(Category category, List<Category> allActive)
        {
            if (category.ParentId.HasValue)
            {
                return new List<int> { category.ParentId.Value };
            }

            // Eğer bu ana kategori birleşik bir navbar grubunun ana temsilcisiyse (örn. atistirmalik = 1400 + 1100),
            // aynı NavbarGroupKey'i paylaşan tüm aktif ana kategorileri dahil et.
            if (!string.IsNullOrWhiteSpace(category.NavbarGroupKey) &&
                string.Equals(category.Slug, category.NavbarGroupKey, StringComparison.OrdinalIgnoreCase))
            {
                return allActive
                    .Where(c =>
                        c.ParentId == null &&
                        string.Equals(c.NavbarGroupKey, category.NavbarGroupKey, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.Id == category.Id ? 0 : 1)
                    .ThenBy(c => c.SortOrder)
                    .Select(c => c.Id)
                    .ToList();
            }

            return new List<int> { category.Id };
        }

        private ProductListDto MapProductToStorefrontDto(Product product, ProductOrderLimitSettingsDto limitSettings)
        {
            var imageUrls = product.ProductImages != null && product.ProductImages.Any(pi => pi.IsActive && !string.IsNullOrWhiteSpace(pi.Url))
                ? product.ProductImages
                    .Where(pi => pi.IsActive && !string.IsNullOrWhiteSpace(pi.Url))
                    .OrderByDescending(pi => pi.IsMain)
                    .ThenBy(pi => pi.Id)
                    .Select(pi => pi.Url.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : (!string.IsNullOrWhiteSpace(product.ImageUrl)
                    ? new List<string> { product.ImageUrl.Trim() }
                    : new List<string>());

            var isWeightBased = WeightBasedProductResolver.ResolveIsWeightBased(product);
            var dto = new ProductListDto
            {
                Id = product.Id,
                Sku = product.SKU,
                Name = product.Name,
                Slug = product.Slug ?? string.Empty,
                Description = product.Description ?? string.Empty,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = imageUrls.FirstOrDefault() ?? product.ImageUrl,
                ImageUrls = imageUrls,
                Brand = product.Brand?.Name ?? string.Empty,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                IsWeightBased = isWeightBased,
                Unit = !isWeightBased
                    ? "ADET"
                    : product.WeightUnit switch
                    {
                        WeightUnit.Kilogram => "KG",
                        WeightUnit.Gram => "GR",
                        WeightUnit.Liter => "LT",
                        WeightUnit.Milliliter => "ML",
                        _ => "ADET"
                    },
                MaxOrderQuantity = product.MaxOrderQuantity,
                MinOrderQuantity = product.MinOrderQuantity,
                QuantityStep = product.QuantityStep,
                MinOrderWeight = product.MinOrderWeight,
                MaxOrderWeight = product.MaxOrderWeight
            };

            if (product.SpecialPrice.HasValue && product.SpecialPrice.Value > 0 && product.SpecialPrice.Value < product.Price)
            {
                dto.OriginalPrice = product.Price;
                dto.DiscountPercentage = (int)Math.Round((1 - (product.SpecialPrice.Value / product.Price)) * 100);
            }

            if (_orderLimitResolver != null)
            {
                dto.OrderLimits = _orderLimitResolver.ResolveLimits(product, null, limitSettings);
            }

            return dto;
        }

        // ✨ GET /api/categories/tree - Hiyerarşik kategori ağacı (sadece aktif)
        [HttpGet("tree")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCategoryTree()
        {
            var tree = await _categoryService.GetCategoryTreeAsync();
            var activeTree = tree.Where(c => c.IsActive).ToList();
            return Ok(activeTree);
        }

        // ✨ GET /api/categories/root - Ana kategoriler (sadece aktif)
        [HttpGet("root")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRootCategories()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(categories.Where(c => c.IsActive && !string.Equals(c.Slug, CategorySeeder.UncategorizedSlug, StringComparison.OrdinalIgnoreCase)));
        }

        // ✨ GET /api/categories/{id}/subcategories - Belirli kategorinin alt kategorileri
        [HttpGet("{id:int}/subcategories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryListDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSubCategories(int id)
        {
            var subCategories = await _categoryService.GetSubCategoriesAsync(id);
            
            var subCategoryDtos = subCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryListDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = c.ImageUrl,
                    ParentId = c.ParentId,
                    IsActive = c.IsActive,
                    SortOrder = c.SortOrder,
                    MikroAnaGrupKod = c.MikroAnaGrupKod,
                    MikroAltGrupKod = c.MikroAltGrupKod,
                    NavbarGroupKey = c.NavbarGroupKey
                });

            return Ok(subCategoryDtos);
        }
    }
}
