
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Product
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.DTOs.ProductReview;   // Product DTO'ları
using ECommerce.Core.DTOs; // PagedResult



namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Ürün yönetimi servisi interface'i.
    /// CRUD operasyonları, arama, kampanya entegrasyonu ve kullanıcı işlemlerini kapsar.
    /// </summary>
    public interface IProductService
    {
        Task<IEnumerable<ProductListDto>> GetProductsAsync(
            string query = null, int? categoryId = null, int page = 1, int pageSize = 20);

        Task<ProductListDto?> GetByIdAsync(int id);
        Task<ProductListDto> CreateProductAsync(ProductCreateDto productDto);
        Task<ProductListDto> UpdateProductAsync(int id, ProductUpdateDto productDto);
        /// <summary>
        /// SKU bazlı ürün güncelleme — Mikro ERP ürünlerinde id olmadığında kullanılır.
        /// Yerel DB'de SKU varsa günceller, yoksa yeni ürün oluşturur (upsert).
        /// </summary>
        Task<ProductListDto> UpdateBySkuAsync(string sku, ProductUpdateDto productDto);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int id, int stock);
        Task<int> GetProductCountAsync();
        Task<IEnumerable<ProductListDto>> GetAllProductsAsync(int page = 1, int size = 10);

        /// <summary>
        /// Sayfalı ürün listesi döndürür (PagedResult formatında).
        /// Toplu export işlemleri için totalCount bilgisi içerir.
        /// </summary>
        /// <param name="page">Sayfa numarası (1'den başlar)</param>
        /// <param name="size">Sayfa başına ürün sayısı</param>
        /// <returns>PagedResult formatında ürün listesi</returns>
        Task<PagedResult<ProductListDto>> GetProductsPagedAsync(int page = 1, int size = 50);

        // Kullanıcı tarafı ürün listeleme
        Task<IEnumerable<ProductListDto>> GetActiveProductsAsync(int page = 1, int size = 10, int? categoryId = null);

        // Kullanıcı tarafı ürün detayı
        Task<ProductListDto?> GetProductByIdAsync(int id);

        // ✅ Genel Arama Metodu
        Task<IEnumerable<ProductListDto>> SearchProductsAsync(string query, int page = 1, int size = 10);

        // ✅ Kullanıcı ürün yorumu ekleme (DTO kullanacak)
        Task AddProductReviewAsync(int productId, int userId, ProductReviewCreateDto reviewDto);

        // Kullanıcı favoriye ekleme
        Task AddFavoriteAsync(int userId, int productId);
        
        #region Kampanya Entegrasyonu
        
        /// <summary>
        /// Kullanıcı tarafı ürün detayı - Kampanya bilgileriyle birlikte.
        /// Aktif kampanya varsa SpecialPrice, CampaignId, CampaignName ve DiscountPercentage doldurulur.
        /// </summary>
        /// <param name="id">Ürün ID</param>
        /// <returns>Kampanya bilgileri dahil ürün detayı</returns>
        Task<ProductListDto?> GetProductByIdWithCampaignAsync(int id);
        
        /// <summary>
        /// Aktif ürünleri kampanya bilgileriyle birlikte getirir.
        /// Kampanyalı ürünler önce sıralanır.
        /// </summary>
        Task<IEnumerable<ProductListDto>> GetActiveProductsWithCampaignAsync(int page = 1, int size = 10, int? categoryId = null);
        
        #endregion
    }
}
