using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Tam senkronizasyon Hangfire job'ı.
    /// 
    /// GÖREV: Her gün 06:00'da tüm verileri senkronize eder.
    /// - Ürün bilgileri (yeni ürünler, güncellemeler)
    /// - Stok miktarları
    /// - Fiyat bilgileri
    /// - Pasif/aktif durumları
    /// 
    /// NEDEN 06:00: Mağaza açılmadan önce, trafik düşükken,
    /// günün en güncel verileriyle başlamak için.
    /// 
    /// SÜRE: Bu job diğerlerinden uzun sürer (5-15 dk),
    /// Hangfire timeout'u buna göre ayarlanmalı.
    /// </summary>
    public class MikroFullSyncJob : IFullSyncJob
    {
        private readonly IMikroSyncService _syncService;
        private readonly IStokSyncService _stokSyncService;
        private readonly IFiyatSyncService _fiyatSyncService;
        private readonly ILogger<MikroFullSyncJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-full-sync";

        /// <inheritdoc />
        public string Description => "Mikro ERP ile tam veri senkronizasyonu (günlük 06:00)";

        public MikroFullSyncJob(
            IMikroSyncService syncService,
            IStokSyncService stokSyncService,
            IFiyatSyncService fiyatSyncService,
            ILogger<MikroFullSyncJob> logger)
        {
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _stokSyncService = stokSyncService ?? throw new ArgumentNullException(nameof(stokSyncService));
            _fiyatSyncService = fiyatSyncService ?? throw new ArgumentNullException(nameof(fiyatSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();
            var errors = new List<string>();
            int totalProcessed = 0;
            int totalSuccess = 0;
            int totalError = 0;

            _logger.LogInformation(
                "[{JobName}] ═══════════════════════════════════════════════════════",
                JobName);
            _logger.LogInformation(
                "[{JobName}] TAM SENKRONİZASYON BAŞLIYOR",
                JobName);
            _logger.LogInformation(
                "[{JobName}] ═══════════════════════════════════════════════════════",
                JobName);

            try
            {
                // ═══════════════════════════════════════════════════════
                // ADIM 1: STOK SENKRONİZASYONU (Tüm Stoklar)
                // ═══════════════════════════════════════════════════════
                _logger.LogInformation("[{JobName}] ADIM 1/3: Stok senkronizasyonu...", JobName);

                try
                {
                    var stokResult = await _stokSyncService.SyncAllFromMikroAsync(cancellationToken);
                    totalProcessed += stokResult.ProcessedCount;
                    totalSuccess += stokResult.ProcessedCount - stokResult.Errors.Count;
                    totalError += stokResult.Errors.Count;

                    if (!stokResult.IsSuccess)
                    {
                        errors.AddRange(stokResult.Errors.Select(e => $"[Stok] {e.Message}"));
                    }

                    _logger.LogInformation(
                        "[{JobName}] Stok sync tamamlandı: {Success}/{Total}",
                        JobName, stokResult.ProcessedCount - stokResult.Errors.Count, stokResult.ProcessedCount);
                }
                catch (Exception ex)
                {
                    errors.Add($"[Stok] {ex.Message}");
                    _logger.LogError(ex, "[{JobName}] Stok senkronizasyonunda hata", JobName);
                }

                // İptal kontrolü
                cancellationToken.ThrowIfCancellationRequested();

                // ═══════════════════════════════════════════════════════
                // ADIM 2: FİYAT SENKRONİZASYONU (Tüm Fiyatlar)
                // ═══════════════════════════════════════════════════════
                _logger.LogInformation("[{JobName}] ADIM 2/3: Fiyat senkronizasyonu...", JobName);

                try
                {
                    var fiyatResult = await _fiyatSyncService.SyncAllFromMikroAsync(cancellationToken);
                    totalProcessed += fiyatResult.ProcessedCount;
                    totalSuccess += fiyatResult.ProcessedCount - fiyatResult.Errors.Count;
                    totalError += fiyatResult.Errors.Count;

                    if (!fiyatResult.IsSuccess)
                    {
                        errors.AddRange(fiyatResult.Errors.Select(e => $"[Fiyat] {e.Message}"));
                    }

                    _logger.LogInformation(
                        "[{JobName}] Fiyat sync tamamlandı: {Success}/{Total}",
                        JobName, fiyatResult.ProcessedCount - fiyatResult.Errors.Count, fiyatResult.ProcessedCount);
                }
                catch (Exception ex)
                {
                    errors.Add($"[Fiyat] {ex.Message}");
                    _logger.LogError(ex, "[{JobName}] Fiyat senkronizasyonunda hata", JobName);
                }

                // İptal kontrolü
                cancellationToken.ThrowIfCancellationRequested();

                // ═══════════════════════════════════════════════════════
                // ADIM 3: DURUM RAPORU OLUŞTUR
                // ═══════════════════════════════════════════════════════
                _logger.LogInformation("[{JobName}] ADIM 3/3: Durum raporu oluşturuluyor...", JobName);

                try
                {
                    var statusReport = await _syncService.GetSyncStatusAsync(cancellationToken);
                    
                    // SyncStates listesinden son sync zamanlarını al
                    var stokState = statusReport.SyncStates.FirstOrDefault(s => s.SyncType == "Stok");
                    var fiyatState = statusReport.SyncStates.FirstOrDefault(s => s.SyncType == "Fiyat");
                    
                    result.Metadata["StokSyncStatus"] = stokState?.LastSyncTime?.ToString("o") ?? "N/A";
                    result.Metadata["FiyatSyncStatus"] = fiyatState?.LastSyncTime?.ToString("o") ?? "N/A";
                    result.Metadata["OverallHealth"] = statusReport.IsHealthy ? "Healthy" : "Unhealthy";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{JobName}] Durum raporu oluşturulamadı", JobName);
                }

                // ═══════════════════════════════════════════════════════
                // SONUÇ
                // ═══════════════════════════════════════════════════════
                result.Success = errors.Count == 0;
                result.Message = errors.Count == 0
                    ? "Tam senkronizasyon başarıyla tamamlandı"
                    : $"Tam senkronizasyon tamamlandı, {errors.Count} hata var";
                result.ProcessedCount = totalProcessed;
                result.SuccessCount = totalSuccess;
                result.ErrorCount = totalError;
                result.Errors = errors;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "[{JobName}] ═══════════════════════════════════════════════════════",
                    JobName);
                _logger.LogInformation(
                    "[{JobName}] TAM SENKRONİZASYON TAMAMLANDI",
                    JobName);
                _logger.LogInformation(
                    "[{JobName}] Toplam: {Total}, Başarılı: {Success}, Hata: {Error}, Süre: {Duration}ms",
                    JobName, totalProcessed, totalSuccess, totalError, result.DurationMs);
                _logger.LogInformation(
                    "[{JobName}] ═══════════════════════════════════════════════════════",
                    JobName);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] Tam senkronizasyon iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Tam senkronizasyon sırasında kritik hata",
                    JobName);

                return JobResult.Failed(
                    $"Tam senkronizasyon hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteDeltaAsync(
            DateTime? sinceDate = null,
            CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();
            var effectiveSince = sinceDate ?? DateTime.UtcNow.AddDays(-1);

            _logger.LogInformation(
                "[{JobName}] Delta senkronizasyon başlıyor. Tarih: {Since}",
                JobName, effectiveSince);

            try
            {
                // Delta sync - sadece değişen kayıtlar

                // Stok delta
                var stokResult = await _stokSyncService.SyncDeltaFromMikroAsync(effectiveSince, cancellationToken);

                // Fiyat delta
                var fiyatResult = await _fiyatSyncService.SyncDeltaFromMikroAsync(effectiveSince, cancellationToken);

                var totalProcessed = stokResult.ProcessedCount + fiyatResult.ProcessedCount;
                var totalSuccess = (stokResult.ProcessedCount - stokResult.Errors.Count) + 
                                   (fiyatResult.ProcessedCount - fiyatResult.Errors.Count);
                var totalError = stokResult.Errors.Count + fiyatResult.Errors.Count;

                result.Success = stokResult.IsSuccess && fiyatResult.IsSuccess;
                result.ProcessedCount = totalProcessed;
                result.SuccessCount = totalSuccess;
                result.ErrorCount = totalError;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"Delta senkronizasyon tamamlandı ({effectiveSince:dd.MM.yyyy HH:mm}'den beri)";

                result.Metadata["SinceDate"] = effectiveSince.ToString("o");
                result.Metadata["StokDelta"] = stokResult.ProcessedCount;
                result.Metadata["FiyatDelta"] = fiyatResult.ProcessedCount;

                _logger.LogInformation(
                    "[{JobName}] Delta senkronizasyon tamamlandı. İşlenen: {Processed}",
                    JobName, result.ProcessedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Delta senkronizasyonda hata",
                    JobName);

                return JobResult.Failed(
                    $"Delta senkronizasyon hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }
    }
}
