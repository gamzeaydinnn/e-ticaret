using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IProductService
using ECommerce.Entities.Concrete;            // Product
using ECommerce.Core.Interfaces;              // IProductRepository
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs.ProductReview;            // Product DTO
using System.Linq;



namespace ECommerce.Business.Services.Managers
{
    public class ProductManager : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IStockUpdatePublisher _stockPublisher;
        // Yeni Eklenen: Genel Arama Metodu
        public async Task<IEnumerable<ProductListDto>> SearchProductsAsync(string query, int page = 1, int size = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Arama sorgusu yoksa boş liste dönüyoruz veya aktif ürünleri dönebilirsiniz.
                return Enumerable.Empty<ProductListDto>();
            }

            // Tüm ürünleri çek
            var allProducts = await _productRepository.GetAllAsync();

            // Arama sorgusuna göre filtrele (isimde veya açıklamada içeriyorsa)
            var filteredProducts = allProducts.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Sayfalama uygula
            var pagedProducts = filteredProducts
                .OrderBy(p => p.Name) // Sonuçları isme göre sırala
                .Skip((page - 1) * size)
                .Take(size);

            // DTO'ya dönüştür
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
                CategoryName = p.Category?.Name ?? string.Empty
            });
        }
        

        private sealed class NoopStockPublisher : IStockUpdatePublisher
        {
            public Task PublishAsync(int productId, int newQuantity) => Task.CompletedTask;
        }

        public ProductManager(IProductRepository productRepository, IReviewRepository reviewRepository, IStockUpdatePublisher stockPublisher)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _stockPublisher = stockPublisher;
        }

        // Backward-compatible ctor for tests or contexts without realtime wiring
        public ProductManager(IProductRepository productRepository, IReviewRepository reviewRepository)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _stockPublisher = new NoopStockPublisher();
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAsync(string query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            var products = await _productRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p =>
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
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
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
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
                Description = product.Description,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand?.Name ?? string.Empty,
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

            return new ProductListDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand?.Name ?? string.Empty,
                CategoryName = product.Category?.Name ?? string.Empty
            };
        }

        public async Task UpdateProductAsync(int id, ProductUpdateDto productDto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return;

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.SpecialPrice = productDto.SpecialPrice;
            product.StockQuantity = productDto.StockQuantity;
            product.CategoryId = productDto.CategoryId;
            product.ImageUrl = productDto.ImageUrl ?? product.ImageUrl;
            product.BrandId = productDto.BrandId;

            await _productRepository.UpdateAsync(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return;
            await _productRepository.DeleteAsync(product);
        }

        public async Task UpdateStockAsync(int id, int stock)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                product.StockQuantity = stock;
                await _productRepository.UpdateAsync(product);
                await _stockPublisher.PublishAsync(product.Id, product.StockQuantity);
            }
        }

        public async Task<int> GetProductCountAsync()
        {
            var allProducts = await _productRepository.GetAllAsync();
            return allProducts.Count();
        }

        public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(int page = 1, int size = 10)
        {
            var products = (await _productRepository.GetAllAsync())
                .OrderBy(p => p.Name)
                .Skip((page - 1) * size)
                .Take(size);

            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryName = p.Category?.Name ?? string.Empty
            });
        }

        public async Task<IEnumerable<ProductListDto>> GetActiveProductsAsync(int page = 1, int size = 10, int? categoryId = null)
        {
            var products = await _productRepository.GetAllAsync();
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);

            products = products.Skip((page - 1) * size).Take(size);

            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryName = p.Category?.Name ?? string.Empty
            });
        }

        public async Task<ProductListDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            return new ProductListDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SpecialPrice = product.SpecialPrice,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand?.Name ?? string.Empty,
                CategoryName = product.Category?.Name ?? string.Empty
            };
        }

        public async Task AddProductReviewAsync(int productId, int userId, ProductReviewCreateDto reviewDto)
        {
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

        public async Task<IEnumerable<ProductListDto>> FilterProductsAsync(ProductFilterDto filter)
        {
            var products = await _productRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim();
                products = products.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (filter.CategoryIds != null && filter.CategoryIds.Count > 0)
                products = products.Where(p => filter.CategoryIds.Contains(p.CategoryId));

            if (filter.BrandIds != null && filter.BrandIds.Count > 0)
                products = products.Where(p => p.BrandId.HasValue && filter.BrandIds.Contains(p.BrandId.Value));

            if (filter.MinPrice.HasValue)
            {
                products = products.Where(p => (p.SpecialPrice ?? p.Price) >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                products = products.Where(p => (p.SpecialPrice ?? p.Price) <= filter.MaxPrice.Value);
            }

            if (filter.InStockOnly == true)
                products = products.Where(p => p.StockQuantity > 0);

            if (filter.MinRating.HasValue && filter.MinRating.Value > 0)
            {
                products = products.Where(p =>
                {
                    var approved = p.ProductReviews?.Where(r => r.IsApproved) ?? Enumerable.Empty<ProductReview>();
                    if (!approved.Any()) return false;
                    var avg = approved.Average(r => (double)r.Rating);
                    return avg >= filter.MinRating.Value;
                });
            }

            // Sorting
            var sortBy = (filter.SortBy ?? "name").ToLowerInvariant();
            var sortDir = (filter.SortDir ?? "asc").ToLowerInvariant();

            Func<Entities.Concrete.Product, object> keySelector = sortBy switch
            {
                "price" => p => (p.SpecialPrice ?? p.Price),
                "created" => p => p.CreatedAt,
                _ => p => p.Name
            };

            products = sortDir == "desc" ? products.OrderByDescending(keySelector) : products.OrderBy(keySelector);

            // Paging
            var page = Math.Max(1, filter.Page);
            var size = Math.Clamp(filter.Size, 1, 100);
            products = products.Skip((page - 1) * size).Take(size);

            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SpecialPrice = p.SpecialPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand?.Name ?? string.Empty,
                CategoryName = p.Category?.Name ?? string.Empty
            });
        }

        public Task AddFavoriteAsync(int userId, int productId)
        {
            // TODO: Favori sistemi eklenecekse buraya yazılacak.
            return Task.CompletedTask;
        }

    }
}
