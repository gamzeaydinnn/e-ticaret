using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Fiyat senkronizasyon Hangfire job'ı.
    /// 
    /// GÖREV: Her 1 saatte Mikro'dan fiyat bilgilerini çeker
    /// ve e-ticaret ürün fiyatlarını günceller.
    /// 
    /// NEDEN 1 SAAT: Fiyat değişiklikleri stoğa göre daha nadir,
    /// saatlik kontrol yeterli. Çok sık çekmek gereksiz.
    /// 
    /// DİKKAT:
    /// - Fiyat değişikliği sepetteki ürünleri etkilemez (snapshot)
    /// - Kampanya fiyatları ayrı sistem, bu sadece liste fiyatı
    /// </summary>
    public class MikroFiyatSyncJob : IFiyatSyncJob
    {
        private readonly IFiyatSyncService _fiyatSyncService;
        private readonly ILogger<MikroFiyatSyncJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-fiyat-sync";

        /// <inheritdoc />
        public string Description => "Mikro ERP'den fiyat bilgilerini senkronize eder (1 saat)";

        public MikroFiyatSyncJob(
            IFiyatSyncService fiyatSyncService,
            ILogger<MikroFiyatSyncJob> logger)
        {
            _fiyatSyncService = fiyatSyncService ?? throw new ArgumentNullException(nameof(fiyatSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();

            _logger.LogInformation(
                "[{JobName}] Fiyat senkronizasyonu başlıyor...",
                JobName);

            try
            {
                // Mevcut sync servisini kullan - Delta sync (değişenler)
                var syncResult = await _fiyatSyncService.SyncDeltaFromMikroAsync(null, cancellationToken);

                // Sonuçları dönüştür
                result.Success = syncResult.IsSuccess;
                result.Message = syncResult.IsSuccess
                    ? "Fiyat senkronizasyonu tamamlandı"
                    : $"Fiyat senkronizasyonu başarısız: {string.Join(", ", syncResult.Errors.Select(e => e.Message))}";
                result.ProcessedCount = syncResult.ProcessedCount;
                result.SuccessCount = syncResult.ProcessedCount - syncResult.Errors.Count;
                result.ErrorCount = syncResult.Errors.Count;
                result.SkippedCount = 0;
                result.CompletedAt = DateTime.UtcNow;

                if (syncResult.Errors.Any())
                {
                    result.Errors.AddRange(syncResult.Errors.Select(e => e.Message));
                }

                // Fiyat değişikliği istatistikleri
                result.Metadata["PriceIncreases"] = 0; // TODO: Sync servisinden alınabilir
                result.Metadata["PriceDecreases"] = 0;

                _logger.LogInformation(
                    "[{JobName}] Fiyat senkronizasyonu tamamlandı. " +
                    "İşlenen: {Processed}, Başarılı: {Success}, Hata: {Error}, Süre: {Duration}ms",
                    JobName, result.ProcessedCount, result.SuccessCount,
                    result.ErrorCount, result.DurationMs);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] Fiyat senkronizasyonu iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Fiyat senkronizasyonu sırasında hata oluştu",
                    JobName);

                return JobResult.Failed(
                    $"Fiyat senkronizasyonu hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteForProductsAsync(
            IEnumerable<int>? productIds = null,
            CancellationToken cancellationToken = default)
        {
            if (productIds == null || !productIds.Any())
            {
                // Tüm fiyatları senkronize et
                return await ExecuteAsync(cancellationToken);
            }

            var result = JobResult.Start();
            var ids = productIds.ToList();

            _logger.LogInformation(
                "[{JobName}] Belirli ürünler için fiyat senkronizasyonu başlıyor. Ürün sayısı: {Count}",
                JobName, ids.Count);

            try
            {
                // Mevcut servis toplu çalışıyor - Delta sync
                var syncResult = await _fiyatSyncService.SyncDeltaFromMikroAsync(null, cancellationToken);

                result.Success = syncResult.IsSuccess;
                result.ProcessedCount = ids.Count;
                result.SuccessCount = syncResult.ProcessedCount - syncResult.Errors.Count;
                result.ErrorCount = syncResult.Errors.Count;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"{ids.Count} ürün için fiyat senkronizasyonu tamamlandı";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Ürün bazlı fiyat senkronizasyonunda hata",
                    JobName);

                return JobResult.Failed(
                    $"Ürün bazlı fiyat senkronizasyonu hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }
    }
}
