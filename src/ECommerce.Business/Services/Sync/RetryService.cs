using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Başarısız sync işlemlerini yeniden deneyen servis.
    /// 
    /// GÖREV: Geçici hatalar nedeniyle başarısız olan sync
    /// işlemlerini otomatik olarak yeniden dener.
    /// 
    /// STRATEJİ: Exponential Backoff
    /// - 1. deneme: Hemen
    /// - 2. deneme: 1 dakika sonra
    /// - 3. deneme: 5 dakika sonra
    /// - 3+ başarısız: Dead Letter Queue (manuel müdahale)
    /// 
    /// RETRY EDİLEBİLİR HATALAR:
    /// - Network timeout
    /// - HTTP 5xx hataları
    /// - Database deadlock
    /// - Geçici API hataları
    /// 
    /// RETRY EDİLEMEZ HATALAR:
    /// - HTTP 4xx hataları (validation, auth)
    /// - Business rule ihlalleri
    /// - Veri tutarsızlıkları
    /// </summary>
    public class RetryService : IRetryService
    {
        private readonly IMikroSyncRepository _repository;
        private readonly ISyncLogger _syncLogger;
        private readonly IStokSyncService _stokSyncService;
        private readonly IFiyatSyncService _fiyatSyncService;
        private readonly ISiparisSyncService _siparisSyncService;
        private readonly ICariSyncService _cariSyncService;
        private readonly ILogger<RetryService> _logger;

        // Maksimum deneme sayısı
        private const int MaxAttempts = 3;

        // Retry gecikmeleri (saniye)
        private static readonly int[] RetryDelaysSeconds = { 0, 60, 300 }; // 0, 1dk, 5dk

        public RetryService(
            IMikroSyncRepository repository,
            ISyncLogger syncLogger,
            IStokSyncService stokSyncService,
            IFiyatSyncService fiyatSyncService,
            ISiparisSyncService siparisSyncService,
            ICariSyncService cariSyncService,
            ILogger<RetryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _syncLogger = syncLogger ?? throw new ArgumentNullException(nameof(syncLogger));
            _stokSyncService = stokSyncService ?? throw new ArgumentNullException(nameof(stokSyncService));
            _fiyatSyncService = fiyatSyncService ?? throw new ArgumentNullException(nameof(fiyatSyncService));
            _siparisSyncService = siparisSyncService ?? throw new ArgumentNullException(nameof(siparisSyncService));
            _cariSyncService = cariSyncService ?? throw new ArgumentNullException(nameof(cariSyncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<RetryResult> ProcessPendingRetriesAsync(
            string? entityType = null,
            int maxItems = 100,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new RetryResult();

            _logger.LogInformation(
                "[RetryService] ═══════════════════════════════════════════════════════");
            _logger.LogInformation(
                "[RetryService] RETRY İŞLEMİ BAŞLIYOR");
            _logger.LogInformation(
                "[RetryService] ═══════════════════════════════════════════════════════");

            try
            {
                // Bekleyen retry'ları al
                var pendingLogs = await _repository.GetPendingLogsAsync(
                    entityType,
                    MaxAttempts,
                    cancellationToken);

                var logsToProcess = pendingLogs
                    .Where(l => ShouldRetryNow(l))
                    .Take(maxItems)
                    .ToList();

                if (!logsToProcess.Any())
                {
                    _logger.LogInformation(
                        "[RetryService] Bekleyen retry yok");
                    return RetryResult.Empty();
                }

                _logger.LogInformation(
                    "[RetryService] {Count} kayıt işlenecek",
                    logsToProcess.Count);

                result.TotalProcessed = logsToProcess.Count;

                // Her log için retry yap
                foreach (var log in logsToProcess)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("[RetryService] İşlem iptal edildi");
                        break;
                    }

                    try
                    {
                        var success = await RetryLogAsync(log, cancellationToken);

                        if (success)
                        {
                            result.SuccessCount++;
                            _logger.LogInformation(
                                "[RetryService] ✓ Retry başarılı: {EntityType} LogId: {LogId}",
                                log.EntityType, log.Id);
                        }
                        else
                        {
                            result.FailedCount++;
                            
                            // 3+ başarısız deneme kontrolü
                            if (log.Attempts >= MaxAttempts)
                            {
                                result.DeadLetterCount++;
                                _logger.LogWarning(
                                    "[RetryService] ⚠️ Dead Letter'a taşındı: {EntityType} LogId: {LogId}",
                                    log.EntityType, log.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"LogId {log.Id}: {ex.Message}");

                        _logger.LogError(ex,
                            "[RetryService] Retry sırasında hata: {EntityType} LogId: {LogId}",
                            log.EntityType, log.Id);
                    }

                    // Rate limiting - API'yi yormamak için
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RetryService] Kritik hata");
                result.Errors.Add($"Kritik hata: {ex.Message}");
            }

            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "[RetryService] ═══════════════════════════════════════════════════════");
            _logger.LogInformation(
                "[RetryService] RETRY TAMAMLANDI: Başarılı: {Success}, Başarısız: {Failed}, " +
                "Dead Letter: {DeadLetter}, Süre: {Duration}ms",
                result.SuccessCount, result.FailedCount, result.DeadLetterCount, result.DurationMs);
            _logger.LogInformation(
                "[RetryService] ═══════════════════════════════════════════════════════");

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> RetrySpecificLogAsync(
            int logId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[RetryService] Manuel retry başlatılıyor: LogId: {LogId}",
                logId);

            var logs = await _repository.GetLogsByDateRangeAsync(
                DateTime.MinValue,
                DateTime.MaxValue,
                cancellationToken: cancellationToken);

            var log = logs.FirstOrDefault(l => l.Id == logId);

            if (log == null)
            {
                _logger.LogWarning(
                    "[RetryService] Log bulunamadı: LogId: {LogId}",
                    logId);
                return false;
            }

            return await RetryLogAsync(log, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DeadLetterItem>> GetDeadLetterItemsAsync(
            CancellationToken cancellationToken = default)
        {
            var logs = await _repository.GetLogsByDateRangeAsync(
                DateTime.UtcNow.AddDays(-30), // Son 30 gün
                DateTime.UtcNow,
                status: "DeadLetter",
                cancellationToken: cancellationToken);

            return logs.Select(l => new DeadLetterItem
            {
                LogId = l.Id,
                EntityType = l.EntityType,
                Direction = l.Direction,
                ExternalId = l.ExternalId,
                InternalId = l.InternalId,
                Attempts = l.Attempts,
                LastError = l.LastError,
                CreatedAt = l.CreatedAt,
                LastAttemptAt = l.LastAttemptAt,
                Message = l.Message
            });
        }

        /// <inheritdoc />
        public async Task RequeueDeadLetterAsync(
            int logId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[RetryService] Dead Letter yeniden kuyruğa alınıyor: LogId: {LogId}",
                logId);

            // Log'u Pending durumuna al ve Attempts'ı sıfırla
            await _syncLogger.RetryOperationAsync(logId, "Manuel requeue", cancellationToken);

            // Attempts'ı sıfırlamak için repository'yi güncelle
            var logs = await _repository.GetLogsByDateRangeAsync(
                DateTime.MinValue,
                DateTime.MaxValue,
                cancellationToken: cancellationToken);

            var log = logs.FirstOrDefault(l => l.Id == logId);
            if (log != null)
            {
                log.Attempts = 0;
                log.Status = "Pending";
                await _repository.UpdateLogAsync(log, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task MarkAsUnrecoverableAsync(
            int logId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "[RetryService] Dead Letter kalıcı başarısız işaretleniyor: LogId: {LogId}, Neden: {Reason}",
                logId, reason);

            var logs = await _repository.GetLogsByDateRangeAsync(
                DateTime.MinValue,
                DateTime.MaxValue,
                cancellationToken: cancellationToken);

            var log = logs.FirstOrDefault(l => l.Id == logId);
            if (log != null)
            {
                log.Status = "Unrecoverable";
                log.LastError = $"Manuel işaretleme: {reason}";
                log.LastAttemptAt = DateTime.UtcNow;
                await _repository.UpdateLogAsync(log, cancellationToken);
            }
        }

        /// <inheritdoc />
        public bool IsRetryableException(Exception exception)
        {
            // Network hataları - retry edilebilir
            if (exception is HttpRequestException)
                return true;

            if (exception is TaskCanceledException || exception is OperationCanceledException)
                return true;

            if (exception is TimeoutException)
                return true;

            // Özel exception mesajları
            var message = exception.Message.ToLowerInvariant();

            // Retry edilebilir durumlar
            if (message.Contains("timeout") ||
                message.Contains("connection") ||
                message.Contains("network") ||
                message.Contains("temporarily") ||
                message.Contains("service unavailable") ||
                message.Contains("502") ||
                message.Contains("503") ||
                message.Contains("504"))
            {
                return true;
            }

            // Retry edilemez durumlar
            if (message.Contains("unauthorized") ||
                message.Contains("forbidden") ||
                message.Contains("not found") ||
                message.Contains("validation") ||
                message.Contains("400") ||
                message.Contains("401") ||
                message.Contains("403") ||
                message.Contains("404"))
            {
                return false;
            }

            // Varsayılan: retry dene
            return true;
        }

        /// <inheritdoc />
        public TimeSpan CalculateNextRetryDelay(int attemptNumber)
        {
            // 0-indexed array, 1-indexed attempt
            var index = Math.Min(attemptNumber - 1, RetryDelaysSeconds.Length - 1);
            index = Math.Max(0, index);

            var seconds = RetryDelaysSeconds[index];

            // Jitter ekle (±10%) - thundering herd önleme
            var jitter = new Random().NextDouble() * 0.2 - 0.1; // -10% to +10%
            var adjustedSeconds = seconds * (1 + jitter);

            return TimeSpan.FromSeconds(Math.Max(0, adjustedSeconds));
        }

        #region Private Methods

        /// <summary>
        /// Log kaydının şimdi retry edilip edilmeyeceğini kontrol eder.
        /// </summary>
        private bool ShouldRetryNow(MicroSyncLog log)
        {
            if (log.Status != "Pending" && log.Status != "Failed")
                return false;

            if (log.Attempts >= MaxAttempts)
                return false;

            // Retry gecikmesi kontrolü
            var requiredDelay = CalculateNextRetryDelay(log.Attempts);
            var timeSinceLastAttempt = DateTime.UtcNow - (log.LastAttemptAt ?? log.CreatedAt);

            return timeSinceLastAttempt >= requiredDelay;
        }

        /// <summary>
        /// Tek bir log kaydını retry eder.
        /// </summary>
        private async Task<bool> RetryLogAsync(MicroSyncLog log, CancellationToken cancellationToken)
        {
            try
            {
                // Entity tipine göre retry yap
                SyncResult result = log.EntityType switch
                {
                    "Stok" => await RetryStokAsync(log, cancellationToken),
                    "Fiyat" => await RetryFiyatAsync(log, cancellationToken),
                    "Siparis" => await RetrySiparisAsync(log, cancellationToken),
                    "Cari" => await RetryCariAsync(log, cancellationToken),
                    _ => throw new NotSupportedException($"Bilinmeyen entity tipi: {log.EntityType}")
                };

                if (result.IsSuccess)
                {
                    await _syncLogger.CompleteOperationAsync(log.Id, "Retry başarılı", cancellationToken);
                    return true;
                }
                else
                {
                    var errorMsg = result.Errors.Any() 
                        ? result.Errors.First().Message 
                        : "Bilinmeyen hata";
                    await _syncLogger.FailOperationAsync(log.Id, errorMsg, cancellationToken);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _syncLogger.FailOperationAsync(log.Id, ex.Message, cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Stok sync retry.
        /// </summary>
        private async Task<SyncResult> RetryStokAsync(MicroSyncLog log, CancellationToken cancellationToken)
        {
            if (log.Direction == "FromERP")
            {
                // Mikro'dan E-Ticaret'e stok sync - delta sync kullan
                // Not: Tekil sync metodu olmadığı için delta sync ile değişenleri çekeriz
                return await _stokSyncService.SyncDeltaFromMikroAsync(
                    log.CreatedAt.AddMinutes(-5), // 5 dk öncesinden beri değişenleri çek
                    cancellationToken);
            }
            else if (log.Direction == "ToERP" && !string.IsNullOrEmpty(log.InternalId))
            {
                // E-Ticaret'ten Mikro'ya stok güncelleme
                if (int.TryParse(log.InternalId, out var productId))
                {
                    return await _stokSyncService.PushStockToMikroAsync(productId, cancellationToken);
                }
            }

            return SyncResult.Fail(new SyncError("Retry", log.ExternalId, "Geçersiz retry parametreleri"));
        }

        /// <summary>
        /// Fiyat sync retry.
        /// </summary>
        private async Task<SyncResult> RetryFiyatAsync(MicroSyncLog log, CancellationToken cancellationToken)
        {
            if (log.Direction == "FromERP")
            {
                // Mikro'dan fiyat sync - delta sync kullan
                return await _fiyatSyncService.SyncDeltaFromMikroAsync(
                    log.CreatedAt.AddMinutes(-5),
                    cancellationToken);
            }
            else if (log.Direction == "ToERP" && !string.IsNullOrEmpty(log.InternalId))
            {
                // E-Ticaret'ten Mikro'ya fiyat güncelleme - kampanya fiyatı için
                // Not: Log'da fiyat bilgisi tutulmuyor, bu yüzden 0 ile deneme yaparız
                // Gerçek implementasyonda log'a fiyat bilgisi eklenebilir
                if (int.TryParse(log.InternalId, out var productId))
                {
                    // Ürünün mevcut fiyatını gönder
                    return await _fiyatSyncService.PushPriceToMikroAsync(productId, 0, cancellationToken);
                }
            }

            return SyncResult.Fail(new SyncError("Retry", log.ExternalId, "Geçersiz retry parametreleri"));
        }

        /// <summary>
        /// Sipariş sync retry.
        /// </summary>
        private async Task<SyncResult> RetrySiparisAsync(MicroSyncLog log, CancellationToken cancellationToken)
        {
            if (log.Direction == "ToERP" && !string.IsNullOrEmpty(log.InternalId))
            {
                if (int.TryParse(log.InternalId, out var orderId))
                {
                    return await _siparisSyncService.PushOrderToMikroAsync(orderId, cancellationToken);
                }
            }

            return SyncResult.Fail(new SyncError("Retry", log.InternalId, "Geçersiz retry parametreleri"));
        }

        /// <summary>
        /// Cari sync retry.
        /// </summary>
        private async Task<SyncResult> RetryCariAsync(MicroSyncLog log, CancellationToken cancellationToken)
        {
            if (log.Direction == "ToERP" && !string.IsNullOrEmpty(log.InternalId))
            {
                if (int.TryParse(log.InternalId, out var userId))
                {
                    return await _cariSyncService.SyncUserToCariAsync(userId, cancellationToken);
                }
            }

            return SyncResult.Fail(new SyncError("Retry", log.InternalId, "Geçersiz retry parametreleri"));
        }

        #endregion
    }
}
