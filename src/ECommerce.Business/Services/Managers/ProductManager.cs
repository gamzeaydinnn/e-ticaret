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
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return; // null-safe kontrol

            await _productRepository.Delete(product);
        }

        public async Task<IEnumerable<ProductListDto>> SearchAsync(string searchTerm)
        {
            var products = await _productRepository.SearchAsync(searchTerm);
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

        public Task<IEnumerable<ProductListDto>> GetProductsAsync(string query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }
    }
}
