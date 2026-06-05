using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ECommerce.Core.Extensions;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.DTOs.ProductReview;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Constants;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Core.Helpers;
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün yönetimi için controller.
    /// Public endpoint'ler + Admin CRUD + Resim yükleme desteği sağlar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
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

        private static readonly Dictionary<string, string[]> ProductNameCategoryHints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["temel-gida"] = new[] { "recel", "reçel", "pekmez", "marmelat", "bal", "tahin" },
            ["dondurma-ve-dondurulmus-gida"] = new[] { "dondurma", "donmus", "donmuş", "dondurulmus", "dondurulmuş", "superfresh", "super fresh" },
            ["ev-ve-mutfak"] = new[] { "pisirme kagidi", "pişirme kağıdı", "kagit", "kağıt", "servis seti", "servis", "folyo" },
            ["temizlik"] = new[] { "eldiven", "muayene" },
            ["sut-ve-sut-urunleri"] = new[] { "nesquik" },
        };

        private static readonly HashSet<string> ProductHintOverridableCategorySlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "atistirmalik",
            "temel-gida",
            "sut-ve-sut-urunleri"
        };

        private const string FrozenStorefrontCategorySlug = "dondurma-ve-dondurulmus-gida";
        private const string UncategorizedCategorySlug = "diger";

        private sealed class MergedProductRow
        {
            public ProductListDto Product { get; init; } = new();
            public string CategorySlug { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
            public bool IsVisible { get; init; } = true;
        }

        private sealed class DuplicateProductCandidate
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string Sku { get; init; } = string.Empty;
            public string Barcode { get; init; } = string.Empty;
            public decimal Price { get; init; }
            public decimal? SpecialPrice { get; init; }
            public int StockQuantity { get; init; }
            public bool IsActive { get; init; }
            public int CategoryId { get; init; }
            public string CategoryName { get; init; } = string.Empty;
            public string ExactMatchKey { get; init; } = string.Empty;
        }

        private sealed class DuplicateGroupResponse
        {
            public string GroupKey { get; init; } = string.Empty;
            public string Reason { get; init; } = string.Empty;
            public List<object> Products { get; init; } = new();
        }

        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileStorage _fileStorage;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMikroDbService _mikroDbService;
        private readonly IProductRepository _productRepository;
        private readonly ECommerceDbContext _dbContext;
        private readonly IAutoCategoryMappingEngine? _autoCategoryMappingEngine;
        private readonly IProductInfoSyncService? _productInfoSyncService;
        private readonly IProductAdminOverrideSettingsService _productAdminOverrideSettingsService;

        // İzin verilen dosya türleri (güvenlik için whitelist yaklaşımı)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif", ".avif", ".svg" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff", "image/avif", "image/svg+xml", "image/x-bmp" };
        
        // Maksimum dosya boyutu: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;
        // Import sırasında indirilen görsel başına max boyut: 15MB
        private const long MaxImportImageSize = 15 * 1024 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(
            IProductService productService, 
            IWebHostEnvironment environment,
            IFileStorage fileStorage,
            ILogger<ProductsController> logger,
            IMikroDbService mikroDbService,
            IProductRepository productRepository,
            ECommerceDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IProductAdminOverrideSettingsService productAdminOverrideSettingsService,
            IAutoCategoryMappingEngine? autoCategoryMappingEngine = null,
            IProductInfoSyncService? productInfoSyncService = null)
        {
            _productService = productService;
            _mikroDbService = mikroDbService;
            _productRepository = productRepository;
            _environment = environment;
            _fileStorage = fileStorage;
            _logger = logger;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _productAdminOverrideSettingsService = productAdminOverrideSettingsService;
            _autoCategoryMappingEngine = autoCategoryMappingEngine;
            _productInfoSyncService = productInfoSyncService;
        }

        [HttpGet("admin/override-settings")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetAdminOverrideSettings()
        {
            var settings = await _productAdminOverrideSettingsService.GetSettingsForAdminAsync(HttpContext.RequestAborted);
            return Ok(settings);
        }

        [HttpPut("admin/override-settings")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateAdminOverrideSettings([FromBody] ProductAdminOverrideSettingsUpdateDto request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Güncelleme verisi gerekli." });
            }

            var result = await _productAdminOverrideSettingsService.UpdateSettingsAsync(
                request,
                GetCurrentUserId(),
                GetCurrentUserName(),
                HttpContext.RequestAborted);

            return Ok(new
            {
                message = "Genel Mikro bağımsızlık ayarları güncellendi.",
                data = result
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            page = Math.Max(page, 1);
            size = Math.Min(Math.Max(size, 1), 250);

            if (_mikroDbService.IsConfigured)
            {
                var mergedProducts = await BuildMergedPublicProductsAsync(HttpContext.RequestAborted);
                if (mergedProducts.Count > 0)
                {
                    IEnumerable<MergedProductRow> filtered = mergedProducts;

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        filtered = filtered.Where(row => MatchesSearchQuery(row.Product, query));
                    }

                    var items = filtered
                        .OrderBy(row => row.Product.Name)
                        .Skip((page - 1) * size)
                        .Take(size)
                        .Select(row => row.Product)
                        .ToList();

                    return Ok(items);
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                var allProducts = await _productService.GetActiveProductsAsync(page, size);
                return Ok(allProducts);
            }

            var products = await _productService.SearchProductsAsync(query, page, size);
            return Ok(products);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] int? categoryId = null)
        {
            // Mikro ERP aktifse: Mikro ürünlerini local DB kategori bilgisiyle birleştir
            if (_mikroDbService.IsConfigured)
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                if (unified.Count == 0)
                {
                    _logger.LogWarning("[Products] Mikro configured but returned 0 products. Falling back to local DB. CategoryId: {CategoryId}", categoryId);
                    var fallbackProducts = await _productService.GetActiveProductsWithCampaignAsync(page, size, categoryId);
                    return Ok(fallbackProducts);
                }

                var requestedCategory = categoryId.HasValue
                    ? await _dbContext.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == categoryId.Value, HttpContext.RequestAborted)
                    : null;
                var requestedCategorySlug = ResolveRequestedCategorySlug(requestedCategory);

                // Local DB'deki ürünleri SKU bazlı eşle (kategori, isim, fiyat override)
                var localAll = await _dbContext.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .ToListAsync();

                var skuToLocal = localAll
                    .Where(p => !string.IsNullOrEmpty(p.SKU))
                    .GroupBy(p => p.SKU!)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Slug → CategoryId eşlemesi (DB'deki gerçek kategori id'leri)
                var slugToId = await _dbContext.Categories
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

                // Slug → CategoryName eşlemesi
                var slugToName = await _dbContext.Categories
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase);

                // CategoryId → Slug ters eşleme (doğrulama için)
                var idToSlug = slugToId.ToDictionary(kv => kv.Value, kv => kv.Key);

                // NEDEN: MikroCategoryMapping tablosundan AnagrupKod → CategoryId eşlemesi.
                // Otomatik kategori sistemi (ADIM 4-10) bu tabloya yazar.
                var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);
                var overrideDefaults = await GetProductOverrideDefaultsAsync(HttpContext.RequestAborted);

                var merged = unified
                    .Select(p =>
                    {
                        var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);

                        // NEDEN: Kategori çözümleme önceliği:
                        // 1. Mikro AnagrupKod → MikroCategoryMapping tablosu (en güvenilir)
                        // 2. Ürün adından keyword eşleme (MatchCategorySlug)
                        // 3. Local DB CategoryId KULLANILMAZ — eski bug'lı sync yanlış atamış
                        int? catId = null;
                        string catName = string.Empty;
                        string categorySlug = string.Empty;

                        var resolvedCategory = PreferLocalCategoryOverride(
                            hasLocal ? local : null,
                            overrideDefaults,
                            ResolveCategoryInfo(
                                p.AnagrupKod,
                                p.GrupKod,
                                p.StokAd,
                                categoryMappings,
                                idToSlug,
                                slugToName));

                        catId = resolvedCategory.CategoryId;
                        catName = resolvedCategory.CategoryName;
                        categorySlug = resolvedCategory.CategorySlug;

                        return new
                        {
                            Product = new ProductListDto
                            {
                                Id = hasLocal ? local!.Id : 0,
                                // NEDEN: SKU, Id=0 ürünlerde detay sayfasına yönlendirmek için kullanılır
                                Sku = p.StokKod,
                                Name = ResolveDisplayName(p.StokAd, hasLocal ? local : null, overrideDefaults),
                                Slug = hasLocal && !string.IsNullOrEmpty(local!.Slug) ? local.Slug : GenerateSlug(p.StokAd),
                                Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                                Price = ResolveDisplayPrice(p.Fiyat, hasLocal ? local : null, overrideDefaults),
                                SpecialPrice = ResolveDisplaySpecialPrice(hasLocal ? local : null, overrideDefaults),
                                StockQuantity = (int)Math.Max(0, p.StokMiktar),
                                ImageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                                // NEDEN: Mikro ERP birim bilgisi — KG ürünlerde frontend ağırlık seçici gösterir
                                Unit = string.IsNullOrWhiteSpace(p.Birim) ? "ADET" : p.Birim.Trim().ToUpperInvariant(),
                                CategoryId = catId,
                                CategoryName = catName,
                                AdminOverrideName = hasLocal ? local!.AdminOverrideName : null,
                                AdminOverridePrice = hasLocal ? local!.AdminOverridePrice : null,
                                AdminOverrideCategory = hasLocal ? local!.AdminOverrideCategory : null,
                                EffectiveAdminOverrideName = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideName, overrideDefaults.DefaultAdminOverrideName) : false,
                                EffectiveAdminOverridePrice = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverridePrice, overrideDefaults.DefaultAdminOverridePrice) : false,
                                EffectiveAdminOverrideCategory = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideCategory, overrideDefaults.DefaultAdminOverrideCategory) : false,
                            },
                            CategorySlug = categorySlug,
                            IsVisible = hasLocal ? local!.IsActive : p.WebeGonderilecekFl,
                        };
                    })
                    .Where(x => x.IsVisible)
                    .Where(x => x.Product.Price > 0); // Fiyatsız ürünleri gösterme

                // Kategoriye göre filtrele
                if (categoryId.HasValue)
                {
                    merged = merged.Where(x =>
                        x.Product.CategoryId == categoryId.Value ||
                        (!string.IsNullOrWhiteSpace(requestedCategorySlug) &&
                         string.Equals(x.CategorySlug, requestedCategorySlug, StringComparison.OrdinalIgnoreCase)));
                }

                // Sırala ve sayfalandır
                var result = merged
                    .OrderByDescending(x => x.Product.HasActiveCampaign)
                    .ThenByDescending(x => x.Product.DiscountPercentage ?? 0)
                    .ThenBy(x => x.Product.Name)
                    .Skip((Math.Max(1, page) - 1) * Math.Max(1, size))
                    .Take(Math.Max(1, size))
                    .Select(x => x.Product)
                    .ToList();

                return Ok(result);
            }

            // Mikro yoksa sadece local DB
            var products = await _productService.GetActiveProductsWithCampaignAsync(page, size, categoryId);
            return Ok(products);
        }

        // Admin panel için tüm ürünleri getir — Mikro ERP SQL tabanlı (aktif ürünler)
        [HttpGet("admin/all")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetAllProductsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int size = 100,
            [FromQuery] string? sku = null,
            [FromQuery] string? name = null,
            [FromQuery] string? status = null,
            [FromQuery] string? stockStatus = null)
        {
            page = Math.Max(page, 1);
            size = Math.Clamp(size, 1, 200);

            if (_mikroDbService.IsConfigured)
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);

                // Yerel DB'deki ürünleri SKU bazlı eşle — yerel override katmanı
                var localAll = await _dbContext.Products
                    .Include(product => product.Category)
                    .AsNoTracking()
                    .ToListAsync(HttpContext.RequestAborted);
                var skuToLocal = localAll
                    .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                    .GroupBy(p => p.SKU.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                var unifiedSkuSet = new HashSet<string>(
                    unified
                        .Select(product => product.StokKod?.Trim())
                        .Where(productSku => !string.IsNullOrWhiteSpace(productSku))
                        .Cast<string>(),
                    StringComparer.OrdinalIgnoreCase);

                var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);

                var activeCategories = await _dbContext.Categories
                    .AsNoTracking()
                    .Where(category => category.IsActive)
                    .ToListAsync(HttpContext.RequestAborted);

                var idToSlug = activeCategories
                    .Where(category => category.Id > 0)
                    .GroupBy(category => category.Id)
                    .ToDictionary(
                        group => group.Key,
                        group => NormalizeCategorySlug(group.First().Slug ?? group.First().Name),
                        EqualityComparer<int>.Default);

                var slugToName = activeCategories
                    .SelectMany(category => new[]
                    {
                        new KeyValuePair<string, string>(NormalizeCategorySlug(category.Slug), category.Name ?? string.Empty),
                        new KeyValuePair<string, string>(NormalizeCategorySlug(category.Name), category.Name ?? string.Empty)
                    })
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                    .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

                var categoryLookup = activeCategories
                    .GroupBy(category => category.Id)
                    .ToDictionary(group => group.Key, group => group.First().Name ?? string.Empty);
                var overrideDefaults = await GetProductOverrideDefaultsAsync(HttpContext.RequestAborted);

                var mergedProducts = unified
                    .Select(p =>
                    {
                        // Yerel DB kaydı varsa override olarak kullan
                        var normalizedSku = p.StokKod?.Trim() ?? string.Empty;
                        var hasLocal = skuToLocal.TryGetValue(normalizedSku, out var local);
                        var resolvedCategoryInfo = ResolveCategoryInfo(
                            p.AnagrupKod,
                            p.GrupKod,
                            p.StokAd,
                            categoryMappings,
                            idToSlug,
                            slugToName);

                        var resolvedCategoryId = hasLocal && ProductAdminOverridePolicy.ShouldUseAdminCategory(local, overrideDefaults)
                            ? (int?)local.CategoryId
                            : resolvedCategoryInfo.CategoryId;
                        var resolvedCategoryName = resolvedCategoryId.HasValue &&
                                                   categoryLookup.TryGetValue(resolvedCategoryId.Value, out var categoryName)
                            ? categoryName
                            : resolvedCategoryInfo.CategoryName;
                        return new
                        {
                            id = hasLocal ? local!.Id : 0,
                            sku = p.StokKod,
                            name = ResolveDisplayName(p.StokAd, hasLocal ? local : null, overrideDefaults),
                            price = ResolveDisplayPrice(p.Fiyat, hasLocal ? local : null, overrideDefaults),
                            specialPrice = ResolveDisplaySpecialPrice(hasLocal ? local : null, overrideDefaults),
                            stockQuantity = (int)Math.Max(0, p.StokMiktar),
                            stock = (int)Math.Max(0, p.StokMiktar),
                            isActive = hasLocal ? local!.IsActive : p.WebeGonderilecekFl,
                            // Kategori: yerel override varsa yerel
                            categoryId = resolvedCategoryId,
                            categoryName = resolvedCategoryName,
                            categorySlug = hasLocal && !string.IsNullOrWhiteSpace(local!.Category?.Slug)
                                ? NormalizeCategorySlug(local.Category.Slug)
                                : resolvedCategoryInfo.CategorySlug,
                            categoryCode = p.GrupKod,
                            anagrupCode = p.AnagrupKod,
                            // Açıklama: yerel override
                            description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                            imageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                            adminOverrideName = hasLocal ? local!.AdminOverrideName : null,
                            adminOverridePrice = hasLocal ? local!.AdminOverridePrice : null,
                            adminOverrideCategory = hasLocal ? local!.AdminOverrideCategory : null,
                            effectiveAdminOverrideName = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideName, overrideDefaults.DefaultAdminOverrideName) : false,
                            effectiveAdminOverridePrice = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverridePrice, overrideDefaults.DefaultAdminOverridePrice) : false,
                            effectiveAdminOverrideCategory = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideCategory, overrideDefaults.DefaultAdminOverrideCategory) : false,
                            source = "mikro-erp"
                        };
                    })
                    .GroupBy(
                        product => !string.IsNullOrWhiteSpace(product.sku)
                            ? product.sku.Trim()
                            : $"local:{product.id}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First());

                var localOnlyProducts = localAll
                    .Where(product => string.IsNullOrWhiteSpace(product.SKU))
                    .Select(product => new
                    {
                        id = product.Id,
                        sku = product.SKU,
                        name = product.Name,
                        price = product.Price,
                        specialPrice = product.SpecialPrice,
                        stockQuantity = product.StockQuantity,
                        stock = product.StockQuantity,
                        isActive = product.IsActive,
                        categoryId = (int?)product.CategoryId,
                        categoryName = product.Category?.Name ?? categoryLookup.GetValueOrDefault(product.CategoryId, string.Empty),
                        categorySlug = NormalizeCategorySlug(product.Category?.Slug ?? product.Category?.Name ?? string.Empty),
                        categoryCode = string.Empty,
                        anagrupCode = string.Empty,
                        description = product.Description,
                        imageUrl = product.ImageUrl,
                        adminOverrideName = product.AdminOverrideName,
                        adminOverridePrice = product.AdminOverridePrice,
                        adminOverrideCategory = product.AdminOverrideCategory,
                        effectiveAdminOverrideName = ProductAdminOverridePolicy.ResolveOverride(product.AdminOverrideName, overrideDefaults.DefaultAdminOverrideName),
                        effectiveAdminOverridePrice = ProductAdminOverridePolicy.ResolveOverride(product.AdminOverridePrice, overrideDefaults.DefaultAdminOverridePrice),
                        effectiveAdminOverrideCategory = ProductAdminOverridePolicy.ResolveOverride(product.AdminOverrideCategory, overrideDefaults.DefaultAdminOverrideCategory),
                        source = "local-db"
                    });

                var filtered = mergedProducts
                    .Concat(localOnlyProducts)
                    .Where(product =>
                        string.IsNullOrWhiteSpace(sku) ||
                        (!string.IsNullOrWhiteSpace(product.sku) && product.sku.Contains(sku, StringComparison.OrdinalIgnoreCase)))
                    .Where(product =>
                        string.IsNullOrWhiteSpace(name) ||
                        (!string.IsNullOrWhiteSpace(product.name) && product.name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    .Where(product => MatchesAdminStatusFilter(product.isActive, status))
                    .Where(product => MatchesAdminStockFilter(product.stockQuantity, stockStatus));

                var total = filtered.Count();
                var paged = filtered
                    .OrderBy(product => string.IsNullOrWhiteSpace(product.sku) ? product.name : product.sku)
                    .ThenBy(product => product.name)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                return Ok(new
                {
                    items = paged,
                    total,
                    page,
                    pageSize = size,
                    totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size))
                });
            }

            var localProducts = await _dbContext.Products
                .Include(product => product.Category)
                .AsNoTracking()
                .ToListAsync(HttpContext.RequestAborted);

            var filteredLocalProducts = localProducts
                .Where(product =>
                    string.IsNullOrWhiteSpace(sku) ||
                    (!string.IsNullOrWhiteSpace(product.SKU) && product.SKU.Contains(sku, StringComparison.OrdinalIgnoreCase)))
                .Where(product =>
                    string.IsNullOrWhiteSpace(name) ||
                    (!string.IsNullOrWhiteSpace(product.Name) && product.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .Where(product => MatchesAdminStatusFilter(product.IsActive, status))
                .Where(product => MatchesAdminStockFilter(product.StockQuantity, stockStatus))
                .Select(product => new
                {
                    id = product.Id,
                    sku = product.SKU,
                    name = product.Name,
                    price = product.Price,
                    specialPrice = product.SpecialPrice,
                    stockQuantity = product.StockQuantity,
                    stock = product.StockQuantity,
                    isActive = product.IsActive,
                    categoryId = product.CategoryId,
                    categoryName = product.Category != null ? product.Category.Name : string.Empty,
                    description = product.Description,
                    imageUrl = product.ImageUrl,
                    source = "local-db"
                });

            var totalLocal = filteredLocalProducts.Count();
            var pagedLocal = filteredLocalProducts
                .OrderBy(product => string.IsNullOrWhiteSpace(product.sku) ? product.name : product.sku)
                .ThenBy(product => product.name)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return Ok(new
            {
                items = pagedLocal,
                total = totalLocal,
                page,
                pageSize = size,
                totalPages = Math.Max(1, (int)Math.Ceiling(totalLocal / (double)size))
            });
        }

        private static bool MatchesAdminStatusFilter(bool isActive, string? status)
        {
            return NormalizeAdminFilterValue(status) switch
            {
                "active" => isActive,
                "inactive" => !isActive,
                _ => true,
            };
        }

        private static bool MatchesAdminStockFilter(int stockQuantity, string? stockStatus)
        {
            return NormalizeAdminFilterValue(stockStatus) switch
            {
                "in-stock" => stockQuantity > 0,
                "out-of-stock" => stockQuantity <= 0,
                _ => true,
            };
        }

        private static string NormalizeAdminFilterValue(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Mikro ERP ürünlerini otomatik kategorize eder.
        /// Ürün adına göre akıllı eşleme yapar, eksik kategorileri oluşturur,
        /// yerel DB'de ürün kaydı yoksa SKU bazlı upsert yapar.
        /// </summary>
        [HttpPost("admin/auto-categorize")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> AutoCategorizeProducts()
        {
            if (!_mikroDbService.IsConfigured)
                return BadRequest(new { message = "Mikro ERP bağlantısı yapılandırılmamış." });

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            if (unified.Count == 0)
                return Ok(new { message = "Mikro ERP'de ürün bulunamadı.", created = 0, updated = 0 });

            var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);

            // 1) Mevcut yerel ürünleri SKU bazlı indexle
            var localProducts = await _dbContext.Products.ToListAsync();
            var skuToLocal = localProducts
                .Where(p => !string.IsNullOrEmpty(p.SKU))
                .GroupBy(p => p.SKU!)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var overrideDefaults = await GetProductOverrideDefaultsAsync(HttpContext.RequestAborted);

            int created = 0, updated = 0, categorized = 0, resolvedByEngine = 0;

            foreach (var mikro in unified)
            {
                var categoryId = ResolveCategoryId(mikro.AnagrupKod, mikro.GrupKod, categoryMappings);
                if (!categoryId.HasValue && _autoCategoryMappingEngine != null)
                {
                    categoryId = await _autoCategoryMappingEngine.ResolveOrCreateMappingAsync(
                        mikro.AnagrupKod ?? string.Empty,
                        mikro.GrupKod,
                        HttpContext.RequestAborted);

                    CacheResolvedMapping(categoryMappings, mikro.AnagrupKod, categoryId.Value);
                    resolvedByEngine++;
                }

                categoryId ??= await GetFallbackCategoryIdAsync(HttpContext.RequestAborted);

                if (skuToLocal.TryGetValue(mikro.StokKod, out var local))
                {
                    if (ProductAdminOverridePolicy.CanSyncName(local, overrideDefaults))
                    {
                        local.Name = mikro.StokAd;
                    }

                    if (mikro.Fiyat > 0 && ProductAdminOverridePolicy.CanSyncPrice(local, overrideDefaults))
                    {
                        local.Price = mikro.Fiyat;
                    }
                    local.StockQuantity = (int)Math.Max(0, mikro.StokMiktar);
                    if (ProductAdminOverridePolicy.CanSyncCategory(local, overrideDefaults))
                    {
                        local.CategoryId = categoryId.Value;
                    }
                    local.UpdatedAt = DateTime.UtcNow;
                    updated++;
                    categorized++;
                }
                else
                {
                    var newProduct = new Product
                    {
                        Name = mikro.StokAd,
                        SKU = mikro.StokKod,
                        Price = mikro.Fiyat,
                        StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                        CategoryId = categoryId.Value,
                        Description = string.Empty,
                        ImageUrl = string.Empty,
                        IsActive = mikro.WebeGonderilecekFl,
                        Slug = GenerateSlug(mikro.StokAd),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.Products.Add(newProduct);
                    skuToLocal[mikro.StokKod] = newProduct; // Duplicate engelle
                    created++;
                    categorized++;
                }
            }

            await _dbContext.SaveChangesAsync();

            // 2) Mapping tabanlı yeniden kategorileme ve yetim düzeltmesi
            RecategorizeResult? recategorizeResult = null;
            if (_productInfoSyncService != null)
            {
                recategorizeResult = await _productInfoSyncService.RecategorizeAllProductsAsync(HttpContext.RequestAborted);
            }

            // 3) Kategori bazlı dağılım raporu
            var categoryNameById = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.Name, HttpContext.RequestAborted);

            var distribution = unified
                .Select(p => ResolveCategoryId(p.AnagrupKod, p.GrupKod, categoryMappings) ?? 0)
                .Where(categoryId => categoryId > 0)
                .GroupBy(categoryId => categoryId)
                .Select(g => new
                {
                    categoryId = g.Key,
                    category = categoryNameById.GetValueOrDefault(g.Key, "Bilinmeyen"),
                    productCount = g.Count()
                })
                .OrderByDescending(x => x.productCount)
                .ToList();

            _logger.LogInformation(
                "[AutoCategorize] Tamamlandı. Toplam: {Total}, Yeni: {Created}, Güncellenen: {Updated}, Kategorize: {Categorized}",
                unified.Count, created, updated, categorized);

            return Ok(new
            {
                message = $"Otomatik kategorileme tamamlandı.",
                totalProducts = unified.Count,
                created,
                updated,
                categorized,
                resolvedByEngine,
                recategorize = recategorizeResult,
                distribution
            });
        }

        /// <summary>
        /// URL-friendly slug oluşturur (Türkçe karakter desteği).
        /// </summary>
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var slug = text.ToLowerInvariant()
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

            // Alfanumerik ve tire dışı karakterleri temizle
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s]+", "-");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        private static string NormalizeCategorySlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return string.Empty;

            var normalized = GenerateSlug(slug);
            return CategorySlugAliases.TryGetValue(normalized, out var alias)
                ? alias
                : normalized;
        }

        private static string ResolveRequestedCategorySlug(Category? category)
        {
            if (category == null) return string.Empty;

            var slugFromEntity = NormalizeCategorySlug(category.Slug);
            var slugFromName = NormalizeCategorySlug(category.Name);

            if (!string.IsNullOrWhiteSpace(slugFromName) &&
                (string.IsNullOrWhiteSpace(slugFromEntity) ||
                 string.Equals(slugFromEntity, "diger", StringComparison.OrdinalIgnoreCase)))
            {
                return slugFromName;
            }

            return !string.IsNullOrWhiteSpace(slugFromEntity) ? slugFromEntity : slugFromName;
        }

        private static decimal ResolveDisplayPrice(decimal mikroPrice, Product? localProduct, ProductAdminOverrideSettingsDto overrideDefaults)
        {
            return ProductAdminOverridePolicy.ResolvePrice(mikroPrice, localProduct, overrideDefaults);
        }

        private static string ResolveDisplayName(string mikroName, Product? localProduct, ProductAdminOverrideSettingsDto overrideDefaults)
        {
            return ProductAdminOverridePolicy.ResolveName(mikroName, localProduct, overrideDefaults);
        }

        private static decimal? ResolveDisplaySpecialPrice(Product? localProduct, ProductAdminOverrideSettingsDto overrideDefaults)
        {
            return ProductAdminOverridePolicy.ResolveSpecialPrice(localProduct, overrideDefaults);
        }

        private static (int? CategoryId, string CategorySlug, string CategoryName) PreferLocalCategoryOverride(
            Product? localProduct,
            ProductAdminOverrideSettingsDto overrideDefaults,
            (int? CategoryId, string CategorySlug, string CategoryName) resolvedCategory)
        {
                if (ProductAdminOverridePolicy.ShouldUseAdminCategory(localProduct, overrideDefaults))
            {
                return (
                    localProduct!.CategoryId,
                    NormalizeCategorySlug(localProduct.Category?.Slug ?? string.Empty),
                    localProduct.Category?.Name ?? resolvedCategory.CategoryName);
            }

            return resolvedCategory;
        }

        private async Task<List<MergedProductRow>> BuildMergedPublicProductsAsync(CancellationToken cancellationToken)
        {
            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, cancellationToken);
            if (unified.Count == 0)
                return new List<MergedProductRow>();

            var overrideDefaults = await GetProductOverrideDefaultsAsync(cancellationToken);

            var localAll = await _dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var skuToLocal = localAll
                .Where(p => !string.IsNullOrEmpty(p.SKU))
                .GroupBy(p => p.SKU!)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var slugToId = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var slugToName = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var idToSlug = slugToId.ToDictionary(kv => kv.Value, kv => kv.Key);
            var categoryMappings = await LoadActiveCategoryMappingsAsync(cancellationToken);

            return unified
                .Select(p =>
                {
                    var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);
                    var resolvedCategory = PreferLocalCategoryOverride(
                        hasLocal ? local : null,
                        overrideDefaults,
                        ResolveCategoryInfo(
                            p.AnagrupKod,
                            p.GrupKod,
                            p.StokAd,
                            categoryMappings,
                            idToSlug,
                            slugToName));

                    return new MergedProductRow
                    {
                        Product = new ProductListDto
                        {
                            Id = hasLocal ? local!.Id : 0,
                            Sku = p.StokKod,
                            Name = ResolveDisplayName(p.StokAd, hasLocal ? local : null, overrideDefaults),
                            Slug = hasLocal && !string.IsNullOrEmpty(local!.Slug) ? local.Slug : GenerateSlug(p.StokAd),
                            Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                            Price = ResolveDisplayPrice(p.Fiyat, hasLocal ? local : null, overrideDefaults),
                            SpecialPrice = ResolveDisplaySpecialPrice(hasLocal ? local : null, overrideDefaults),
                            StockQuantity = (int)Math.Max(0, p.StokMiktar),
                            ImageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                            Unit = string.IsNullOrWhiteSpace(p.Birim) ? "ADET" : p.Birim.Trim().ToUpperInvariant(),
                            CategoryId = resolvedCategory.CategoryId,
                            CategoryName = resolvedCategory.CategoryName,
                            AdminOverrideName = hasLocal ? local!.AdminOverrideName : null,
                            AdminOverridePrice = hasLocal ? local!.AdminOverridePrice : null,
                            AdminOverrideCategory = hasLocal ? local!.AdminOverrideCategory : null,
                            EffectiveAdminOverrideName = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideName, overrideDefaults.DefaultAdminOverrideName) : false,
                            EffectiveAdminOverridePrice = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverridePrice, overrideDefaults.DefaultAdminOverridePrice) : false,
                            EffectiveAdminOverrideCategory = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideCategory, overrideDefaults.DefaultAdminOverrideCategory) : false,
                        },
                        CategorySlug = resolvedCategory.CategorySlug,
                        CreatedAt = hasLocal ? local!.CreatedAt : DateTime.MinValue,
                        IsVisible = hasLocal ? local!.IsActive : p.WebeGonderilecekFl,
                    };
                })
                .Where(row => row.IsVisible)
                .Where(row => row.Product.Price > 0)
                .ToList();
        }

        private static bool MatchesSearchQuery(ProductListDto product, string query)
        {
            var normalizedQuery = GenerateSlug(query);
            if (string.IsNullOrWhiteSpace(normalizedQuery))
                return true;

            return ContainsNormalizedSearchTerm(product.Name, normalizedQuery)
                || ContainsNormalizedSearchTerm(product.Description, normalizedQuery)
                || ContainsNormalizedSearchTerm(product.CategoryName, normalizedQuery)
                || ContainsNormalizedSearchTerm(product.Sku, normalizedQuery);
        }

        private static bool ContainsNormalizedSearchTerm(string? value, string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return GenerateSlug(value).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<MergedProductRow> ApplyMergedProductSort(
            IEnumerable<MergedProductRow> products,
            string? sort,
            bool isDescending)
        {
            var normalizedSort = sort?.Trim().ToLowerInvariant();

            return normalizedSort switch
            {
                "price" => isDescending
                    ? products.OrderByDescending(item => item.Product.Price).ThenBy(item => item.Product.Name)
                    : products.OrderBy(item => item.Product.Price).ThenBy(item => item.Product.Name),
                "newest" => isDescending
                    ? products.OrderBy(item => item.CreatedAt).ThenBy(item => item.Product.Name)
                    : products.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Product.Name),
                _ => isDescending
                    ? products.OrderByDescending(item => item.Product.Name)
                    : products.OrderBy(item => item.Product.Name)
            };
        }

        /// <summary>
        /// Sayfalı ürün listesi döndürür (PagedResult formatında).
        /// Toplu export işlemleri için optimize edilmiştir.
        /// </summary>
        /// <param name="page">Sayfa numarası (1'den başlar)</param>
        /// <param name="size">Sayfa başına ürün sayısı (max: 100)</param>
        /// <returns>PagedResult formatında ürün listesi</returns>
        [HttpGet("admin/paged")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetProductsPaged([FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            // Güvenlik: max sayfa boyutunu sınırla
            size = Math.Min(size, 100);
            page = Math.Max(page, 1);

            var pagedResult = await _productService.GetProductsPagedAsync(page, size);
            return Ok(pagedResult);
        }

        [HttpGet("category/{categoryId:int}/paged")]
        public async Task<IActionResult> GetProductsByCategoryPaged(
            int categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int size = 50,
            [FromQuery] string sort = "name",
            [FromQuery] string direction = "asc",
            [FromQuery] bool? inStock = null)
        {
            page = Math.Max(page, 1);
            size = Math.Min(Math.Max(size, 1), 100);

            if (_mikroDbService.IsConfigured)
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                if (unified.Count > 0)
                {
                    var requestedCategory = await _dbContext.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == categoryId, HttpContext.RequestAborted);
                    var requestedCategorySlug = ResolveRequestedCategorySlug(requestedCategory);

                    var localAll = await _dbContext.Products
                        .Include(p => p.Category)
                        .AsNoTracking()
                        .ToListAsync(HttpContext.RequestAborted);

                    var skuToLocal = localAll
                        .Where(p => !string.IsNullOrEmpty(p.SKU))
                        .GroupBy(p => p.SKU!)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var slugToId = await _dbContext.Categories
                        .AsNoTracking()
                        .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);

                    var slugToName = await _dbContext.Categories
                        .AsNoTracking()
                        .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase);

                    var idToSlug = slugToId.ToDictionary(kv => kv.Value, kv => kv.Key);
                    var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);
                    var overrideDefaults = await GetProductOverrideDefaultsAsync(HttpContext.RequestAborted);
                    var isDescending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

                    IEnumerable<MergedProductRow> merged = unified
                        .Select(p =>
                        {
                            var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);
                            var resolvedCategory = PreferLocalCategoryOverride(
                                hasLocal ? local : null,
                                overrideDefaults,
                                ResolveCategoryInfo(
                                    p.AnagrupKod,
                                    p.GrupKod,
                                    p.StokAd,
                                    categoryMappings,
                                    idToSlug,
                                    slugToName));

                            return new MergedProductRow
                            {
                                Product = new ProductListDto
                                {
                                    Id = hasLocal ? local!.Id : 0,
                                    Sku = p.StokKod,
                                    Name = ResolveDisplayName(p.StokAd, hasLocal ? local : null, overrideDefaults),
                                    Slug = hasLocal && !string.IsNullOrEmpty(local!.Slug) ? local.Slug : GenerateSlug(p.StokAd),
                                    Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : string.Empty,
                                    Price = ResolveDisplayPrice(p.Fiyat, hasLocal ? local : null, overrideDefaults),
                                    SpecialPrice = ResolveDisplaySpecialPrice(hasLocal ? local : null, overrideDefaults),
                                    StockQuantity = (int)Math.Max(0, p.StokMiktar),
                                    ImageUrl = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : string.Empty,
                                    Unit = string.IsNullOrWhiteSpace(p.Birim) ? "ADET" : p.Birim.Trim().ToUpperInvariant(),
                                    CategoryId = resolvedCategory.CategoryId,
                                    CategoryName = resolvedCategory.CategoryName,
                                    AdminOverrideName = hasLocal ? local!.AdminOverrideName : null,
                                    AdminOverridePrice = hasLocal ? local!.AdminOverridePrice : null,
                                    AdminOverrideCategory = hasLocal ? local!.AdminOverrideCategory : null,
                                    EffectiveAdminOverrideName = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideName, overrideDefaults.DefaultAdminOverrideName) : false,
                                    EffectiveAdminOverridePrice = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverridePrice, overrideDefaults.DefaultAdminOverridePrice) : false,
                                    EffectiveAdminOverrideCategory = hasLocal ? ProductAdminOverridePolicy.ResolveOverride(local!.AdminOverrideCategory, overrideDefaults.DefaultAdminOverrideCategory) : false,
                                },
                                CategorySlug = resolvedCategory.CategorySlug,
                                CreatedAt = hasLocal ? local!.CreatedAt : DateTime.MinValue,
                                IsVisible = hasLocal ? local!.IsActive : p.WebeGonderilecekFl,
                            };
                        })
                        .Where(x => x.IsVisible)
                        .Where(x => x.Product.Price > 0)
                        .Where(x =>
                            x.Product.CategoryId == categoryId ||
                            (!string.IsNullOrWhiteSpace(requestedCategorySlug) &&
                             string.Equals(x.CategorySlug, requestedCategorySlug, StringComparison.OrdinalIgnoreCase)));

                    if (inStock.HasValue)
                    {
                        merged = inStock.Value
                            ? merged.Where(x => x.Product.StockQuantity > 0)
                            : merged.Where(x => x.Product.StockQuantity <= 0);
                    }

                    var sorted = ApplyMergedProductSort(merged, sort, isDescending).ToList();
                    var total = sorted.Count;
                    var items = sorted
                        .Skip((page - 1) * size)
                        .Take(size)
                        .Select(x => x.Product)
                        .ToList();

                    return Ok(new PagedResult<ProductListDto>(items, total, (page - 1) * size, size));
                }
            }

            var pagedResult = await _productService.GetProductsByCategoryPagedAsync(
                categoryId,
                page,
                size,
                sort,
                direction,
                inStock);

            return Ok(pagedResult);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            // Kampanya bilgileriyle birlikte getir
            var product = await _productService.GetProductByIdWithCampaignAsync(id);
            if (product == null) return NotFound();

            // Local DB stok bilgisi stale olabilir — Mikro'dan gerçek zamanlı stok al
            if (_mikroDbService.IsConfigured && !string.IsNullOrEmpty(product.Sku))
            {
                var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                var mikro = unified.FirstOrDefault(u =>
                    string.Equals(u.StokKod, product.Sku, StringComparison.OrdinalIgnoreCase));
                if (mikro != null)
                {
                    product.StockQuantity = (int)Math.Max(0, mikro.StokMiktar);
                    product.Unit = string.IsNullOrWhiteSpace(mikro.Birim) ? "ADET" : mikro.Birim.Trim().ToUpperInvariant();
                }
            }

            return Ok(product);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetProductBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            slug = slug.Trim();

            var localProduct = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == slug, HttpContext.RequestAborted);

            if (localProduct != null)
            {
                var localDto = await _productService.GetProductByIdWithCampaignAsync(localProduct.Id);
                if (localDto != null)
                {
                    if (_mikroDbService.IsConfigured && !string.IsNullOrEmpty(localDto.Sku))
                    {
                        var unifiedLocal = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                        var mikroLocal = unifiedLocal.FirstOrDefault(u =>
                            string.Equals(u.StokKod, localDto.Sku, StringComparison.OrdinalIgnoreCase));

                        if (mikroLocal != null)
                        {
                            localDto.StockQuantity = (int)Math.Max(0, mikroLocal.StokMiktar);
                            localDto.Unit = string.IsNullOrWhiteSpace(mikroLocal.Birim)
                                ? "ADET"
                                : mikroLocal.Birim.Trim().ToUpperInvariant();
                        }
                    }

                    return Ok(localDto);
                }
            }

            if (!_mikroDbService.IsConfigured) return NotFound();

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            var mikro = unified.FirstOrDefault(u =>
                string.Equals(GenerateSlug(u.StokAd), slug, StringComparison.OrdinalIgnoreCase));

            if (mikro == null) return NotFound();

            return await GetProductBySku(mikro.StokKod);
        }

        /// <summary>
        /// SKU ile ürün detayı getir — Mikro ERP'de local DB kaydı olmayan (Id=0) ürünler için.
        /// Frontend, Id=0 ürünlerde SKU ile bu endpoint'i çağırır.
        /// </summary>
        [HttpGet("sku/{sku}")]
        public async Task<IActionResult> GetProductBySku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest();
            sku = sku.Trim();

            // Önce local DB'de ara
            var localProduct = await _dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku, HttpContext.RequestAborted);

            if (localProduct != null)
            {
                var localDto = await _productService.GetProductByIdWithCampaignAsync(localProduct.Id);
                if (localDto != null)
                {
                    // Local DB stok bilgisi stale olabilir — Mikro'dan gerçek zamanlı stok al
                    if (_mikroDbService.IsConfigured)
                    {
                        var unifiedLocal = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                        var mikroLocal = unifiedLocal.FirstOrDefault(u =>
                            string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));
                        if (mikroLocal != null)
                        {
                            localDto.StockQuantity = (int)Math.Max(0, mikroLocal.StokMiktar);
                            localDto.Unit = string.IsNullOrWhiteSpace(mikroLocal.Birim) ? "ADET" : mikroLocal.Birim.Trim().ToUpperInvariant();
                        }
                    }
                    return Ok(localDto);
                }
            }

            // Local DB'de yoksa Mikro'dan getir
            if (!_mikroDbService.IsConfigured) return NotFound();

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            var mikro = unified.FirstOrDefault(u =>
                string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));

            if (mikro == null) return NotFound();

            // Mikro verisinden ProductListDto inşa et (detay sayfası bu formatı kullanır)
            var slugToId = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var slugToName = await _dbContext.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Slug, c => c.Name, StringComparer.OrdinalIgnoreCase);

            var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);
            var catId = ResolveCategoryId(mikro.AnagrupKod, mikro.GrupKod, categoryMappings)
                ?? await GetFallbackCategoryIdAsync(HttpContext.RequestAborted);
            var categorySlug = slugToId.FirstOrDefault(item => item.Value == catId).Key;

            var dto = new ProductListDto
            {
                Id = 0,
                Sku = mikro.StokKod,
                Name = mikro.StokAd,
                Slug = GenerateSlug(mikro.StokAd),
                Description = string.Empty,
                Price = mikro.Fiyat,
                SpecialPrice = null,
                StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                ImageUrl = string.Empty,
                Unit = string.IsNullOrWhiteSpace(mikro.Birim) ? "ADET" : mikro.Birim.Trim().ToUpperInvariant(),
                CategoryId = catId,
                CategoryName = !string.IsNullOrWhiteSpace(categorySlug) ? slugToName.GetValueOrDefault(categorySlug, string.Empty) : string.Empty,
            };

            return Ok(dto);
        }

        /// <summary>
        /// Mikro ERP'den gelen (Id=0) ürünü local DB'ye kaydeder veya mevcutsa bulur.
        /// Sepete ekle öncesi frontend tarafından çağrılır — AllowAnonymous çünkü misafir sepeti destekler.
        /// </summary>
        [HttpPost("ensure-local/{sku}")]
        [AllowAnonymous]
        public async Task<IActionResult> EnsureLocalProduct(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest(new { message = "SKU gerekli." });
            sku = sku.Trim();

            // Önce local DB'de ara — zaten varsa direkt döndür
            var existing = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku, HttpContext.RequestAborted);

            if (existing != null)
                return Ok(new { id = existing.Id, sku = existing.SKU, name = existing.Name });

            // Mikro'dan getir
            if (!_mikroDbService.IsConfigured)
                return NotFound(new { message = "Mikro ERP bağlantısı yok." });

            var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
            var mikro = unified.FirstOrDefault(u =>
                string.Equals(u.StokKod, sku, StringComparison.OrdinalIgnoreCase));

            if (mikro == null)
                return NotFound(new { message = $"Ürün bulunamadı: {sku}" });

            // Kategori eşleme
            var categoryMappings = await LoadActiveCategoryMappingsAsync(HttpContext.RequestAborted);
            var categoryId = ResolveCategoryId(mikro.AnagrupKod, mikro.GrupKod, categoryMappings);

            if (!categoryId.HasValue && _autoCategoryMappingEngine != null)
            {
                categoryId = await _autoCategoryMappingEngine.ResolveOrCreateMappingAsync(
                    mikro.AnagrupKod ?? string.Empty,
                    mikro.GrupKod,
                    HttpContext.RequestAborted);
                CacheResolvedMapping(categoryMappings, mikro.AnagrupKod, categoryId.Value);
            }

            categoryId ??= await GetFallbackCategoryIdAsync(HttpContext.RequestAborted);

            // Slug çakışmasını önle — aynı isimli ürün varsa sonuna SKU ekle
            var baseSlug = GenerateSlug(mikro.StokAd);
            var slug = baseSlug;
            if (await _dbContext.Products.AnyAsync(p => p.Slug == slug))
                slug = $"{baseSlug}-{sku.ToLowerInvariant()}";

            var product = new Product
            {
                Name = mikro.StokAd,
                SKU = mikro.StokKod,
                Price = mikro.Fiyat,
                StockQuantity = (int)Math.Max(0, mikro.StokMiktar),
                CategoryId = categoryId.Value,
                Description = string.Empty,
                ImageUrl = string.Empty,
                IsActive = true,
                Slug = slug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[EnsureLocal] Mikro ürün local DB'ye eklendi: {Sku} → Id={Id}", sku, product.Id);

            return Ok(new { id = product.Id, sku = product.SKU, name = product.Name });
        }

        private async Task<Dictionary<string, List<MikroCategoryMapping>>> LoadActiveCategoryMappingsAsync(CancellationToken cancellationToken)
        {
            var mappings = await _dbContext.MikroCategoryMappings
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.Priority)
                .ThenBy(m => m.Id)
                .ToListAsync(cancellationToken);

            return mappings
                .GroupBy(m => m.MikroAnagrupKod, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static (int? CategoryId, string CategorySlug, string CategoryName) ResolveCategoryInfo(
            string? anagrupKod,
            string? grupKod,
            string? productName,
            Dictionary<string, List<MikroCategoryMapping>> mappings,
            Dictionary<int, string> idToSlug,
            Dictionary<string, string> slugToName)
        {
            var categoryId = ResolveCategoryId(anagrupKod, grupKod, mappings);
            if (!categoryId.HasValue)
            {
                return TryResolveCategoryInfoFromProductName(productName, idToSlug, slugToName);
            }

            if (!idToSlug.TryGetValue(categoryId.Value, out var slug))
                return (categoryId, string.Empty, string.Empty);

            var normalizedSlug = NormalizeCategorySlug(slug);
            if (string.Equals(normalizedSlug, UncategorizedCategorySlug, StringComparison.OrdinalIgnoreCase))
            {
                var fallback = TryResolveCategoryInfoFromProductName(productName, idToSlug, slugToName);
                if (fallback.CategoryId.HasValue)
                    return fallback;
            }

            var hintedCategory = TryResolveCategoryInfoFromProductName(productName, idToSlug, slugToName);
            if (hintedCategory.CategoryId.HasValue &&
                ShouldPreferHintedCategory(normalizedSlug, hintedCategory.CategorySlug))
            {
                return hintedCategory;
            }

            return (
                categoryId,
                normalizedSlug,
                slugToName.GetValueOrDefault(slug, slugToName.GetValueOrDefault(normalizedSlug, string.Empty)));
        }

        private static int? ResolveCategoryId(
            string? anagrupKod,
            string? grupKod,
            Dictionary<string, List<MikroCategoryMapping>> mappings)
        {
            if (!string.IsNullOrWhiteSpace(anagrupKod) &&
                mappings.TryGetValue(anagrupKod.Trim(), out var directMappings) &&
                directMappings.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(grupKod))
                {
                    var exactAltgrupMatch = directMappings.FirstOrDefault(mapping =>
                        !string.IsNullOrWhiteSpace(mapping.MikroAltgrupKod) &&
                        string.Equals(mapping.MikroAltgrupKod, grupKod.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (exactAltgrupMatch != null)
                        return exactAltgrupMatch.CategoryId;
                }

                var baseMapping = directMappings.FirstOrDefault(mapping =>
                    string.IsNullOrWhiteSpace(mapping.MikroAltgrupKod) &&
                    string.IsNullOrWhiteSpace(mapping.MikroMarkaKod));

                return (baseMapping ?? directMappings[0]).CategoryId;
            }

            if (!string.IsNullOrWhiteSpace(grupKod) &&
                mappings.TryGetValue(grupKod.Trim(), out var groupMappings) &&
                groupMappings.Count > 0)
            {
                return groupMappings[0].CategoryId;
            }

            if (mappings.TryGetValue("*", out var wildcardMappings) && wildcardMappings.Count > 0)
                return wildcardMappings[0].CategoryId;

            return null;
        }

        private static (int? CategoryId, string CategorySlug, string CategoryName) TryResolveCategoryInfoFromProductName(
            string? productName,
            Dictionary<int, string> idToSlug,
            Dictionary<string, string> slugToName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return (null, string.Empty, string.Empty);

            var normalizedName = NormalizeCategorySlug(productName)
                .Replace('-', ' ');

            foreach (var (slug, hints) in ProductNameCategoryHints)
            {
                if (!hints.Any(hint => normalizedName.Contains(NormalizeCategorySlug(hint).Replace('-', ' '), StringComparison.OrdinalIgnoreCase)))
                    continue;

                var categoryEntry = idToSlug.FirstOrDefault(item =>
                    string.Equals(item.Value, slug, StringComparison.OrdinalIgnoreCase));

                if (categoryEntry.Key <= 0)
                    continue;

                return (
                    categoryEntry.Key,
                    slug,
                    slugToName.GetValueOrDefault(slug, string.Empty));
            }

            return (null, string.Empty, string.Empty);
        }

        private static bool ShouldPreferHintedCategory(string resolvedSlug, string hintedSlug)
        {
            if (string.IsNullOrWhiteSpace(hintedSlug) ||
                string.Equals(resolvedSlug, hintedSlug, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(hintedSlug, FrozenStorefrontCategorySlug, StringComparison.OrdinalIgnoreCase) &&
                   ProductHintOverridableCategorySlugs.Contains(resolvedSlug);
        }

        private void CacheResolvedMapping(
            Dictionary<string, List<MikroCategoryMapping>> mappings,
            string? anagrupKod,
            int categoryId)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
                return;

            if (!mappings.TryGetValue(anagrupKod, out var existing))
            {
                existing = new List<MikroCategoryMapping>();
                mappings[anagrupKod] = existing;
            }

            if (existing.Any(mapping => mapping.CategoryId == categoryId))
                return;

            existing.Insert(0, new MikroCategoryMapping
            {
                MikroAnagrupKod = anagrupKod,
                CategoryId = categoryId,
                Priority = 10,
                IsActive = true
            });
        }

        private async Task<int> GetFallbackCategoryIdAsync(CancellationToken cancellationToken)
        {
            var wildcard = await _dbContext.MikroCategoryMappings
                .AsNoTracking()
                .Where(m => m.IsActive && m.MikroAnagrupKod == "*")
                .OrderByDescending(m => m.Priority)
                .Select(m => (int?)m.CategoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (wildcard.HasValue)
                return wildcard.Value;

            return await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Yeni ürün oluştur
        [HttpPost]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            
            try
            {
                var product = await _productService.CreateProductAsync(dto);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Ürün güncelle
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            
            try
            {
                var product = await _productService.UpdateProductAsync(id, dto);
                if (product == null) return NotFound();
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admin/duplicates")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetDuplicateProducts()
        {
            var products = await _dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.SKU,
                    p.Price,
                    p.SpecialPrice,
                    p.StockQuantity,
                    p.IsActive,
                    p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty
                })
                .ToListAsync(HttpContext.RequestAborted);

            var cacheLookup = await _dbContext.MikroProductCaches
                .AsNoTracking()
                .Where(c => c.LocalProductId.HasValue)
                .GroupBy(c => c.LocalProductId!.Value)
                .Select(g => g.OrderByDescending(item => item.GuncellemeTarihi).First())
                .ToDictionaryAsync(c => c.LocalProductId!.Value, c => c, HttpContext.RequestAborted);

            var candidates = products
                .Select(product =>
                {
                    cacheLookup.TryGetValue(product.Id, out var cache);
                    var barcode = cache?.Barkod ?? ExtractBarcodeFromDescription(product.Description);
                    return new DuplicateProductCandidate
                    {
                        Id = product.Id,
                        Name = product.Name ?? string.Empty,
                        Description = product.Description ?? string.Empty,
                        Sku = product.SKU ?? string.Empty,
                        Barcode = barcode ?? string.Empty,
                        Price = product.Price,
                        SpecialPrice = product.SpecialPrice,
                        StockQuantity = product.StockQuantity,
                        IsActive = product.IsActive,
                        CategoryId = product.CategoryId,
                        CategoryName = product.CategoryName ?? string.Empty,
                            ExactMatchKey = BuildDuplicateExactMatchKey(
                                product.Name,
                                product.Description,
                                product.SKU,
                                barcode,
                                product.Price,
                                product.SpecialPrice,
                                product.CategoryId)
                    };
                })
                .ToList();

            var groups = new List<DuplicateGroupResponse>();
            var fingerprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var exactGroup in candidates
                    .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ExactMatchKey))
                    .GroupBy(candidate => candidate.ExactMatchKey, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(item => item.Id).Distinct().Count() > 1)
                .OrderByDescending(group => group.Count()))
            {
                    AddDuplicateGroup(groups, fingerprints, $"exact:{exactGroup.Key}", "Tıpatıp aynı ürün bilgisi", exactGroup);
            }

            return Ok(new
            {
                totalGroups = groups.Count,
                groups
            });
        }

        // SKU bazlı ürün güncelle/oluştur — Mikro ERP ürünlerinde id=0 olduğundan SKU ile eşleştir
        [HttpPut("by-sku/{sku}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateProductBySku(string sku, [FromBody] ProductUpdateDto dto)
        {
            if (dto == null) return BadRequest("Ürün bilgileri gerekli.");
            if (string.IsNullOrWhiteSpace(sku)) return BadRequest(new { message = "SKU gerekli" });

            try
            {
                var product = await _productService.UpdateBySkuAsync(sku.Trim(), dto);
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Ürün sil
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (!result) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> DeactivateProduct(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçerli ürün ID gerekli." });

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Ürün bulunamadı." });

            if (!product.IsActive)
            {
                return Ok(new
                {
                    id = product.Id,
                    isActive = false,
                    message = "Ürün zaten pasif durumda."
                });
            }

            // NEDEN: Duplicate düzeltmesinde ürünü silmek sipariş ve audit geçmişini bozar.
            // Güvenli yaklaşım olarak yalnızca görünürlüğünü kapatıyoruz.
            product.IsActive = false;
            product.AdminDeactivated = true;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            return Ok(new
            {
                id = product.Id,
                isActive = false,
                message = "Ürün pasife alındı."
            });
        }

        // Stok güncelle
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateDto dto)
        {
            try
            {
                var result = await _productService.UpdateStockAsync(id, dto.Stock);
                if (!result) return NotFound();
                return Ok(new { message = "Stok güncellendi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Excel/CSV dosyasından toplu ürün yükleme + görsel yükleme
        // Desteklenen formatlar: .xlsx, .xls, .csv
        // Görsel URL sütunu: HTTP/HTTPS URL (otomatik indirilir), yüklenen dosya adı, veya mevcut /uploads/ yolu
        // Aynı anda birden fazla görsel dosyası gönderilebilir — CSV'deki dosya adıyla eşleştirilir
        [HttpPost("import/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        [RequestSizeLimit(200 * 1024 * 1024)] // 200MB — görseller dahil
        public async Task<IActionResult> ImportFromExcel(IFormFile file, [FromForm] IFormFileCollection? images = null)
        {
            _logger.LogInformation("📥 Excel import başlatılıyor — görsel sayısı: {ImageCount}", images?.Count ?? 0);

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return BadRequest(new { message = "Sadece Excel (.xlsx, .xls) veya CSV (.csv) dosyaları kabul edilir." });

            try
            {
                // Yüklenen görselleri dosya adına göre indexle (büyük/küçük harf duyarsız)
                // CSV'deki ImageUrl sütunundaki dosya adıyla eşleştirilmek için
                var uploadedImageMap = new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        if (img.Length > 0)
                            uploadedImageMap[Path.GetFileName(img.FileName)] = img;
                    }
                }

                var products = new List<ProductCreateDto>();
                const int maxProducts = 500;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (extension == ".csv")
                    {
                        // CSV işleme — UTF-8 + BOM desteği ile Türkçe karakter desteği
                        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        var headerLine = await reader.ReadLineAsync();
                        if (headerLine == null)
                            return BadRequest(new { message = "CSV dosyası boş." });

                        int lineNumber = 1;
                        while (!reader.EndOfStream && products.Count < maxProducts)
                        {
                            lineNumber++;
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Virgül içeren değerleri desteklemek için RFC 4180 parse
                            var values = ParseCsvLine(line);
                            if (values.Length < 5)
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Yetersiz sütun sayısı, atlanıyor", lineNumber);
                                continue;
                            }

                            var rawImageUrl = values.Length > 5 ? values[5].Trim().Trim('"') : null;
                            var imageUrl = await ResolveImageUrlAsync(rawImageUrl, uploadedImageMap);

                            var productDto = new ProductCreateDto
                            {
                                Name = values[0].Trim().Trim('"'),
                                Description = values.Length > 1 ? values[1].Trim().Trim('"') : "",
                                Price = decimal.TryParse(values[2].Trim().Replace(',', '.'),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var price) ? price : 0,
                                Stock = int.TryParse(values[3].Trim(), out var stock) ? stock : 0,
                                CategoryId = int.TryParse(values[4].Trim(), out var catId) ? catId : 1,
                                ImageUrl = imageUrl,
                                SpecialPrice = values.Length > 6 && decimal.TryParse(values[6].Trim().Replace(',', '.'),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var specialPrice) ? specialPrice : (decimal?)null
                            };

                            if (string.IsNullOrWhiteSpace(productDto.Name))
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Ürün adı boş, atlanıyor", lineNumber);
                                continue;
                            }
                            if (productDto.Price <= 0)
                            {
                                _logger.LogWarning("⚠️ Satır {Line}: Geçersiz fiyat ({Price}), atlanıyor", lineNumber, values[2]);
                                continue;
                            }

                            products.Add(productDto);
                        }
                    }
                    else
                    {
                        // Excel işleme (.xlsx, .xls)
                        using var workbook = new XLWorkbook(stream);
                        var worksheet = workbook.Worksheets.First();
                        var rows = worksheet.RowsUsed().Skip(1);

                        int rowNumber = 1;
                        foreach (var row in rows)
                        {
                            rowNumber++;
                            if (products.Count >= maxProducts) break;

                            var name = row.Cell(1).GetString()?.Trim();
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                _logger.LogWarning("⚠️ Excel satır {Row}: Ürün adı boş, atlanıyor", rowNumber);
                                continue;
                            }

                            if (!row.Cell(3).TryGetValue<decimal>(out var price) || price <= 0)
                            {
                                _logger.LogWarning("⚠️ Excel satır {Row}: Geçersiz fiyat, atlanıyor", rowNumber);
                                continue;
                            }

                            decimal? specialPrice = null;
                            if (row.Cell(7).TryGetValue<decimal>(out var sp) && sp > 0)
                                specialPrice = sp;

                            var rawImageUrl = row.Cell(6).GetString()?.Trim();
                            var imageUrl = await ResolveImageUrlAsync(rawImageUrl, uploadedImageMap);

                            products.Add(new ProductCreateDto
                            {
                                Name = name,
                                Description = row.Cell(2).GetString()?.Trim() ?? "",
                                Price = price,
                                Stock = row.Cell(4).TryGetValue<int>(out var stockVal) ? stockVal : 0,
                                CategoryId = row.Cell(5).TryGetValue<int>(out var catId) ? catId : 1,
                                ImageUrl = imageUrl,
                                SpecialPrice = specialPrice
                            });
                        }
                    }
                }

                if (products.Count == 0)
                    return BadRequest(new { message = "Dosyada geçerli ürün bulunamadı. Lütfen şablonu kontrol edin." });

                _logger.LogInformation("📋 {Count} ürün işlenecek", products.Count);

                var createdCount = 0;
                var errors = new List<string>();

                foreach (var productDto in products)
                {
                    try
                    {
                        await _productService.CreateProductAsync(productDto);
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"'{productDto.Name}': {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning("⚠️ Ürün oluşturulamadı: {Error}", errorMsg);
                    }
                }

                _logger.LogInformation("✅ Import tamamlandı: {Success}/{Total} ürün eklendi, {Images} görsel işlendi",
                    createdCount, products.Count, uploadedImageMap.Count);

                return Ok(new
                {
                    success = true,
                    message = $"{createdCount} ürün başarıyla eklendi.",
                    totalProcessed = products.Count,
                    successCount = createdCount,
                    errorCount = errors.Count,
                    imagesProcessed = uploadedImageMap.Count,
                    errors = errors.Take(20).ToList(),
                    warning = products.Count >= 500 ? "Maksimum 500 ürün limiti nedeniyle bazı satırlar atlanmış olabilir." : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excel import sırasında hata oluştu");
                return BadRequest(new { message = $"Dosya işlenirken hata oluştu: {ex.Message}" });
            }
        }

        /// <summary>
        /// CSV satırını RFC 4180'e göre ayrıştırır (tırnak içindeki virgülleri korur).
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    // Çift tırnak escape: "" → "
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Görsel URL'ini çözümler:
        /// 1) HTTP/HTTPS URL → görsel indirilir, /uploads/products/ altına kaydedilir
        /// 2) Yüklenen dosya adıyla eşleşirse → dosya kaydedilir, yol döndürülür
        /// 3) Sadece dosya adıysa → /uploads/products/{ad} yolu döndürülür
        /// 4) Zaten /uploads/ yoluyla başlıyorsa → olduğu gibi bırakılır
        /// </summary>
        private async Task<string?> ResolveImageUrlAsync(
            string? rawUrl,
            Dictionary<string, IFormFile> uploadedImages)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return null;

            // Durum 1: HTTP/HTTPS URL — görsel indir ve sisteme kaydet (SSRF korumalı)
            if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return await DownloadAndSaveImageAsync(rawUrl);
            }

            // Durum 2: Yüklenen görsel dosyalarından biriyle eşleşiyor
            var fileName = Path.GetFileName(rawUrl); // Tam yol verilmişse sadece dosya adını al
            if (!string.IsNullOrEmpty(fileName) && uploadedImages.TryGetValue(fileName, out var uploadedFile))
            {
                return await SaveUploadedImageAsync(uploadedFile);
            }

            // Durum 3: Zaten sistemdeki bir yol (/uploads/... ile başlıyor)
            if (rawUrl.StartsWith("/"))
                return rawUrl;

            // Durum 4: Sadece dosya adı — /uploads/products/ altında olduğu varsayılır
            return $"/uploads/products/{rawUrl}";
        }

        private static void AddDuplicateGroup(
            List<DuplicateGroupResponse> groups,
            HashSet<string> fingerprints,
            string keyPrefix,
            string reason,
            IEnumerable<DuplicateProductCandidate> source)
        {
            var products = source
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Name)
                .ToList();

            if (products.Count < 2)
                return;

            var idsFingerprint = string.Join("-", products.Select(item => item.Id).OrderBy(id => id));
            var fingerprint = $"{reason}:{idsFingerprint}";
            if (!fingerprints.Add(fingerprint))
                return;

            groups.Add(new DuplicateGroupResponse
            {
                GroupKey = keyPrefix,
                Reason = reason,
                Products = products
                    .Select(item => (object)new
                    {
                        id = item.Id,
                        name = item.Name,
                        description = item.Description,
                        sku = item.Sku,
                        barcode = item.Barcode,
                        price = item.Price,
                        specialPrice = item.SpecialPrice,
                        stockQuantity = item.StockQuantity,
                        isActive = item.IsActive,
                        categoryId = item.CategoryId,
                        categoryName = item.CategoryName
                    })
                    .ToList()
            });
        }

        private static string BuildDuplicateExactMatchKey(
            string? name,
            string? description,
            string? sku,
            string? barcode,
            decimal price,
            decimal? specialPrice,
            int categoryId)
        {
            var normalizedName = NormalizeDuplicateText(name);
            var normalizedDescription = NormalizeDuplicateText(description);
            var normalizedSku = NormalizeDuplicateText(sku);
            var normalizedBarcode = NormalizeDuplicateText(barcode);

            if (string.IsNullOrWhiteSpace(normalizedName) &&
                string.IsNullOrWhiteSpace(normalizedDescription) &&
                string.IsNullOrWhiteSpace(normalizedSku) &&
                string.IsNullOrWhiteSpace(normalizedBarcode))
            {
                return string.Empty;
            }

            return string.Join("|", new[]
            {
                normalizedName,
                normalizedDescription,
                normalizedSku,
                normalizedBarcode,
                price.ToString("0.####", CultureInfo.InvariantCulture),
                specialPrice?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty,
                categoryId.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string NormalizeDuplicateText(string? value)
        {
            return string.Join(
                " ",
                (value ?? string.Empty)
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private Task<ProductAdminOverrideSettingsDto> GetProductOverrideDefaultsAsync(CancellationToken cancellationToken)
        {
            return _productAdminOverrideSettingsService.GetSettingsAsync(cancellationToken);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("userId")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? "Admin";
        }

        private static string? ExtractBarcodeFromDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            const string marker = "[Barkod:";
            var startIndex = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
                return null;

            var valueStart = startIndex + marker.Length;
            var endIndex = description.IndexOf(']', valueStart);
            if (endIndex <= valueStart)
                return null;

            return description[valueStart..endIndex].Trim();
        }

        /// <summary>
        /// Harici URL'den görsel indirir, MIME tipi doğrular ve sisteme kaydeder.
        /// SSRF koruması: özel IP aralıkları engellenir.
        /// </summary>
        private async Task<string?> DownloadAndSaveImageAsync(string url)
        {
            try
            {
                // SSRF koruması: URI parse et ve özel ağ adreslerini engelle
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("⚠️ Geçersiz görsel URL'i: {Url}", url);
                    return null;
                }

                if (IsPrivateOrLocalUri(uri))
                {
                    _logger.LogWarning("⚠️ Güvenlik: İç ağ adresine görsel isteği engellendi: {Host}", uri.Host);
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient("ImageDownload");
                using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ Görsel indirilemedi ({Status}): {Url}", (int)response.StatusCode, url);
                    return null;
                }

                // Content-Type doğrulaması — sadece bilinen resim formatları
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                if (!AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                {
                    // Content-Type güvenilmez olabilir — uzantıdan da dene
                    var ext = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("⚠️ Desteklenmeyen görsel tipi ({Type}): {Url}", contentType, url);
                        return null;
                    }
                    // Uzantı geçerliyse devam et, content type olarak text/plain gibi değerler olabilir
                    contentType = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".webp" => "image/webp",
                        ".bmp" => "image/bmp",
                        ".tiff" or ".tif" => "image/tiff",
                        ".avif" => "image/avif",
                        ".svg" => "image/svg+xml",
                        _ => contentType
                    };
                }

                // Boyut kontrolü (max 15MB)
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > MaxImportImageSize)
                {
                    _logger.LogWarning("⚠️ Görsel çok büyük ({Size}MB): {Url}",
                        contentLength.Value / 1024 / 1024, url);
                    return null;
                }

                await using var imageStream = await response.Content.ReadAsStreamAsync();

                // Dosya adını URL'den türet
                var originalName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(originalName) || originalName == "/")
                    originalName = "product";

                var ext2 = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext2) || !AllowedExtensions.Contains(ext2))
                    ext2 = ".jpg";

                var savedUrl = await _fileStorage.UploadAsync(imageStream, $"{originalName}{ext2}", contentType);
                _logger.LogInformation("✅ Görsel indirildi ve kaydedildi: {Url} → {SavedPath}", url, savedUrl);
                return savedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Görsel indirme hatası ({Url}): {Error}", url, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Yüklenen IFormFile görselini kaydeder.
        /// </summary>
        private async Task<string?> SaveUploadedImageAsync(IFormFile imageFile)
        {
            try
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("⚠️ Desteklenmeyen görsel uzantısı: {Ext}", ext);
                    return null;
                }

                if (imageFile.Length > MaxImportImageSize)
                {
                    _logger.LogWarning("⚠️ Görsel çok büyük: {Name} ({Size}MB)",
                        imageFile.FileName, imageFile.Length / 1024 / 1024);
                    return null;
                }

                // İçerik tipi: form gönderiminden gelen veya uzantıdan tahmin edilen
                var contentType = imageFile.ContentType ?? "image/jpeg";

                await using var stream = imageFile.OpenReadStream();
                var savedUrl = await _fileStorage.UploadAsync(stream, imageFile.FileName, contentType);
                _logger.LogInformation("✅ Görsel kaydedildi: {Name} → {Path}", imageFile.FileName, savedUrl);
                return savedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Görsel kaydetme hatası ({Name}): {Error}", imageFile.FileName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// SSRF koruması: URI'nin özel/yerel ağ adresine işaret edip etmediğini kontrol eder.
        /// </summary>
        private static bool IsPrivateOrLocalUri(Uri uri)
        {
            var host = uri.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!System.Net.IPAddress.TryParse(host, out var ip))
                return false; // DNS adı — genel kabul

            var bytes = ip.GetAddressBytes();
            // IPv4 kontrolü
            if (bytes.Length == 4)
            {
                return bytes[0] == 10                                   // 10.x.x.x
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)  // 172.16-31.x.x
                    || (bytes[0] == 192 && bytes[1] == 168)              // 192.168.x.x
                    || bytes[0] == 127                                   // 127.x.x.x loopback
                    || bytes[0] == 169 && bytes[1] == 254;               // 169.254.x.x link-local
            }
            // IPv6 loopback (::1)
            if (bytes.Length == 16)
            {
                return ip.Equals(System.Net.IPAddress.IPv6Loopback)
                    || ip.IsIPv6LinkLocal
                    || ip.IsIPv6SiteLocal;
            }
            return false;
        }

        // Excel şablonu indir
        // Türkçe karakterli örnek veriler ve detaylı açıklamalar içerir
        [HttpGet("import/template")]
        [Authorize(Roles = Roles.AdminLike)]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            
            // Ana şablon sayfası
            var worksheet = workbook.Worksheets.Add("Ürünler");

            // Başlıklar - 1. satır
            worksheet.Cell(1, 1).Value = "Ürün Adı *";
            worksheet.Cell(1, 2).Value = "Açıklama";
            worksheet.Cell(1, 3).Value = "Fiyat (TL) *";
            worksheet.Cell(1, 4).Value = "Stok Adedi *";
            worksheet.Cell(1, 5).Value = "Kategori ID *";
            worksheet.Cell(1, 6).Value = "Görsel URL";
            worksheet.Cell(1, 7).Value = "İndirimli Fiyat (TL)";
            worksheet.Cell(1, 8).Value = "Ağırlık (gram)";

            // Başlık stilini ayarla - Koyu mavi arkaplan, beyaz yazı
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Zorunlu alan başlıklarını kırmızı yap
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.DarkRed;
            worksheet.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.DarkRed;

            // Türkçe karakterli örnek veriler - 2-6. satırlar
            // Örnek 1: Dana Kuşbaşı
            worksheet.Cell(2, 1).Value = "Dana Kuşbaşı 500gr";
            worksheet.Cell(2, 2).Value = "Taze kesim dana kuşbaşı eti, günlük taze";
            worksheet.Cell(2, 3).Value = 289.90;
            worksheet.Cell(2, 4).Value = 50;
            worksheet.Cell(2, 5).Value = 1;
            worksheet.Cell(2, 6).Value = "/uploads/products/dana-kusbaşı.jpg";
            worksheet.Cell(2, 7).Value = 259.90;
            worksheet.Cell(2, 8).Value = 500;

            // Örnek 2: Şekerpare
            worksheet.Cell(3, 1).Value = "Şekerpare 1kg";
            worksheet.Cell(3, 2).Value = "Geleneksel Türk tatlısı, taze üretim";
            worksheet.Cell(3, 3).Value = 149.90;
            worksheet.Cell(3, 4).Value = 30;
            worksheet.Cell(3, 5).Value = 2;
            worksheet.Cell(3, 6).Value = "/uploads/products/sekerpare.jpg";
            worksheet.Cell(3, 7).Value = "";
            worksheet.Cell(3, 8).Value = 1000;

            // Örnek 3: Çökelek Peyniri
            worksheet.Cell(4, 1).Value = "Çökelek Peyniri 250gr";
            worksheet.Cell(4, 2).Value = "Köy tipi doğal çökelek, katkısız";
            worksheet.Cell(4, 3).Value = 59.90;
            worksheet.Cell(4, 4).Value = 100;
            worksheet.Cell(4, 5).Value = 3;
            worksheet.Cell(4, 6).Value = "";
            worksheet.Cell(4, 7).Value = "";
            worksheet.Cell(4, 8).Value = 250;

            // Örnek 4: Tütsülenmiş Sığır Pastırması
            worksheet.Cell(5, 1).Value = "Tütsülenmiş Sığır Pastırması 200gr";
            worksheet.Cell(5, 2).Value = "Özel baharatlarla hazırlanmış pastırma";
            worksheet.Cell(5, 3).Value = 189.90;
            worksheet.Cell(5, 4).Value = 25;
            worksheet.Cell(5, 5).Value = 1;
            worksheet.Cell(5, 6).Value = "/uploads/products/pastirma.jpg";
            worksheet.Cell(5, 7).Value = 169.90;
            worksheet.Cell(5, 8).Value = 200;

            // Örnek 5: Kaşar Peyniri
            worksheet.Cell(6, 1).Value = "Şek Kaşar Peyniri 500gr";
            worksheet.Cell(6, 2).Value = "Taze inek sütünden üretilmiş kaşar";
            worksheet.Cell(6, 3).Value = 129.90;
            worksheet.Cell(6, 4).Value = 75;
            worksheet.Cell(6, 5).Value = 3;
            worksheet.Cell(6, 6).Value = "";
            worksheet.Cell(6, 7).Value = "";
            worksheet.Cell(6, 8).Value = 500;

            // Sütun genişliklerini ayarla
            worksheet.Column(1).Width = 35; // Ürün Adı
            worksheet.Column(2).Width = 50; // Açıklama
            worksheet.Column(3).Width = 15; // Fiyat
            worksheet.Column(4).Width = 15; // Stok
            worksheet.Column(5).Width = 15; // Kategori ID
            worksheet.Column(6).Width = 40; // Görsel URL
            worksheet.Column(7).Width = 20; // İndirimli Fiyat
            worksheet.Column(8).Width = 15; // Ağırlık

            // Açıklamalar sayfası
            var helpSheet = workbook.Worksheets.Add("Açıklamalar");
            helpSheet.Cell(1, 1).Value = "ALAN AÇIKLAMALARI";
            helpSheet.Cell(1, 1).Style.Font.Bold = true;
            helpSheet.Cell(1, 1).Style.Font.FontSize = 14;

            helpSheet.Cell(3, 1).Value = "Alan Adı";
            helpSheet.Cell(3, 2).Value = "Zorunlu";
            helpSheet.Cell(3, 3).Value = "Açıklama";
            helpSheet.Cell(3, 4).Value = "Örnek Değer";
            
            var helpHeaderRow = helpSheet.Row(3);
            helpHeaderRow.Style.Font.Bold = true;
            helpHeaderRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Alan açıklamaları
            helpSheet.Cell(4, 1).Value = "Ürün Adı";
            helpSheet.Cell(4, 2).Value = "EVET";
            helpSheet.Cell(4, 3).Value = "Ürünün tam adı (max 200 karakter). Türkçe karakterler desteklenir.";
            helpSheet.Cell(4, 4).Value = "Dana Kuşbaşı 500gr";

            helpSheet.Cell(5, 1).Value = "Açıklama";
            helpSheet.Cell(5, 2).Value = "HAYIR";
            helpSheet.Cell(5, 3).Value = "Ürün açıklaması (max 1000 karakter)";
            helpSheet.Cell(5, 4).Value = "Taze kesim dana eti";

            helpSheet.Cell(6, 1).Value = "Fiyat (TL)";
            helpSheet.Cell(6, 2).Value = "EVET";
            helpSheet.Cell(6, 3).Value = "Normal satış fiyatı (ondalıklı sayı, örn: 99.90)";
            helpSheet.Cell(6, 4).Value = "289.90";

            helpSheet.Cell(7, 1).Value = "Stok Adedi";
            helpSheet.Cell(7, 2).Value = "EVET";
            helpSheet.Cell(7, 3).Value = "Mevcut stok miktarı (tam sayı)";
            helpSheet.Cell(7, 4).Value = "50";

            helpSheet.Cell(8, 1).Value = "Kategori ID";
            helpSheet.Cell(8, 2).Value = "EVET";
            helpSheet.Cell(8, 3).Value = "Ürünün ait olduğu kategori numarası";
            helpSheet.Cell(8, 4).Value = "1";

            helpSheet.Cell(9, 1).Value = "Görsel URL";
            helpSheet.Cell(9, 2).Value = "HAYIR";
            helpSheet.Cell(9, 3).Value = "Görsel yolu. 3 seçenek: 1) HTTP/HTTPS URL (otomatik indirilir), 2) Yüklenen görsel dosyasının adı (örn: urun.jpg), 3) /uploads/products/... yolu";
            helpSheet.Cell(9, 4).Value = "https://site.com/urun.jpg  VEYA  urun.jpg  VEYA  /uploads/products/urun.jpg";

            helpSheet.Cell(10, 1).Value = "İndirimli Fiyat";
            helpSheet.Cell(10, 2).Value = "HAYIR";
            helpSheet.Cell(10, 3).Value = "Varsa indirimli fiyat (boş = indirim yok)";
            helpSheet.Cell(10, 4).Value = "259.90";

            helpSheet.Cell(11, 1).Value = "Ağırlık (gram)";
            helpSheet.Cell(11, 2).Value = "HAYIR";
            helpSheet.Cell(11, 3).Value = "Ürün ağırlığı gram cinsinden";
            helpSheet.Cell(11, 4).Value = "500";

            // Önemli notlar
            helpSheet.Cell(13, 1).Value = "ÖNEMLİ NOTLAR:";
            helpSheet.Cell(13, 1).Style.Font.Bold = true;
            helpSheet.Cell(14, 1).Value = "• Zorunlu alanlar (*) mutlaka doldurulmalıdır";
            helpSheet.Cell(15, 1).Value = "• Türkçe karakterler (ğ, ü, ş, ö, ç, ı, İ) desteklenir";
            helpSheet.Cell(16, 1).Value = "• Fiyatlar için nokta (.) kullanın, virgül (,) değil";
            helpSheet.Cell(17, 1).Value = "• İlk satır başlık satırıdır, silmeyin";
            helpSheet.Cell(18, 1).Value = "• Maksimum 500 ürün yüklenebilir";
            helpSheet.Cell(19, 1).Value = "• GÖRSEL SEÇENEKLERİ:";
            helpSheet.Cell(19, 1).Style.Font.Bold = true;
            helpSheet.Cell(20, 1).Value = "  1) HTTP/HTTPS URL yazın → görsel otomatik indirilir (örn: https://example.com/resim.jpg)";
            helpSheet.Cell(21, 1).Value = "  2) Görsel dosya adını yazın (örn: kasar.jpg) → aynı anda görsel dosyalarını seçerek yükleyin";
            helpSheet.Cell(22, 1).Value = "  3) /uploads/products/... yolu yazın → mevcut sistemdeki yolu kullanır";
            helpSheet.Cell(23, 1).Value = "• Desteklenen görsel formatları: JPG, JPEG, PNG, GIF, WEBP, BMP, TIFF, AVIF, SVG";

            helpSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "urun_sablonu.xlsx");
        }

        /// <summary>
        /// Mevcut ürünleri Excel dosyası olarak dışa aktarır.
        /// Admin panelinden tüm ürünleri indirmek için kullanılır.
        /// Türkçe karakterler UTF-8 encoding ile doğru şekilde kaydedilir.
        /// </summary>
        [HttpGet("export/excel")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                _logger.LogInformation("📥 Ürün Excel export işlemi başlatılıyor");

                // Mikro ERP bağlıysa: sadece web aktif ürünleri (WebeGonderilecekFl=true) al
                IEnumerable<object> exportRows;
                if (_mikroDbService.IsConfigured)
                {
                    var unified = await _mikroDbService.GetUnifiedProductsAsync(null, null, HttpContext.RequestAborted);
                    var localAll = (await _productRepository.GetAllAsync()).ToList();
                    var skuToLocal = localAll
                        .Where(p => !string.IsNullOrEmpty(p.SKU))
                        .GroupBy(p => p.SKU!)
                        .ToDictionary(g => g.Key, g => g.First());

                    exportRows = unified
                        .Where(p => p.WebeGonderilecekFl)  // sadece web aktif ürünler
                        .OrderBy(p => p.StokKod)
                        .Select(p =>
                        {
                            var hasLocal = skuToLocal.TryGetValue(p.StokKod, out var local);
                            return (object)new
                            {
                                Id    = hasLocal ? local!.Id : 0,
                                Name  = hasLocal && !string.IsNullOrEmpty(local!.Name) ? local.Name : p.StokAd,
                                Description = hasLocal && !string.IsNullOrEmpty(local!.Description) ? local.Description : "",
                                // Fiyat: Mikro ERP esas kaynak — yerel DB sadece Mikro 0 ise fallback
                                Price = p.Fiyat > 0 ? p.Fiyat : (hasLocal ? local!.Price : 0m),
                                SpecialPrice = hasLocal ? local!.SpecialPrice : (decimal?)null,
                                StockQuantity = (int)Math.Max(0, p.StokMiktar),
                                CategoryId    = hasLocal && local!.CategoryId > 0 ? (int?)local.CategoryId : (int?)null,
                                CategoryName  = (string?)null,
                                ImageUrl      = hasLocal && !string.IsNullOrEmpty(local!.ImageUrl) ? local.ImageUrl : "",
                                Sku           = p.StokKod,
                                IsActive      = p.WebeGonderilecekFl
                            };
                        })
                        .ToList();
                }
                else
                {
                    var dbProducts = await _productService.GetActiveProductsAsync(1, 50000);
                    exportRows = dbProducts.Select(p => (object)new
                    {
                        p.Id, p.Name, p.Description, p.Price, p.SpecialPrice,
                        p.StockQuantity, p.CategoryId, p.CategoryName, p.ImageUrl,
                        Sku = "", IsActive = true
                    }).ToList();
                }

                var productList = exportRows.ToList();
                
                if (productList == null || !productList.Any())
                {
                    _logger.LogWarning("⚠️ Export için ürün bulunamadı");
                    return NotFound(new { message = "Dışa aktarılacak ürün bulunamadı." });
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Ürünler");

                // Başlıklar
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Ürün Adı";
                worksheet.Cell(1, 3).Value = "Açıklama";
                worksheet.Cell(1, 4).Value = "Fiyat (TL)";
                worksheet.Cell(1, 5).Value = "İndirimli Fiyat (TL)";
                worksheet.Cell(1, 6).Value = "Stok Adedi";
                worksheet.Cell(1, 7).Value = "Kategori ID";
                worksheet.Cell(1, 8).Value = "Kategori Adı";
                worksheet.Cell(1, 9).Value = "Görsel URL";
                worksheet.Cell(1, 10).Value = "SKU";
                worksheet.Cell(1, 11).Value = "Ağırlık (gram)";
                worksheet.Cell(1, 12).Value = "Aktif";
                worksheet.Cell(1, 13).Value = "Oluşturma Tarihi";

                // Başlık stili
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Ürünleri yaz — dynamic ile exportRows'dan alan oku
                int row = 2;
                foreach (dynamic product in productList)
                {
                    worksheet.Cell(row, 1).Value  = (int)(product.Id ?? 0);
                    worksheet.Cell(row, 2).Value  = (string)(product.Name ?? "");
                    worksheet.Cell(row, 3).Value  = (string)(product.Description ?? "");
                    worksheet.Cell(row, 4).Value  = (decimal)(product.Price ?? 0m);
                    worksheet.Cell(row, 5).Value  = (decimal)(product.SpecialPrice ?? 0m);
                    worksheet.Cell(row, 6).Value  = (int)(product.StockQuantity ?? 0);
                    worksheet.Cell(row, 7).Value  = (int)(product.CategoryId ?? 0);
                    worksheet.Cell(row, 8).Value  = (string)(product.CategoryName ?? "");
                    worksheet.Cell(row, 9).Value  = (string)(product.ImageUrl ?? "");
                    worksheet.Cell(row, 10).Value = (string)(product.Sku ?? "");
                    worksheet.Cell(row, 11).Value = 0;
                    worksheet.Cell(row, 12).Value = (bool)(product.IsActive ?? true) ? "Evet" : "Hayır";
                    worksheet.Cell(row, 13).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    row++;
                }

                // Sütun genişliklerini ayarla
                worksheet.Column(1).Width = 8;   // ID
                worksheet.Column(2).Width = 40;  // Ürün Adı
                worksheet.Column(3).Width = 50;  // Açıklama
                worksheet.Column(4).Width = 15;  // Fiyat
                worksheet.Column(5).Width = 18;  // İndirimli Fiyat
                worksheet.Column(6).Width = 12;  // Stok
                worksheet.Column(7).Width = 12;  // Kategori ID
                worksheet.Column(8).Width = 20;  // Kategori Adı
                worksheet.Column(9).Width = 45;  // Görsel URL
                worksheet.Column(10).Width = 15; // SKU
                worksheet.Column(11).Width = 15; // Ağırlık
                worksheet.Column(12).Width = 10; // Aktif
                worksheet.Column(13).Width = 18; // Oluşturma Tarihi

                // Alternatif satır renklendirme (okunabilirlik için)
                for (int i = 2; i <= row - 1; i++)
                {
                    if (i % 2 == 0)
                    {
                        worksheet.Row(i).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }

                // Dosya adı: urunler_YYYYMMDD_HHMMSS.xlsx
                var fileName = $"urunler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("✅ Excel export tamamlandı. {Count} ürün dışa aktarıldı", row - 2);

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excel export sırasında hata oluştu");
                return StatusCode(500, new { message = "Ürünler dışa aktarılırken bir hata oluştu." });
            }
        }

        [HttpPost("{id}/review")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] ProductReviewCreateDto reviewDto)
        {
            if (reviewDto == null) return BadRequest("Review body is required.");

            reviewDto.ProductId = id;
            var userId = User.GetUserId();

            try
            {
                await _productService.AddProductReviewAsync(id, userId, reviewDto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> AddToFavorite(int id)
        {
            await _productService.AddFavoriteAsync(User.GetUserId(), id);
            return Ok();
        }

        /// <summary>
        /// Ürün resmi yükler (multipart/form-data)
        /// Bilgisayardan resim dosyası seçilerek uploads/products klasörüne kaydedilir.
        /// </summary>
        /// <param name="image">Yüklenecek resim dosyası (jpg, jpeg, png, gif, webp)</param>
        /// <returns>Yüklenen dosyanın URL'ini döner</returns>
        [HttpPost("upload/image")]
        [Authorize(Roles = Roles.AdminLike)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            _logger.LogInformation("📤 Ürün resmi yükleme başlatılıyor");
            
            try
            {
                // Dosya var mı kontrolü
                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("⚠️ Dosya seçilmedi");
                    return BadRequest(new { message = "Lütfen bir resim dosyası seçin." });
                }

                // Dosya boyutu kontrolü
                if (image.Length > MaxFileSize)
                {
                    _logger.LogWarning("⚠️ Dosya çok büyük: {Size}MB", image.Length / (1024 * 1024));
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir." });
                }

                // Dosya uzantısı kontrolü (whitelist yaklaşımı)
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("⚠️ Geçersiz dosya uzantısı: {Extension}", extension);
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                }

                // MIME type kontrolü (güvenlik için ek katman)
                var mimeType = image.ContentType.ToLowerInvariant();
                if (!AllowedMimeTypes.Contains(mimeType))
                {
                    _logger.LogWarning("⚠️ Geçersiz MIME type: {MimeType}", mimeType);
                    return BadRequest(new { message = "Geçersiz dosya türü. Sadece resim dosyaları kabul edilir." });
                }

                // Dosyayı LocalFileStorage üzerinden yükle
                // Dosya adı: product_{timestamp}_{guid}.{ext} formatında oluşturulur
                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    var fileName = $"product_{image.FileName}";
                    imageUrl = await _fileStorage.UploadAsync(stream, fileName, image.ContentType);
                }

                _logger.LogInformation("✅ Ürün resmi yüklendi: {ImageUrl}", imageUrl);

                // Başarılı yanıt - yüklenen dosyanın URL'ini döndür
                return Ok(new { 
                    success = true,
                    imageUrl = imageUrl,
                    message = "Resim başarıyla yüklendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ürün resmi yüklenirken hata oluştu");
                return StatusCode(500, new { message = "Resim yüklenirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }
    }

    // DTO sınıfları
    public class StockUpdateDto
    {
        public int Stock { get; set; }
    }
}
