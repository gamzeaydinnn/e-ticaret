using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ana Sayfa Ürün Bloğu Repository Interface'i
    /// ------------------------------------------------
    /// HomeProductBlock ve HomeBlockProduct entity'leri için
    /// veritabanı işlemlerini tanımlar.
    /// 
    /// Performans Notları:
    /// - GetActiveBlocksWithProductsAsync: Include ile eager loading yapar
    /// - Kategori bazlı bloklar için ayrı ürün sorgusu gerekebilir
    /// </summary>
    public interface IHomeBlockRepository
    {
        #region HomeProductBlock CRUD

        /// <summary>
        /// Tüm blokları getirir (admin için)
        /// Banner ve Category navigation'ları dahil
        /// </summary>
        Task<IEnumerable<HomeProductBlock>> GetAllAsync();

        /// <summary>
        /// Aktif blokları DisplayOrder'a göre sıralı getirir
        /// Tarih kontrolü yapar (StartDate/EndDate)
        /// </summary>
        Task<IEnumerable<HomeProductBlock>> GetActiveBlocksAsync();

        /// <summary>
        /// Aktif blokları ürünleriyle birlikte getirir (ana sayfa için)
        /// Manuel bloklar: HomeBlockProduct tablosundan
        /// Kategori/discounted/newest: Product tablosundan filtreleme
        /// </summary>
        Task<IEnumerable<HomeProductBlock>> GetActiveBlocksWithProductsAsync();

        /// <summary>
        /// ID'ye göre tek blok getirir (detay için)
        /// </summary>
        Task<HomeProductBlock?> GetByIdAsync(int id);

        /// <summary>
        /// ID'ye göre blok ve ürünlerini getirir
        /// </summary>
        Task<HomeProductBlock?> GetByIdWithProductsAsync(int id);

        /// <summary>
        /// Slug'a göre blok getirir
        /// </summary>
        Task<HomeProductBlock?> GetBySlugAsync(string slug);

        /// <summary>
        /// Yeni blok ekler
        /// </summary>
        Task<HomeProductBlock> AddAsync(HomeProductBlock block);

        /// <summary>
        /// Mevcut bloğu günceller
        /// </summary>
        Task UpdateAsync(HomeProductBlock block);

        /// <summary>
        /// Bloğu siler (soft delete değil, hard delete)
        /// İlişkili HomeBlockProduct kayıtları da silinir (cascade)
        /// </summary>
        Task DeleteAsync(int id);

        #endregion

        #region HomeBlockProduct (Blok-Ürün İlişkisi)

        /// <summary>
        /// Bloğa ürün ekler
        /// Aynı ürün aynı bloğa tekrar eklenemez (duplicate check)
        /// </summary>
        Task AddProductToBlockAsync(int blockId, int productId, int displayOrder = 0);

        /// <summary>
        /// Bloğa birden fazla ürün ekler (toplu ekleme)
        /// </summary>
        Task AddProductsToBlockAsync(int blockId, IEnumerable<(int productId, int displayOrder)> products);

        /// <summary>
        /// Bloktan ürün çıkarır
        /// </summary>
        Task RemoveProductFromBlockAsync(int blockId, int productId);

        /// <summary>
        /// Bloktaki tüm ürünleri çıkarır
        /// </summary>
        Task ClearBlockProductsAsync(int blockId);

        /// <summary>
        /// Bloktaki ürün sıralamasını günceller
        /// </summary>
        Task UpdateProductOrderAsync(int blockId, int productId, int newDisplayOrder);

        /// <summary>
        /// Bloktaki ürünlerin sıralamasını toplu günceller
        /// </summary>
        Task UpdateBlockProductsOrderAsync(int blockId, IEnumerable<(int productId, int displayOrder, bool isActive)> products);

        /// <summary>
        /// Bloktaki ürün sayısını döndürür
        /// </summary>
        Task<int> GetBlockProductCountAsync(int blockId);

        /// <summary>
        /// Ürünün hangi bloklarda olduğunu getirir
        /// </summary>
        Task<IEnumerable<HomeProductBlock>> GetBlocksByProductIdAsync(int productId);

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Slug benzersiz mi kontrol eder
        /// </summary>
        Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null);

        /// <summary>
        /// Blok sıralamasını yeniden düzenler (gap'leri kapatır)
        /// </summary>
        Task ReorderBlocksAsync();

        #endregion
    }
}
