using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IProductService
using ECommerce.Entities.Concrete;            // Product
using ECommerce.Core.Interfaces;              // IProductRepository
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs.ProductReview;            // Product DTO
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Business.Services.Managers
{
    public class ProductManager : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMemoryCache _cache;
        private readonly IInventoryLogService _inventoryLogService;
        private const string ProductCacheKeysKey = "products_cache_keys";

        public ProductManager(
            IProductRepository productRepository,
            IReviewRepository reviewRepository,
            IMemoryCache cache,
            IInventoryLogService inventoryLogService)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _cache = cache;
            _inventoryLogService = inventoryLogService;
        }

        // Backwards-compatible constructor for tests or callers that don't provide an IMemoryCache.
        // This avoids breaking existing unit tests that construct ProductManager without cache.
        public ProductManager(
            IProductRepository productRepository,
            IReviewRepository reviewRepository,
            IInventoryLogService inventoryLogService)
            : this(productRepository, reviewRepository, new MemoryCache(new MemoryCacheOptions()), inventoryLogService)
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
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty
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
            });
        }

        public async Task<ProductListDto?> GetByIdAsync(int id)
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

        public async Task<ProductListDto> CreateProductAsync(ProductCreateDto productDto)
        {
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                SpecialPrice = productDto.SpecialPrice,
                StockQuantity = productDto.StockQuantity,
                CategoryId = productDto.CategoryId,
                ImageUrl = productDto.ImageUrl ?? string.Empty,
                BrandId = productDto.BrandId
            };

            await _productRepository.AddAsync(product);
            // Invalidate product-related caches so subsequent reads reflect the new product
            InvalidateProductCaches();

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

        public async Task<ProductListDto> UpdateProductAsync(int id, ProductUpdateDto productDto)
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
            product.ImageUrl = productDto.ImageUrl ?? product.ImageUrl;
            product.BrandId = productDto.BrandId;

            await _productRepository.UpdateAsync(product);
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

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;
            await _productRepository.DeleteAsync(product);
            InvalidateProductCaches();
            return true;
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
