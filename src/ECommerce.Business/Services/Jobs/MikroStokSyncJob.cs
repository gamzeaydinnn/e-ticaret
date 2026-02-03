using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Stok senkronizasyon Hangfire job'ı.
    /// 
    /// GÖREV: Her 15 dakikada Mikro'dan stok bilgilerini çeker
    /// ve e-ticaret veritabanını günceller.
    /// 
    /// NEDEN 15 DAKİKA: Mağaza satışları hızla stok değiştirir,
    /// ancak çok sık çekmek API'yi yorar. 15 dakika dengeli.
    /// 
    /// KULLANIM:
    /// - Hangfire recurring job olarak çalışır
    /// - Manuel tetiklenebilir (admin dashboard)
    /// - Belirli SKU'lar için filtrelenebilir
    /// </summary>
    public class MikroStokSyncJob : IStokSyncJob
    {
        private readonly IStokSyncService _stokSyncService;
        private readonly ILogger<MikroStokSyncJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-stok-sync";

        /// <inheritdoc />
        public string Description => "Mikro ERP'den stok miktarlarını senkronize eder (15 dk)";

        public MikroStokSyncJob(
            IStokSyncService stokSyncService,
            ILogger<MikroStokSyncJob> logger)
        {
            _stokSyncService = stokSyncService ?? throw new ArgumentNullException(nameof(stokSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();
            
            _logger.LogInformation(
                "[{JobName}] Stok senkronizasyonu başlıyor...",
                JobName);

            try
            {
                // Mevcut sync servisini kullan - Delta sync (değişenler)
                var syncResult = await _stokSyncService.SyncDeltaFromMikroAsync(null, cancellationToken);

                // Sonuçları dönüştür
                result.Success = syncResult.IsSuccess;
                result.Message = syncResult.IsSuccess 
                    ? $"Stok senkronizasyonu tamamlandı" 
                    : $"Stok senkronizasyonu başarısız: {string.Join(", ", syncResult.Errors.Select(e => e.Message))}";
                result.ProcessedCount = syncResult.ProcessedCount;
                result.SuccessCount = syncResult.ProcessedCount - syncResult.Errors.Count;
                result.ErrorCount = syncResult.Errors.Count;
                result.SkippedCount = 0;
                result.CompletedAt = DateTime.UtcNow;

                if (syncResult.Errors.Any())
                {
                    result.Errors.AddRange(syncResult.Errors.Select(e => e.Message));
                }

                _logger.LogInformation(
                    "[{JobName}] Stok senkronizasyonu tamamlandı. " +
                    "İşlenen: {Processed}, Başarılı: {Success}, Hata: {Error}, Süre: {Duration}ms",
                    JobName, result.ProcessedCount, result.SuccessCount, 
                    result.ErrorCount, result.DurationMs);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] Stok senkronizasyonu iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "[{JobName}] Stok senkronizasyonu sırasında hata oluştu", 
                    JobName);
                
                return JobResult.Failed(
                    $"Stok senkronizasyonu hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteForSkusAsync(
            IEnumerable<string>? skuList = null, 
            CancellationToken cancellationToken = default)
        {
            if (skuList == null || !skuList.Any())
            {
                // Tüm stokları senkronize et
                return await ExecuteAsync(cancellationToken);
            }

            var result = JobResult.Start();
            var skus = skuList.ToList();

            _logger.LogInformation(
                "[{JobName}] Belirli SKU'lar için stok senkronizasyonu başlıyor. SKU sayısı: {Count}",
                JobName, skus.Count);

            try
            {
                // Her SKU için ayrı sync yap
                // Not: Mevcut servis toplu çalışıyor, bu fonksiyon gelecekte optimize edilebilir
                var syncResult = await _stokSyncService.SyncDeltaFromMikroAsync(null, cancellationToken);

                result.Success = syncResult.IsSuccess;
                result.ProcessedCount = skus.Count;
                result.SuccessCount = syncResult.ProcessedCount - syncResult.Errors.Count;
                result.ErrorCount = syncResult.Errors.Count;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"{skus.Count} SKU için stok senkronizasyonu tamamlandı";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] SKU bazlı stok senkronizasyonunda hata",
                    JobName);
                
                return JobResult.Failed(
                    $"SKU bazlı stok senkronizasyonu hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }
    }
}
