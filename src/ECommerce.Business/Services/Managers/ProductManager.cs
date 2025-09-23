using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Business.Services.Managers
{
    public class ProductManager : IProductService
    {
        private readonly ECommerceDbContext _context;

        public ProductManager(ECommerceDbContext context)
        {
            _context = context;
        }

       public async Task<ProductListDto?> GetByIdAsync(int id)  // <-- ? ekledik
{
    var p = await _context.Products.FindAsync(id);
    if (p == null) return null;
    return new ProductListDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        Brand = p.Brand
    };
}



        public async Task<IEnumerable<ProductListDto>> GetProductsAsync(
            string query = null, int? categoryId = null, int page = 1, int pageSize = 20)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query) || p.Description.Contains(query));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
        public async Task<ProductListDto> CreateAsync(ProductCreateDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl,
                Brand = dto.Brand,
                CategoryId = dto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

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
    var product = await _context.Products.FindAsync(id);
    if (product == null) return;

    product.Name = dto.Name;
    product.Description = dto.Description;
    product.Price = dto.Price;
    product.StockQuantity = dto.StockQuantity;
    product.ImageUrl = dto.ImageUrl;
    product.Brand = dto.Brand;
    product.CategoryId = dto.CategoryId;

    await _context.SaveChangesAsync();
}

public async Task DeleteAsync(int id)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return;

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();
}


        // Create, Update, Delete vs. burada olacak
    }
}
