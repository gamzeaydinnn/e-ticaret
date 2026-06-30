using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IProductService
using ECommerce.Entities.Concrete;            // Product
using ECommerce.Core.Interfaces;              // IProductRepository
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.ProductReview;            // Product DTO
using ECommerce.Data.Context;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;

namespace ECommerce.Business.Services.Managers
{
    public class ProductManager : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMemoryCache _cache;
        private readonly IInventoryLogService _inventoryLogService;
        private readonly ECommerceDbContext? _dbContext;
        private readonly IOrderLimitResolver? _orderLimitResolver;
        private readonly IProductOrderLimitSettingsService? _limitSettingsService;
        private ProductOrderLimitSettingsDto? _cachedLimitSettings;
        private const string ProductCacheKeysKey = "products_cache_keys";

        public ProductManager(
            IProductRepository productRepository,
            IReviewRepository reviewRepository,
            IMemoryCache cache,
            IInventoryLogService inventoryLogService,
            ECommerceDbContext? dbContext = null,
            IOrderLimitResolver? orderLimitResolver = null,
            IProductOrderLimitSettingsService? limitSettingsService = null)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _cache = cache;
            _inventoryLogService = inventoryLogService;
            _dbContext = dbContext;
            _orderLimitResolver = orderLimitResolver;
            _limitSettingsService = limitSettingsService;
        }

        // Backwards-compatible constructor for tests or callers that don't provide an IMemoryCache.
        // This avoids breaking existing unit tests that construct ProductManager without cache.
        public ProductManager(
            IProductRepository productRepository,
            IReviewRepository reviewRepository,
            IInventoryLogService inventoryLogService)
            : this(productRepository, reviewRepository, new MemoryCache(new MemoryCacheOptions()), inventoryLogService, null)
        {
        }

    public async Task<IEnumerable<ProductListDto>> SearchProductsAsync(string? query, int page = 1, int size = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<ProductListDto>();

            var allProducts = await _productRepository.GetAllAsync();

            var filteredProducts = allProducts.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var pagedProducts = filteredProducts
                .OrderBy(p => p.Name)
                .Skip((page - 1) * size)
                .Take(size);

            return pagedProducts.Select(p => new ProductListDto
            {
                Id = p.Id,
                Sku = p.SKU,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                AdminOverrideName = p.AdminOverrideName,
                AdminOverridePrice = p.AdminOverridePrice,
                AdminOverrideCategory = p.AdminOverrideCategory
            });
        }

    public async Task<IEnumerable<ProductListDto>> GetProductsAsync(string? query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            var products = await _productRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p =>
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            products = products.OrderBy(p => p.Name)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize);

            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Sku = p.SKU,
                Name = p.Name,
                Slug = p.Slug ?? string.Empty,
                Description = p.Description ?? string.Empty,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                AdminOverrideName = p.AdminOverrideName,
                AdminOverridePrice = p.AdminOverridePrice,
                AdminOverrideCategory = p.AdminOverrideCategory
            });
        }

        public async Task<ProductListDto?> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            var dto = MapToDto(product);
            await EnrichWithOrderLimitsAsync(dto, product);
            return dto;
        }

        public async Task<ProductListDto> CreateProductAsync(ProductCreateDto productDto)
        {
            // SKU otomatik oluştur: CategoryId + timestamp + random
            var sku = $"PRD{productDto.CategoryId:D2}{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
            var resolvedSku = string.IsNullOrWhiteSpace(productDto.SKU) ? sku : productDto.SKU.Trim();

            var imageUrls = productDto.ImageUrls != null && productDto.ImageUrls.Count > 0
                ? BuildImageUrlList(null, productDto.ImageUrls)
                : BuildImageUrlList(productDto.ImageUrl, productDto.AdditionalImageUrls);
            
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                SpecialPrice = productDto.SpecialPrice,
                StockQuantity = productDto.StockQuantity,
                CategoryId = productDto.CategoryId,
                ImageUrl = imageUrls.FirstOrDefault() ?? string.Empty,
                BrandId = productDto.BrandId,
                AdminOverrideName = productDto.AdminOverrideName,
                AdminOverridePrice = productDto.AdminOverridePrice,
                // Admin panelinden kaydedilen kategori, sonraki Mikro sync'lerinde ezilmemelidir.
                AdminOverrideCategory = true,
                SKU = resolvedSku,
                Slug = await GenerateUniqueProductSlugAsync(productDto.Name, resolvedSku),
                MaxOrderQuantity = productDto.MaxOrderQuantity,
                MinOrderQuantity = productDto.MinOrderQuantity,
                QuantityStep = productDto.QuantityStep,
                IsWeightBased = productDto.IsWeightBased,
                MinOrderWeight = productDto.MinOrderWeight,
                MaxOrderWeight = productDto.MaxOrderWeight
            };

            await _productRepository.AddAsync(product);
            await SyncProductImagesAsync(product.Id, imageUrls);
            // Invalidate product-related caches so subsequent reads reflect the new product
            InvalidateProductCaches();

            var saved = await _productRepository.GetByIdAsync(product.Id);
            return MapToDto(saved ?? product);
        }

        public async Task<ProductListDto?> UpdateProductAsync(int id, ProductUpdateDto productDto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            var oldStock = product.StockQuantity;
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.SpecialPrice = productDto.SpecialPrice;
            product.StockQuantity = productDto.StockQuantity;
            product.CategoryId = productDto.CategoryId;
            product.BrandId = productDto.BrandId;
            product.AdminOverrideName = ResolveAdminOverride(product.AdminOverrideName, productDto.AdminOverrideName);
            product.AdminOverridePrice = ResolveAdminOverride(product.AdminOverridePrice, productDto.AdminOverridePrice);
            // Admin'in seçtiği kategori son karar olmalı; sync bunu tekrar yazmamalı.
            product.AdminOverrideCategory = true;
            product.Slug = await EnsureProductSlugAsync(product, productDto.Name, product.SKU);

            if (productDto.MaxOrderQuantity.HasValue)
                product.MaxOrderQuantity = Math.Max(0, productDto.MaxOrderQuantity.Value);
            if (productDto.MinOrderQuantity.HasValue)
                product.MinOrderQuantity = Math.Max(0, productDto.MinOrderQuantity.Value);
            if (productDto.QuantityStep.HasValue)
                product.QuantityStep = Math.Max(0m, productDto.QuantityStep.Value);
            if (productDto.MinOrderWeight.HasValue)
                product.MinOrderWeight = Math.Max(0m, productDto.MinOrderWeight.Value);
            if (productDto.MaxOrderWeight.HasValue)
                product.MaxOrderWeight = Math.Max(0m, productDto.MaxOrderWeight.Value);

            if (!string.IsNullOrWhiteSpace(productDto.ImageUrl))
            {
                product.ImageUrl = productDto.ImageUrl.Trim();
            }

            await _productRepository.UpdateAsync(product);

            await ApplyProductImageUpdatesAsync(product.Id, productDto);

            if (oldStock != product.StockQuantity)
            {
                var quantity = Math.Abs(product.StockQuantity - oldStock);
                await _inventoryLogService.WriteAsync(
                    product.Id,
                    "ProductUpdated",
                    quantity,
                    oldStock,
                    product.StockQuantity,
                    $"Product:{product.Id}");
            }
            InvalidateProductCaches();
            
            var saved = await _productRepository.GetByIdAsync(product.Id);
            return MapToDto(saved ?? product);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null) 
                {
                    // Ürün zaten yok, başarılı sayalım
                    return true;
                }
                
                await _productRepository.DeleteAsync(product);
                InvalidateProductCaches();
                return true;
            }
            catch (Exception ex)
            {
                // Hata olsa bile cache'i temizle ve başarılı dön
                // Mikrodan tekrar çekilince ürün geri gelir
                InvalidateProductCaches();
                
                // Log yazılabilir
                Console.WriteLine($"[ProductManager] Ürün silme hatası (ID: {id}): {ex.Message}");
                
                // Yine de başarılı dön - kullanıcı için önemli olan ürünün gözükmemesi
                return true;
            }
        }

        /// <summary>
        /// SKU bazlı upsert: yerel DB'de SKU varsa günceller, yoksa yeni ürün oluşturur.
        /// Mikro ERP ürünlerinde local id=0 olduğundan SKU üzerinden eşleştirme yapılır.
        /// </summary>
        public async Task<ProductListDto> UpdateBySkuAsync(string sku, ProductUpdateDto productDto)
        {
            var product = await _productRepository.GetBySkuAsync(sku);
            var additionalImageUrls = ResolveAdditionalImageUrls(productDto);

            if (product != null)
            {
                // Mevcut ürünü güncelle
                var oldStock = product.StockQuantity;
                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.SpecialPrice = productDto.SpecialPrice;
                product.StockQuantity = productDto.StockQuantity;
                product.CategoryId = productDto.CategoryId;
                product.BrandId = productDto.BrandId;
                product.AdminOverrideName = ResolveAdminOverride(product.AdminOverrideName, productDto.AdminOverrideName);
                product.AdminOverridePrice = ResolveAdminOverride(product.AdminOverridePrice, productDto.AdminOverridePrice);
                product.AdminOverrideCategory = true;
                product.Slug = await EnsureProductSlugAsync(product, productDto.Name, product.SKU);

                if (productDto.MaxOrderQuantity.HasValue)
                    product.MaxOrderQuantity = Math.Max(0, productDto.MaxOrderQuantity.Value);
                if (productDto.MinOrderQuantity.HasValue)
                    product.MinOrderQuantity = Math.Max(0, productDto.MinOrderQuantity.Value);
                if (productDto.QuantityStep.HasValue)
                    product.QuantityStep = Math.Max(0m, productDto.QuantityStep.Value);
                if (productDto.MinOrderWeight.HasValue)
                    product.MinOrderWeight = Math.Max(0m, productDto.MinOrderWeight.Value);
                if (productDto.MaxOrderWeight.HasValue)
                    product.MaxOrderWeight = Math.Max(0m, productDto.MaxOrderWeight.Value);

                if (!string.IsNullOrWhiteSpace(productDto.ImageUrl))
                {
                    product.ImageUrl = productDto.ImageUrl.Trim();
                }

                await _productRepository.UpdateAsync(product);

                await ApplyProductImageUpdatesAsync(product.Id, productDto);

                if (oldStock != product.StockQuantity)
                {
                    await _inventoryLogService.WriteAsync(
                        product.Id, "ProductUpdated",
                        Math.Abs(product.StockQuantity - oldStock),
                        oldStock, product.StockQuantity,
                        $"Product:{product.Id}");
                }
            }
            else
            {
                var createUrls = additionalImageUrls != null && additionalImageUrls.Count > 0
                    ? BuildImageUrlList(productDto.ImageUrl, additionalImageUrls)
                    : BuildImageUrlList(productDto.ImageUrl, null);
                // Yeni ürün oluştur — Mikro'dan gelen SKU korunur
                product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    SpecialPrice = productDto.SpecialPrice,
                    StockQuantity = productDto.StockQuantity,
                    CategoryId = productDto.CategoryId,
                    ImageUrl = createUrls.FirstOrDefault() ?? string.Empty,
                    BrandId = productDto.BrandId,
                    AdminOverrideName = productDto.AdminOverrideName,
                    AdminOverridePrice = productDto.AdminOverridePrice,
                    AdminOverrideCategory = true,
                    SKU = sku,
                    Slug = await GenerateUniqueProductSlugAsync(productDto.Name, sku)
                };
                await _productRepository.AddAsync(product);
                await SyncProductImagesAsync(product.Id, createUrls);
            }

            InvalidateProductCaches();

            var saved = await _productRepository.GetByIdAsync(product.Id);
            return MapToDto(saved ?? product);
        }

        private static bool? ResolveAdminOverride(bool? currentValue, bool? requestedValue)
        {
            return requestedValue.HasValue ? requestedValue.Value : currentValue;
        }

        public async Task<bool> UpdateStockAsync(int id, int stock)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;
            
            var oldStock = product.StockQuantity;
            product.StockQuantity = stock;
            await _productRepository.UpdateAsync(product);
            if (oldStock != product.StockQuantity)
            {
                await _inventoryLogService.WriteAsync(
                    product.Id,
                    "ProductUpdated",
                    Math.Abs(stock - oldStock),
                    oldStock,
                    product.StockQuantity,
                    $"Product:{product.Id}");
            }
            InvalidateProductCaches();
            return true;
        }

        public async Task<int> GetProductCountAsync()
        {
            var allProducts = await _productRepository.GetAllAsync();
            return allProducts.Count();
        }

        private async Task<string> EnsureProductSlugAsync(Product product, string? name, string? sku)
        {
            if (!string.IsNullOrWhiteSpace(product.Slug))
            {
                return product.Slug;
            }

            return await GenerateUniqueProductSlugAsync(name, sku, product.Id > 0 ? product.Id : null);
        }

        private async Task<string> GenerateUniqueProductSlugAsync(string? name, string? sku, int? excludeId = null)
        {
            var baseInput = !string.IsNullOrWhiteSpace(name) ? name! : sku ?? "urun";
            var baseSlug = GenerateSlug(baseInput);

            if (!string.IsNullOrWhiteSpace(sku))
            {
                var skuSlug = GenerateSlug(sku);
                if (!string.IsNullOrWhiteSpace(skuSlug) &&
                    !string.Equals(baseSlug, skuSlug, StringComparison.OrdinalIgnoreCase))
                {
                    baseSlug = $"{baseSlug}-{skuSlug}";
                }
            }

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = $"urun-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            if (_dbContext == null)
            {
                return baseSlug;
            }

            var candidate = baseSlug;
            var suffix = 2;
            while (await _dbContext.Products.AnyAsync(product =>
                product.Slug == candidate &&
                (!excludeId.HasValue || product.Id != excludeId.Value)))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return candidate;
        }

        private static string GenerateSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            var previousDash = false;

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    previousDash = false;
                    continue;
                }

                if (previousDash)
                {
                    continue;
                }

                builder.Append('-');
                previousDash = true;
            }

            return builder
                .ToString()
                .Trim('-');
        }

        /// <summary>
        /// Sayfalı ürün listesi döndürür (PagedResult formatında).
        /// Toplu export işlemleri için totalCount bilgisi içerir.
        /// Cache kullanılmaz - güncel veri garantisi için.
        /// </summary>
        public async Task<ECommerce.Core.DTOs.PagedResult<ProductListDto>> GetProductsPagedAsync(int page = 1, int size = 50)
        {
            // Tüm ürünleri al (cache bypass - güncel veri için)
            var allProducts = (await _productRepository.GetAllAsync()).ToList();
            var totalCount = allProducts.Count;

            // Sayfalama uygula
            var pagedProducts = allProducts
                .OrderBy(p => p.Id) // ID'ye göre sırala (deterministic)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    SpecialPrice = p.SpecialPrice,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    Brand = p.Brand?.Name ?? string.Empty,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty
                })
                .ToList();

            return new ECommerce.Core.DTOs.PagedResult<ProductListDto>(
                pagedProducts,
                totalCount,
                (page - 1) * size,
                size
            );
        }

        public async Task<PagedResult<ProductListDto>> GetProductsByCategoryPagedAsync(
            int categoryId,
            int page = 1,
            int size = 50,
            string sort = "name",
            string direction = "asc",
            bool? inStock = null)
        {
            page = Math.Max(page, 1);
            size = Math.Clamp(size, 1, 100);
            var isDescending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

            if (_dbContext == null)
            {
                var products = (await _productRepository.GetAllAsync())
                    .Where(p => p.IsActive && p.CategoryId == categoryId && GetDisplayPrice(p) > 0);

                if (inStock.HasValue)
                    products = inStock.Value
                        ? products.Where(p => p.StockQuantity > 0)
                        : products.Where(p => p.StockQuantity <= 0);

                products = ApplySort(products, sort, isDescending);

                var totalCount = products.Count();
                var items = products
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(MapToListDto)
                    .ToList();

                return new PagedResult<ProductListDto>(items, totalCount, (page - 1) * size, size);
            }

            IQueryable<Product> query = _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.CategoryId == categoryId && (p.SpecialPrice.HasValue && p.SpecialPrice.Value > 0 && p.SpecialPrice.Value < p.Price || p.Price > 0));

            if (inStock.HasValue)
                query = inStock.Value
                    ? query.Where(p => p.StockQuantity > 0)
                    : query.Where(p => p.StockQuantity <= 0);

            query = ApplySort(query, sort, isDescending);

            var total = await query.CountAsync();
            var pagedItems = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    SpecialPrice = p.SpecialPrice,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    Brand = p.Brand != null ? p.Brand.Name : string.Empty,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty
                })
                .ToListAsync();

            return new PagedResult<ProductListDto>(pagedItems, total, (page - 1) * size, size);
        }

        private static IEnumerable<Product> ApplySort(IEnumerable<Product> products, string? sort, bool isDescending)
        {
            var normalizedSort = sort?.Trim().ToLowerInvariant();

            return normalizedSort switch
            {
                "price" => isDescending ? products.OrderByDescending(p => p.Price).ThenBy(p => p.Name) : products.OrderBy(p => p.Price).ThenBy(p => p.Name),
                "newest" => isDescending ? products.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name) : products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                _ => isDescending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name)
            };
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> products, string? sort, bool isDescending)
        {
            var normalizedSort = sort?.Trim().ToLowerInvariant();

            return normalizedSort switch
            {
                "price" => isDescending ? products.OrderByDescending(p => p.Price).ThenBy(p => p.Name) : products.OrderBy(p => p.Price).ThenBy(p => p.Name),
                "newest" => isDescending ? products.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name) : products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                _ => isDescending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name)
            };
        }

        private static decimal GetDisplayPrice(Product product)
        {
            if (product.SpecialPrice.HasValue &&
                product.SpecialPrice.Value > 0 &&
                product.SpecialPrice.Value < product.Price)
            {
                return product.SpecialPrice.Value;
            }

            return product.Price;
        }

        private static ProductListDto MapToListDto(Product p)
        {
            return new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug ?? string.Empty,
                Description = p.Description ?? string.Empty,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty
            };
        }

        public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(int page = 1, int size = 10)
        {
            var cacheKey = $"all_products_{page}_{size}";
            if (!_cache.TryGetValue(cacheKey, out object? cachedObj) || !(cachedObj is IEnumerable<ProductListDto> cached))
            {
                var products = (await _productRepository.GetAllAsync())
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * size)
                    .Take(size);

                cached = products.Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    SpecialPrice = p.SpecialPrice,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    Brand = p.Brand?.Name ?? string.Empty,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty
                }).ToList();

                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
                TrackCacheKey(cacheKey);
            }

            return cached;
        }

        public async Task<IEnumerable<ProductListDto>> GetActiveProductsAsync(int page = 1, int size = 10, int? categoryId = null)
        {
            var cacheKey = $"active_products_{categoryId ?? 0}_{page}_{size}";
            if (!_cache.TryGetValue(cacheKey, out object? cachedObj) || !(cachedObj is IEnumerable<ProductListDto> cached))
            {
                var products = await _productRepository.GetAllAsync();
                
                // SADECE AKTİF ÜRÜNLERİ FİLTRELE
                products = products.Where(p => p.IsActive);
                
                // KATEGORİYE GÖRE FİLTRELE
                if (categoryId.HasValue)
                    products = products.Where(p => p.CategoryId == categoryId.Value);

                products = products.Skip((page - 1) * size).Take(size);

                cached = products.Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    SpecialPrice = p.SpecialPrice,
                    StockQuantity = p.StockQuantity,
                    ImageUrl = p.ImageUrl,
                    Brand = p.Brand?.Name ?? string.Empty,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty
                }).ToList();

                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
                TrackCacheKey(cacheKey);
            }

            return cached;
        }

        public async Task<ProductListDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            return new ProductListDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug ?? string.Empty,
                Description = product.Description ?? string.Empty,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand?.Name ?? string.Empty,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty
            };
        }

        public async Task AddProductReviewAsync(int productId, int userId, ProductReviewCreateDto reviewDto)
        {
            if (await _reviewRepository.HasUserReviewAsync(productId, userId))
            {
                throw new InvalidOperationException("Bu ürüne zaten bir yorum eklemişsiniz.");
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false
            };

            await _reviewRepository.AddAsync(review);
        }

        public Task AddFavoriteAsync(int userId, int productId)
        {
            // TODO: Favori sistemi eklenecekse buraya yazılacak.
            return Task.CompletedTask;
        }

        #region Kampanya Entegrasyonu
        
        /// <summary>
        /// Kullanıcı tarafı ürün detayı - Kampanya bilgileriyle birlikte.
        /// SpecialPrice varsa, kampanya bilgileri de DTO'ya eklenir.
        /// </summary>
        public async Task<ProductListDto?> GetProductByIdWithCampaignAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            var dto = MapToDto(product);
            
            // Kampanya bilgilerini hesapla
            EnrichWithCampaignInfo(dto, product);
            await EnrichWithOrderLimitsAsync(dto, product);
            
            return dto;
        }
        
        /// <summary>
        /// Aktif ürünleri kampanya bilgileriyle birlikte getirir.
        /// Kampanyalı ürünler önce sıralanır (en yüksek indirim önce).
        /// </summary>
        public async Task<IEnumerable<ProductListDto>> GetActiveProductsWithCampaignAsync(int page = 1, int size = 10, int? categoryId = null)
        {
            var cacheKey = $"active_products_campaign_{categoryId ?? 0}_{page}_{size}";
            
            if (!_cache.TryGetValue(cacheKey, out object? cachedObj) || !(cachedObj is IEnumerable<ProductListDto> cached))
            {
                var products = await _productRepository.GetAllAsync();
                
                // Sadece aktif ürünleri filtrele
                products = products.Where(p => p.IsActive);
                
                // Kategoriye göre filtrele
                if (categoryId.HasValue)
                    products = products.Where(p => p.CategoryId == categoryId.Value);

                // DTO'ya çevir, kampanya ve sipariş limitlerini ekle
                var limitSettings = await GetLimitSettingsAsync();
                var dtoList = products.Select(p =>
                {
                    var dto = MapToDto(p);
                    EnrichWithCampaignInfo(dto, p);
                    if (_orderLimitResolver != null)
                    {
                        dto.OrderLimits = _orderLimitResolver.ResolveLimits(p, null, limitSettings);
                    }
                    return dto;
                }).ToList();
                
                // Kampanyalı ürünler önce, sonra indirim yüzdesine göre sırala
                cached = dtoList
                    .OrderByDescending(p => p.HasActiveCampaign)
                    .ThenByDescending(p => p.DiscountPercentage ?? 0)
                    .ThenBy(p => p.Name)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });
                TrackCacheKey(cacheKey);
            }

            return cached;
        }
        
        /// <summary>
        /// Product entity'sini ProductListDto'ya dönüştürür.
        /// </summary>
        private ProductListDto MapToDto(Product product)
        {
            var imageUrls = GetProductImageUrls(product);
            return new ProductListDto
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
                AdminOverrideName = product.AdminOverrideName,
                AdminOverridePrice = product.AdminOverridePrice,
                AdminOverrideCategory = product.AdminOverrideCategory,
                IsWeightBased = WeightBasedProductResolver.ResolveIsWeightBased(product),
                Unit = MapWeightUnitLabel(product.WeightUnit),
                MaxOrderQuantity = product.MaxOrderQuantity,
                MinOrderQuantity = product.MinOrderQuantity,
                QuantityStep = product.QuantityStep,
                MinOrderWeight = product.MinOrderWeight,
                MaxOrderWeight = product.MaxOrderWeight
            };
        }

        private async Task EnrichWithOrderLimitsAsync(ProductListDto dto, Product product, ProductVariant? variant = null)
        {
            if (_orderLimitResolver == null)
            {
                return;
            }

            var settings = await GetLimitSettingsAsync();
            dto.OrderLimits = _orderLimitResolver.ResolveLimits(product, variant, settings);
        }

        private static string MapWeightUnitLabel(Entities.Enums.WeightUnit unit) =>
            unit switch
            {
                Entities.Enums.WeightUnit.Kilogram => "KG",
                Entities.Enums.WeightUnit.Gram => "GR",
                Entities.Enums.WeightUnit.Liter => "LT",
                Entities.Enums.WeightUnit.Milliliter => "ML",
                _ => "ADET"
            };

        private async Task<ProductOrderLimitSettingsDto> GetLimitSettingsAsync()
        {
            if (_cachedLimitSettings != null)
            {
                return _cachedLimitSettings;
            }

            if (_limitSettingsService == null)
            {
                _cachedLimitSettings = new ProductOrderLimitSettingsDto();
                return _cachedLimitSettings;
            }

            _cachedLimitSettings = await _limitSettingsService.GetActiveSettingsAsync();
            return _cachedLimitSettings;
        }

        private static List<string> GetProductImageUrls(Product product)
        {
            if (product.ProductImages?.Any(pi => pi.IsActive && !string.IsNullOrWhiteSpace(pi.Url)) == true)
            {
                return product.ProductImages
                    .Where(pi => pi.IsActive && !string.IsNullOrWhiteSpace(pi.Url))
                    .OrderByDescending(pi => pi.IsMain)
                    .ThenBy(pi => pi.Id)
                    .Select(pi => pi.Url.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                return new List<string> { product.ImageUrl.Trim() };
            }

            return new List<string>();
        }

        private static List<string> BuildImageUrlList(string? primaryUrl, IEnumerable<string>? additionalUrls)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(primaryUrl))
            {
                result.Add(primaryUrl.Trim());
            }

            if (additionalUrls != null)
            {
                foreach (var url in additionalUrls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    var trimmed = url.Trim();
                    if (!result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(trimmed);
                    }
                }
            }

            return result;
        }

        private static List<string>? ResolveAdditionalImageUrls(ProductUpdateDto productDto)
        {
            if (productDto.AdditionalImageUrls != null && productDto.AdditionalImageUrls.Count > 0)
            {
                return BuildImageUrlList(null, productDto.AdditionalImageUrls);
            }

            if (productDto.ImageUrls != null && productDto.ImageUrls.Count > 0)
            {
                return BuildImageUrlList(null, productDto.ImageUrls);
            }

            return null;
        }

        private async Task ApplyProductImageUpdatesAsync(int productId, ProductUpdateDto productDto)
        {
            if (_dbContext == null) return;

            await EnsureLegacyImageMigratedAsync(productId);

            if (productDto.ImageUrls != null)
            {
                await SyncProductImagesToDesiredListAsync(
                    productId,
                    productDto.ImageUrls,
                    productDto.ImageUrl);
                return;
            }

            var additionalUrls = ResolveAdditionalImageUrls(productDto);
            if (additionalUrls != null && additionalUrls.Count > 0)
            {
                await AppendProductImagesAsync(productId, additionalUrls);
            }

            if (!string.IsNullOrWhiteSpace(productDto.ImageUrl))
            {
                await SetPrimaryProductImageAsync(productId, productDto.ImageUrl.Trim());
            }
        }

        private async Task EnsureLegacyImageMigratedAsync(int productId)
        {
            if (_dbContext == null) return;

            var product = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                return;
            }

            var hasImages = await _dbContext.ProductImages
                .AnyAsync(pi => pi.ProductId == productId && pi.IsActive);
            if (hasImages)
            {
                return;
            }

            _dbContext.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = product.ImageUrl.Trim(),
                FileName = product.ImageUrl.Split('/').LastOrDefault() ?? "image",
                IsMain = true,
                IsActive = true
            });
            await _dbContext.SaveChangesAsync();
        }

        private async Task AppendProductImagesAsync(int productId, IList<string> urls)
        {
            if (_dbContext == null) return;

            var normalized = urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (normalized.Count == 0) return;

            var existingUrls = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == productId && pi.IsActive)
                .Select(pi => pi.Url)
                .ToListAsync();

            var productImageUrl = await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => p.ImageUrl)
                .FirstOrDefaultAsync();

            var knownUrls = new HashSet<string>(existingUrls, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(productImageUrl))
            {
                knownUrls.Add(productImageUrl.Trim());
            }

            var added = false;
            foreach (var url in normalized)
            {
                if (knownUrls.Contains(url)) continue;

                _dbContext.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Url = url,
                    FileName = url.Split('/').LastOrDefault() ?? "image",
                    IsMain = false,
                    IsActive = true
                });
                knownUrls.Add(url);
                added = true;
            }

            if (added)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task SetPrimaryProductImageAsync(int productId, string primaryUrl)
        {
            if (_dbContext == null || string.IsNullOrWhiteSpace(primaryUrl)) return;

            var normalizedPrimary = primaryUrl.Trim();
            var images = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == productId && pi.IsActive)
                .ToListAsync();

            var matched = images.FirstOrDefault(pi =>
                string.Equals(pi.Url, normalizedPrimary, StringComparison.OrdinalIgnoreCase));

            if (matched == null)
            {
                _dbContext.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Url = normalizedPrimary,
                    FileName = normalizedPrimary.Split('/').LastOrDefault() ?? "image",
                    IsMain = true,
                    IsActive = true
                });
            }
            else
            {
                foreach (var image in images)
                {
                    image.IsMain = string.Equals(image.Url, normalizedPrimary, StringComparison.OrdinalIgnoreCase);
                }
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.ImageUrl = normalizedPrimary;
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task SyncProductImagesToDesiredListAsync(
            int productId,
            IList<string> desiredUrls,
            string? primaryImageUrl)
        {
            if (_dbContext == null) return;

            var normalized = desiredUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existing = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            var desiredSet = new HashSet<string>(normalized, StringComparer.OrdinalIgnoreCase);
            foreach (var image in existing)
            {
                if (!desiredSet.Contains(image.Url.Trim()))
                {
                    _dbContext.ProductImages.Remove(image);
                }
            }

            var remainingUrls = existing
                .Where(pi => desiredSet.Contains(pi.Url.Trim()))
                .Select(pi => pi.Url.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var url in normalized)
            {
                if (remainingUrls.Contains(url)) continue;

                _dbContext.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Url = url,
                    FileName = url.Split('/').LastOrDefault() ?? "image",
                    IsMain = false,
                    IsActive = true
                });
                remainingUrls.Add(url);
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                var primary = !string.IsNullOrWhiteSpace(primaryImageUrl)
                    ? primaryImageUrl.Trim()
                    : normalized.FirstOrDefault() ?? string.Empty;
                product.ImageUrl = primary;
            }

            await _dbContext.SaveChangesAsync();

            if (normalized.Count == 0)
            {
                return;
            }

            var primaryToSet = !string.IsNullOrWhiteSpace(primaryImageUrl)
                ? primaryImageUrl.Trim()
                : normalized[0];
            await SetPrimaryProductImageAsync(productId, primaryToSet);
        }

        /// <summary>
        /// Yeni ürün oluşturma — ilk görsel seti.
        /// </summary>
        private async Task SyncProductImagesAsync(int productId, IList<string> urls)
        {
            if (_dbContext == null) return;

            var normalized = urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existing = await _dbContext.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync();

            if (existing.Count > 0)
            {
                _dbContext.ProductImages.RemoveRange(existing);
            }

            for (var i = 0; i < normalized.Count; i++)
            {
                var url = normalized[i];
                _dbContext.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Url = url,
                    FileName = url.Split('/').LastOrDefault() ?? "image",
                    IsMain = i == 0,
                    IsActive = true
                });
            }

            await _dbContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// DTO'ya kampanya bilgilerini ekler.
        /// SpecialPrice varsa, OriginalPrice ve DiscountPercentage hesaplanır.
        /// </summary>
        private void EnrichWithCampaignInfo(ProductListDto dto, Product product)
        {
            // SpecialPrice varsa ve Price'dan küçükse kampanya aktif demektir
            if (product.SpecialPrice.HasValue && product.SpecialPrice.Value < product.Price)
            {
                dto.OriginalPrice = product.Price;
                dto.SpecialPrice = product.SpecialPrice;
                
                // İndirim yüzdesini hesapla
                dto.DiscountPercentage = (int)Math.Round(
                    (1 - (product.SpecialPrice.Value / product.Price)) * 100
                );
                
                // NOT: CampaignId ve CampaignName şu an Product entity'sinde saklanmıyor.
                // Bu bilgiler için CampaignRepository'den sorgu yapılabilir.
                // Şimdilik sadece fiyat bazlı bilgiler doldurulur.
            }
        }
        
        #endregion

        // Track and invalidate cache keys created by this manager.
        private void TrackCacheKey(string key)
        {
            try
            {
                var keys = _cache.GetOrCreate(ProductCacheKeysKey, entry => new HashSet<string>());
                if (keys is HashSet<string> set)
                {
                    set.Add(key);
                    _cache.Set(ProductCacheKeysKey, set, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
                }
            }
            catch
            {
                // swallow: tracking is best-effort
            }
        }

        private void InvalidateProductCaches()
        {
            try
            {
                if (_cache.TryGetValue(ProductCacheKeysKey, out object? keysObj) && keysObj is HashSet<string> keys)
                {
                    foreach (var k in keys)
                    {
                        _cache.Remove(k);
                    }
                    _cache.Remove(ProductCacheKeysKey);
                }
            }
            catch
            {
                // swallow
            }
        }
    }
}
