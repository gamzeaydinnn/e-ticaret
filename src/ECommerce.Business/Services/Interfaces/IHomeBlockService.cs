using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.HomeBlock;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu Servis Interface'i
    /// ------------------------------------------------
    /// Business logic katmanı - Controller ve Repository arasında köprü görevi görür.
    /// 
    /// Sorumluluklar:
    /// - DTO/Entity dönüşümleri
    /// - Blok tipine göre ürün filtreleme (category, discounted, newest, bestseller)
    /// - Slug oluşturma ve validasyon
    /// - ViewAllUrl otomatik oluşturma
    /// </summary>
    public interface IHomeBlockService
    {
        #region Public Endpoints (Ana Sayfa İçin)

        /// <summary>
        /// Ana sayfa için aktif blokları ürünleriyle birlikte getirir
        /// DisplayOrder'a göre sıralı, tarih kontrolü yapılmış
        /// Her blok MaxProductCount kadar ürün içerir
        /// </summary>
        Task<IEnumerable<HomeProductBlockDto>> GetActiveBlocksForHomepageAsync();

        /// <summary>
        /// Slug'a göre tek blok getirir (Tümünü Gör sayfası için)
        /// Tüm ürünleri içerir (MaxProductCount sınırı yok)
        /// </summary>
        Task<HomeProductBlockDto?> GetBlockBySlugAsync(string slug);

        #endregion

        #region Admin CRUD Operations

        /// <summary>
        /// Tüm blokları getirir (admin listesi için)
        /// Ürün sayıları dahil
        /// </summary>
        Task<IEnumerable<HomeProductBlockDto>> GetAllBlocksAsync();

        /// <summary>
        /// ID'ye göre blok detayı getirir (admin edit için)
        /// </summary>
        Task<HomeProductBlockDto?> GetBlockByIdAsync(int id);

        /// <summary>
        /// Yeni blok oluşturur
        /// Slug otomatik oluşturulur
        /// </summary>
        Task<HomeProductBlockDto> CreateBlockAsync(CreateHomeBlockDto dto);

        /// <summary>
        /// Mevcut bloğu günceller
        /// </summary>
        Task<HomeProductBlockDto?> UpdateBlockAsync(int id, UpdateHomeBlockDto dto);

        /// <summary>
        /// Bloğu siler
        /// </summary>
        Task<bool> DeleteBlockAsync(int id);

        /// <summary>
        /// Blok sıralamasını değiştirir
        /// </summary>
        Task<bool> UpdateBlockOrderAsync(int id, int newDisplayOrder);

        /// <summary>
        /// Blokları toplu sıralar
        /// </summary>
        Task UpdateBlocksOrderAsync(IEnumerable<(int id, int displayOrder)> orders);

        #endregion

        #region Block Products (Ürün Yönetimi)

        /// <summary>
        /// Bloğa ürün ekler
        /// </summary>
        Task<bool> AddProductToBlockAsync(AddProductToBlockDto dto);

        /// <summary>
        /// Bloğa birden fazla ürün ekler
        /// </summary>
        Task<bool> AddProductsToBlockAsync(int blockId, IEnumerable<int> productIds);

        /// <summary>
        /// Bloktan ürün çıkarır
        /// </summary>
        Task<bool> RemoveProductFromBlockAsync(int blockId, int productId);

        /// <summary>
        /// Bloktaki ürünleri günceller (sıralama, aktiflik)
        /// </summary>
        Task<bool> UpdateBlockProductsAsync(UpdateBlockProductsDto dto);

        /// <summary>
        /// Bloktaki ürün listesini tamamen yeniler
        /// Önce tüm ürünler silinir, sonra yeni liste eklenir
        /// </summary>
        Task<bool> SetBlockProductsAsync(int blockId, IEnumerable<int> productIds);

        #endregion

        #region Validasyon ve Yardımcı

        /// <summary>
        /// Slug benzersiz mi kontrol eder
        /// </summary>
        Task<bool> IsSlugAvailableAsync(string slug, int? excludeBlockId = null);

        /// <summary>
        /// Blok tipine göre ürünleri getirir (preview için)
        /// </summary>
        Task<IEnumerable<HomeBlockProductItemDto>> GetProductsByBlockTypeAsync(string blockType, int? categoryId, int maxCount);

        #endregion
    }
}
