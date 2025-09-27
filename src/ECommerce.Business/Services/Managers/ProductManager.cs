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
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                Brand = product.Brand
            };
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
    var product = await _context.Products.FindAsync(id);
    if (product == null) return;

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();
}


        // Admin panel için ek methodlar
        public async Task<int> GetProductCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync(int page = 1, int size = 10)
        {
            var products = await _context.Products
                .OrderBy(p => p.Name)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

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
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.StockQuantity = stock;
                await _context.SaveChangesAsync();
            }
        }

        public Task<IEnumerable<ProductListDto>> GetProductsAsync(string query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }
    }
}
