
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Product
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs.ProductReview;   // Product DTO'ları



namespace ECommerce.Business.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductListDto>> GetProductsAsync(
            string query = null, int? categoryId = null, int page = 1, int pageSize = 20);

        Task<ProductListDto?> GetByIdAsync(int id);
        Task<ProductListDto> CreateProductAsync(ProductCreateDto productDto);
        Task UpdateProductAsync(int id, ProductUpdateDto productDto);
        Task DeleteProductAsync(int id);
        Task UpdateStockAsync(int id, int stock);
        Task<int> GetProductCountAsync();
        Task<IEnumerable<ProductListDto>> GetAllProductsAsync(int page = 1, int size = 10);

        // Kullanıcı tarafı ürün listeleme
        Task<IEnumerable<ProductListDto>> GetActiveProductsAsync(int page = 1, int size = 10, int? categoryId = null);

        // Kullanıcı tarafı ürün detayı
        Task<ProductListDto?> GetProductByIdAsync(int id);

        // ✅ Kullanıcı ürün yorumu ekleme (DTO kullanacak)
        Task AddProductReviewAsync(int productId, int userId, ProductReviewCreateDto reviewDto);

        // Kullanıcı favoriye ekleme
        Task AddFavoriteAsync(int userId, int productId);
    }
}
