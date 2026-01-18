using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.XmlImport;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// XML Import servisi - tedarikçi XML feed'lerinden ürün ve varyant aktarımını yönetir.
    /// Bu servis, XML parse, mapping, import ve sync işlemlerini koordine eder.
    /// </summary>
    public interface IXmlImportService
    {
        #region Manual Import
        
        /// <summary>
        /// Belirli bir feed'den manuel import başlatır.
        /// </summary>
        Task<XmlImportResultDto> ImportFromFeedAsync(int feedSourceId, XmlImportRequestDto? options = null);
        
        /// <summary>
        /// URL'den doğrudan import yapar (tek seferlik).
        /// </summary>
        Task<XmlImportResultDto> ImportFromUrlAsync(string url, XmlMappingConfigDto mapping);
        
        /// <summary>
        /// Dosya içeriğinden import yapar (upload).
        /// </summary>
        Task<XmlImportResultDto> ImportFromContentAsync(string xmlContent, XmlMappingConfigDto mapping, int? feedSourceId = null);
        
        #endregion
        
        #region Scheduled Sync
        
        /// <summary>
        /// Zamanı gelmiş tüm feed'leri senkronize eder.
        /// Background job tarafından çağrılır.
        /// </summary>
        Task<IEnumerable<XmlImportResultDto>> SyncAllDueFeedsAsync();
        
        /// <summary>
        /// Belirli bir feed'i zorla senkronize eder.
        /// </summary>
        Task<XmlImportResultDto> ForceSyncFeedAsync(int feedSourceId);
        
        #endregion
        
        #region Import Progress & Status
        
        /// <summary>
        /// Devam eden import işlemlerini listeler.
        /// </summary>
        Task<IEnumerable<ImportProgressDto>> GetActiveImportsAsync();
        
        /// <summary>
        /// Import işleminin durumunu getirir.
        /// </summary>
        Task<ImportProgressDto?> GetImportProgressAsync(string importId);
        
        /// <summary>
        /// Import işlemini iptal eder.
        /// </summary>
        Task<bool> CancelImportAsync(string importId);
        
        #endregion
        
        #region Import History
        
        /// <summary>
        /// Import geçmişini listeler.
        /// </summary>
        Task<IEnumerable<XmlImportResultDto>> GetImportHistoryAsync(int? feedSourceId = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Belirli bir import'un detaylarını getirir.
        /// </summary>
        Task<XmlImportResultDto?> GetImportDetailAsync(string importId);
        
        #endregion
        
        #region Validation & Preview
        
        /// <summary>
        /// XML içeriğini doğrular ve hataları raporlar.
        /// </summary>
        Task<XmlValidationResultDto> ValidateXmlAsync(string xmlContent, XmlMappingConfigDto? mapping = null);
        
        /// <summary>
        /// Import önizlemesi yapar - veritabanını değiştirmeden.
        /// </summary>
        Task<XmlImportPreviewDto> PreviewImportAsync(int feedSourceId, int sampleSize = 10);
        
        #endregion
        
        #region Conflict Resolution
        
        /// <summary>
        /// Çakışan ürünleri listeler (aynı SKU, farklı veriler).
        /// </summary>
        Task<IEnumerable<ImportConflictDto>> GetConflictsAsync(string importId);
        
        /// <summary>
        /// Çakışmayı çözer.
        /// </summary>
        Task<bool> ResolveConflictAsync(string conflictId, ConflictResolution resolution);
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Belirli süredir güncellenmeyen (stale) ürünleri deaktif eder.
        /// </summary>
        Task<int> CleanupStaleProductsAsync(int feedSourceId, int hoursThreshold = 48);
        
        /// <summary>
        /// Feed'den kaldırılmış ürünleri işaretler.
        /// </summary>
        Task<int> MarkRemovedProductsAsync(int feedSourceId);
        
        #endregion
    }
    
    #region Supporting DTOs
    
    /// <summary>
    /// Import ilerleme durumu.
    /// </summary>
    public class ImportProgressDto
    {
        public string ImportId { get; set; } = string.Empty;
        public int FeedSourceId { get; set; }
        public string FeedSourceName { get; set; } = string.Empty;
        public ImportStatus Status { get; set; }
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int SkippedCount { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CurrentItem { get; set; }
        public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }
    
    /// <summary>
    /// Import durumu.
    /// </summary>
    public enum ImportStatus
    {
        Pending,
        Downloading,
        Parsing,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// XML doğrulama sonucu.
    /// </summary>
    public class XmlValidationResultDto
    {
        public bool IsValid { get; set; }
        public IEnumerable<XmlValidationError> Errors { get; set; } = new List<XmlValidationError>();
        public IEnumerable<XmlValidationWarning> Warnings { get; set; } = new List<XmlValidationWarning>();
        public int TotalProducts { get; set; }
        public int ValidProducts { get; set; }
        public int InvalidProducts { get; set; }
    }
    
    /// <summary>
    /// XML doğrulama hatası.
    /// </summary>
    public class XmlValidationError
    {
        public int Line { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
    
    /// <summary>
    /// XML doğrulama uyarısı.
    /// </summary>
    public class XmlValidationWarning
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
    }
    
    /// <summary>
    /// Import önizleme sonucu.
    /// </summary>
    public class XmlImportPreviewDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int TotalProducts { get; set; }
        public int NewProducts { get; set; }
        public int UpdatedProducts { get; set; }
        public int UnchangedProducts { get; set; }
        public IEnumerable<ProductPreviewDto> SampleProducts { get; set; } = new List<ProductPreviewDto>();
    }
    
    /// <summary>
    /// Ürün önizlemesi.
    /// </summary>
    public class ProductPreviewDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; } = string.Empty; // New, Update, Unchanged
        public IDictionary<string, string> Changes { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Import çakışması.
    /// </summary>
    public class ImportConflictDto
    {
        public string ConflictId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public ConflictType Type { get; set; }
        public IDictionary<string, ConflictValue> Fields { get; set; } = new Dictionary<string, ConflictValue>();
    }
    
    /// <summary>
    /// Çakışma tipi.
    /// </summary>
    public enum ConflictType
    {
        PriceChange,
        StockChange,
        NameChange,
        CategoryChange,
        MultipleFields
    }
    
    /// <summary>
    /// Çakışan değer.
    /// </summary>
    public class ConflictValue
    {
        public string CurrentValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Çakışma çözüm seçeneği.
    /// </summary>
    public enum ConflictResolution
    {
        KeepCurrent,
        UseNew,
        Merge,
        Skip
    }
    
    #endregion
}
