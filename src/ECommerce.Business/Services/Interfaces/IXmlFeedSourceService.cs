using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.XmlImport;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// XML Feed Source servisi - tedarikçi XML feed kaynaklarını yönetir.
    /// Feed URL'leri, sync ayarları ve mapping konfigürasyonlarını içerir.
    /// </summary>
    public interface IXmlFeedSourceService
    {
        #region CRUD Operations
        
        /// <summary>
        /// Tüm feed kaynaklarını listeler.
        /// </summary>
        Task<IEnumerable<XmlFeedSourceDto>> GetAllAsync();
        
        /// <summary>
        /// Aktif feed kaynaklarını listeler.
        /// </summary>
        Task<IEnumerable<XmlFeedSourceDto>> GetActiveAsync();
        
        /// <summary>
        /// ID'ye göre feed kaynağı getirir.
        /// </summary>
        Task<XmlFeedSourceDto?> GetByIdAsync(int feedSourceId);
        
        /// <summary>
        /// Yeni feed kaynağı oluşturur.
        /// </summary>
        Task<XmlFeedSourceDto> CreateAsync(XmlFeedSourceCreateDto dto);
        
        /// <summary>
        /// Feed kaynağını günceller.
        /// </summary>
        Task<XmlFeedSourceDto> UpdateAsync(int feedSourceId, XmlFeedSourceCreateDto dto);
        
        /// <summary>
        /// Feed kaynağını siler.
        /// </summary>
        Task<bool> DeleteAsync(int feedSourceId);
        
        #endregion
        
        #region Activation & Status
        
        /// <summary>
        /// Feed kaynağını aktif/pasif yapar.
        /// </summary>
        Task<bool> SetActiveStatusAsync(int feedSourceId, bool isActive);
        
        /// <summary>
        /// Feed kaynağının durumunu getirir.
        /// </summary>
        Task<FeedSourceStatusDto?> GetStatusAsync(int feedSourceId);
        
        #endregion
        
        #region Sync Operations
        
        /// <summary>
        /// Sync zamanı gelmiş feed'leri listeler.
        /// </summary>
        Task<IEnumerable<XmlFeedSourceDto>> GetDueForSyncAsync();
        
        /// <summary>
        /// Sync başladığını işaretler.
        /// </summary>
        Task MarkSyncStartedAsync(int feedSourceId);
        
        /// <summary>
        /// Sync sonucunu günceller.
        /// </summary>
        Task UpdateSyncResultAsync(int feedSourceId, bool success, string? errorMessage = null);
        
        /// <summary>
        /// Son sync istatistiklerini günceller.
        /// </summary>
        Task UpdateStatisticsAsync(int feedSourceId, int totalProducts, int newProducts, int updatedProducts, int skippedProducts);
        
        #endregion
        
        #region Mapping Configuration
        
        /// <summary>
        /// Feed kaynağının mapping konfigürasyonunu getirir.
        /// </summary>
        Task<XmlMappingConfigDto?> GetMappingConfigAsync(int feedSourceId);
        
        /// <summary>
        /// Mapping konfigürasyonunu günceller.
        /// </summary>
        Task<bool> UpdateMappingConfigAsync(int feedSourceId, XmlMappingConfigDto config);
        
        /// <summary>
        /// XML örneğinden otomatik mapping önerir.
        /// </summary>
        Task<XmlMappingConfigDto> SuggestMappingAsync(int feedSourceId);
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Feed URL'sini test eder ve erişilebilirliğini kontrol eder.
        /// </summary>
        Task<FeedValidationResult> ValidateFeedUrlAsync(string feedUrl);
        
        /// <summary>
        /// Feed içeriğini parse eder ve örnek veri döndürür.
        /// </summary>
        Task<FeedPreviewResult> PreviewFeedAsync(int feedSourceId, int sampleSize = 5);
        
        #endregion
    }
    
    /// <summary>
    /// Feed source durum bilgisi.
    /// </summary>
    public class FeedSourceStatusDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime? NextSyncAt { get; set; }
        public bool? LastSyncSuccess { get; set; }
        public string? LastErrorMessage { get; set; }
        public int TotalProductsInFeed { get; set; }
        public int ActiveVariantsFromFeed { get; set; }
    }
    
    /// <summary>
    /// Feed URL doğrulama sonucu.
    /// </summary>
    public class FeedValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsReachable { get; set; }
        public bool IsValidXml { get; set; }
        public string? Error { get; set; }
        public int? EstimatedProductCount { get; set; }
        public IEnumerable<string>? DetectedFields { get; set; }
    }
    
    /// <summary>
    /// Feed önizleme sonucu.
    /// </summary>
    public class FeedPreviewResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int TotalProducts { get; set; }
        public IEnumerable<XmlProductItemDto> SampleProducts { get; set; } = new List<XmlProductItemDto>();
    }
}
