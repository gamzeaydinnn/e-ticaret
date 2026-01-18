using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Product;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Product Variant servisi - SKU bazlı varyant yönetimi.
    /// Stok, fiyat ve option değerlerini yönetir.
    /// </summary>
    public interface IProductVariantService
    {
        #region CRUD Operations
        
        /// <summary>
        /// Varyant ID'sine göre detay getirir.
        /// </summary>
        Task<ProductVariantDetailDto?> GetByIdAsync(int variantId);
        
        /// <summary>
        /// SKU'ya göre varyant getirir.
        /// </summary>
        Task<ProductVariantDetailDto?> GetBySkuAsync(string sku);
        
        /// <summary>
        /// Bir ürüne ait tüm varyantları listeler.
        /// </summary>
        Task<IEnumerable<ProductVariantDetailDto>> GetByProductIdAsync(int productId);
        
        /// <summary>
        /// Yeni varyant oluşturur.
        /// </summary>
        Task<ProductVariantDetailDto> CreateAsync(int productId, ProductVariantCreateDto dto);
        
        /// <summary>
        /// Mevcut varyantı günceller.
        /// </summary>
        Task<ProductVariantDetailDto> UpdateAsync(int variantId, ProductVariantUpdateDto dto);
        
        /// <summary>
        /// Varyantı siler (soft delete).
        /// </summary>
        Task<bool> DeleteAsync(int variantId);
        
        #endregion
        
        #region Stock Operations
        
        /// <summary>
        /// Stok miktarını günceller.
        /// </summary>
        Task<bool> UpdateStockAsync(int variantId, int newStock);
        
        /// <summary>
        /// Stok miktarını artırır veya azaltır.
        /// </summary>
        Task<bool> AdjustStockAsync(int variantId, int adjustment, string reason);
        
        /// <summary>
        /// Stoğu düşük olan varyantları listeler.
        /// </summary>
        Task<IEnumerable<ProductVariantDetailDto>> GetLowStockVariantsAsync(int threshold = 5);
        
        /// <summary>
        /// Stok kontrolü yapar - yeterli stok var mı?
        /// </summary>
        Task<bool> CheckStockAvailabilityAsync(int variantId, int requiredQuantity);
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// Toplu stok güncellemesi yapar.
        /// </summary>
        Task<int> BulkUpdateStockAsync(IDictionary<string, int> skuStockMap);
        
        /// <summary>
        /// Toplu fiyat güncellemesi yapar.
        /// </summary>
        Task<int> BulkUpdatePricesAsync(IDictionary<string, decimal> skuPriceMap);
        
        /// <summary>
        /// Aktif olmayan (belirli süredir görülmeyen) varyantları deaktif eder.
        /// </summary>
        Task<int> DeactivateStaleVariantsAsync(int feedSourceId, int hoursThreshold = 48);
        
        #endregion
        
        #region Option Management
        
        /// <summary>
        /// Varyanta option değeri ekler.
        /// </summary>
        Task AddOptionValueAsync(int variantId, int optionId, int optionValueId);
        
        /// <summary>
        /// Varyanttan option değeri kaldırır.
        /// </summary>
        Task RemoveOptionValueAsync(int variantId, int optionId);
        
        /// <summary>
        /// Varyantın tüm option değerlerini günceller.
        /// </summary>
        Task UpdateOptionValuesAsync(int variantId, IEnumerable<(int OptionId, int ValueId)> optionValues);
        
        #endregion
        
        #region Query & Statistics
        
        /// <summary>
        /// Varyant istatistiklerini getirir.
        /// </summary>
        Task<VariantStatisticsDto> GetStatisticsAsync(int? feedSourceId = null);
        
        /// <summary>
        /// Seçilen option'lara göre uygun varyantı bulur.
        /// </summary>
        Task<ProductVariantDetailDto?> FindByOptionsAsync(int productId, IDictionary<int, int> optionValueMap);
        
        #endregion
    }
    
    /// <summary>
    /// Varyant istatistikleri DTO.
    /// </summary>
    public class VariantStatisticsDto
    {
        public int TotalVariants { get; set; }
        public int ActiveVariants { get; set; }
        public int InactiveVariants { get; set; }
        public int OutOfStockVariants { get; set; }
        public int LowStockVariants { get; set; }
        public decimal TotalStockValue { get; set; }
    }
}
