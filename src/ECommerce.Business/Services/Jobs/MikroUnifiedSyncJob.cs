using ECommerce.Core.Interfaces.Cache;
using ECommerce.Core.Interfaces.Jobs;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Birleşik stok + fiyat + ürün bilgisi senkronizasyon Hangfire job'ı.
    /// 
    /// GÖREV: Her 15 dakikada TEK SQL sorgusuyla web-aktif ürünleri Mikro ERP'den çeker
    /// ve MikroProductCache tablosunu günceller.
    /// 
    /// NEDEN TEK JOB:
    /// - Eski: MikroStokSyncJob (15dk) + MikroFiyatSyncJob (1sa) = 2 ayrı job, 3+ API çağrısı
    /// - Yeni: MikroUnifiedSyncJob (15dk) = 1 job, 1 SQL sorgusu
    /// - Mikro ERP yükü %80+ azalır, timeout riski ortadan kalkar
    /// 
    /// KULLANIM:
    /// - Hangfire recurring job olarak çalışır (her 15 dakika)
    /// - Manuel tetiklenebilir (admin dashboard)
    /// </summary>
    public class MikroUnifiedSyncJob : IUnifiedSyncJob
    {
        private readonly IMikroProductCacheService _cacheService;
        private readonly ILogger<MikroUnifiedSyncJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-unified-sync";

        /// <inheritdoc />
        public string Description => "Mikro ERP'den tek SQL ile stok + fiyat + ürün bilgisi senkronize eder (15 dk)";

        public MikroUnifiedSyncJob(
            IMikroProductCacheService cacheService,
            ILogger<MikroUnifiedSyncJob> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();

            _logger.LogInformation(
                "[{JobName}] Birleşik senkronizasyon başlıyor (Unified SQL)...",
                JobName);

            try
            {
                // Tek SQL sorgusuyla tüm web-aktif ürünleri çek ve cache'e yaz
                var syncResult = await _cacheService.FetchAllAndCacheAsync(
                    fiyatListesiNo: 1,
                    depoNo: 0);

                result.Success = syncResult.Success;
                result.Message = syncResult.Success
                    ? $"Birleşik sync tamamlandı. {syncResult.Message}"
                    : $"Birleşik sync başarısız: {syncResult.Message}";
                result.ProcessedCount = syncResult.TotalFetched;
                result.SuccessCount = syncResult.NewProducts + syncResult.UpdatedProducts + syncResult.UnchangedProducts;
                result.ErrorCount = syncResult.Errors.Count;
                result.SkippedCount = syncResult.UnchangedProducts;
                result.CompletedAt = DateTime.UtcNow;

                if (syncResult.Errors.Any())
                {
                    result.Errors.AddRange(syncResult.Errors);
                }

                // Metadata — monitoring ve dashboard için
                result.Metadata["NewProducts"] = syncResult.NewProducts;
                result.Metadata["UpdatedProducts"] = syncResult.UpdatedProducts;
                result.Metadata["UnchangedProducts"] = syncResult.UnchangedProducts;
                result.Metadata["DurationSeconds"] = (int)syncResult.Duration.TotalSeconds;
                result.Metadata["SyncMethod"] = "SQL_UNIFIED";

                // ADIM 2: Cache → Product tablosu senkronizasyonu
                // NEDEN: Frontend Product tablosundan okur. Cache güncellendiğinde
                // Product.Price ve Product.StockQuantity da güncellenmelidir.
                if (syncResult.Success)
                {
                    var productUpdated = await _cacheService.SyncCacheToProductTableAsync();
                    result.Metadata["ProductTableUpdated"] = productUpdated;

                    _logger.LogInformation(
                        "[{JobName}] Cache → Product tablosu sync tamamlandı. Güncellenen: {Count}",
                        JobName, productUpdated);
                }

                _logger.LogInformation(
                    "[{JobName}] Birleşik sync tamamlandı. " +
                    "Toplam: {Total}, Yeni: {New}, Güncellenen: {Updated}, Değişmeyen: {Unchanged}, Süre: {Duration}s",
                    JobName,
                    syncResult.TotalFetched,
                    syncResult.NewProducts,
                    syncResult.UpdatedProducts,
                    syncResult.UnchangedProducts,
                    (int)syncResult.Duration.TotalSeconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] Birleşik sync iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Birleşik sync sırasında hata oluştu",
                    JobName);

                return JobResult.Failed(
                    $"Birleşik sync hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }
    }
}
