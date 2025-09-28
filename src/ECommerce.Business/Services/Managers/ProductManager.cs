using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class ProductManager : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductManager(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAsync(
    string? query = null,
    int? categoryId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    bool? inStock = null,
    int page = 1,
    int pageSize = 20)
{
    // Tüm ürünleri çek
    var products = await _productRepository.GetAllAsync();

    // Filtreleme
    if (!string.IsNullOrEmpty(query))
        products = products.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

    if (categoryId.HasValue)
        products = products.Where(p => p.CategoryId == categoryId.Value);

    if (minPrice.HasValue)
        products = products.Where(p => p.Price >= minPrice.Value);

    if (maxPrice.HasValue)
        products = products.Where(p => p.Price <= maxPrice.Value);

    if (inStock.HasValue)
    {
        if (inStock.Value)
            products = products.Where(p => p.StockQuantity > 0);
        else
            products = products.Where(p => p.StockQuantity == 0);
    }

    // Pagination
    products = products
        .OrderBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize);

    // DTO dönüşümü
    return products.Select(p => new ProductListDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        Brand = p.Brand
    });
}


        public async Task<IEnumerable<ProductListDto>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand
            });
        }

        public async Task<ProductListDto> CreateAsync(ProductCreateDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl ?? string.Empty,
                Brand = dto.Brand ?? string.Empty
            };

            await _productRepository.AddAsync(product);

            return new ProductListDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand
            };
        }

        public async Task UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return; // null-safe kontrol

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.ImageUrl = dto.ImageUrl ?? product.ImageUrl;
            product.Brand = dto.Brand ?? product.Brand;

            await _productRepository.UpdateAsync(product);
        }

public async Task DeleteAsync(int id)
{
    var product = await _productRepository.GetByIdAsync(id);
    if (product == null) return;

    await _productRepository.DeleteAsync(product);
}


        // Admin panel için ek methodlar
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
        StockQuantity = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        Brand = p.Brand
    });
}

        public async Task<ProductListDto> CreateProductAsync(ProductCreateDto productDto)
        {
            return await CreateAsync(productDto);
        }

        public async Task UpdateProductAsync(int id, ProductUpdateDto productDto)
        {
            await UpdateAsync(id, productDto);
        }

        public async Task DeleteProductAsync(int id)
        {
            await DeleteAsync(id);
        }

        public async Task UpdateStockAsync(int id, int stock)
{
    var product = await _productRepository.GetByIdAsync(id);
    if (product != null)
    {
        product.StockQuantity = stock;
        await _productRepository.UpdateAsync(product);
    }
}

        public Task<IEnumerable<ProductListDto>> GetProductsAsync(string query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }

        public Task<ProductListDto?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
