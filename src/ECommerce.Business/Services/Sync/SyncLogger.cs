using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Sync işlemleri için merkezi loglama servisi implementasyonu.
    /// 
    /// GÖREV: Tüm Mikro ↔ E-Ticaret sync işlemlerini veritabanına
    /// kaydeder, durumlarını takip eder ve raporlama sağlar.
    /// 
    /// KULLANIM AKIŞI:
    /// 1. Sync başında: StartOperationAsync → Log kaydı oluşur (Status: Pending)
    /// 2. Başarılı: CompleteOperationAsync → Status: Completed
    /// 3. Başarısız: FailOperationAsync → Status: Failed, Attempts++
    /// 4. Retry: RetryOperationAsync → Status: Pending, sonraki deneme için
    /// 
    /// NOT: Bu servis transactional değildir. Her log işlemi bağımsız commit edilir.
    /// </summary>
    public class SyncLogger : ISyncLogger
    {
        private readonly IMikroSyncRepository _repository;
        private readonly ILogger<SyncLogger> _logger;

        // Log durumları
        private const string StatusPending = "Pending";
        private const string StatusCompleted = "Completed";
        private const string StatusFailed = "Failed";
        private const string StatusDeadLetter = "DeadLetter";

        public SyncLogger(
            IMikroSyncRepository repository,
            ILogger<SyncLogger> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Temel Log İşlemleri

        /// <inheritdoc />
        public async Task<MicroSyncLog> StartOperationAsync(
            string entityType,
            string direction,
            string? externalId = null,
            string? internalId = null,
            string? message = null,
            CancellationToken cancellationToken = default)
        {
            var log = new MicroSyncLog
            {
                EntityType = entityType,
                Direction = direction,
                ExternalId = externalId,
                InternalId = internalId,
                Status = StatusPending,
                Attempts = 1,
                LastAttemptAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Message = message ?? $"{entityType} sync başlatıldı"
            };

            var createdLog = await _repository.CreateLogAsync(log, cancellationToken);

            _logger.LogDebug(
                "[SyncLogger] Operasyon başlatıldı: {EntityType} {Direction}, " +
                "External: {ExternalId}, Internal: {InternalId}, LogId: {LogId}",
                entityType, direction, externalId, internalId, createdLog.Id);

            return createdLog;
        }

        /// <inheritdoc />
        public async Task CompleteOperationAsync(
            int logId,
            string? message = null,
            CancellationToken cancellationToken = default)
        {
            var log = await GetLogByIdAsync(logId, cancellationToken);
            if (log == null)
            {
                _logger.LogWarning("[SyncLogger] Log bulunamadı: {LogId}", logId);
                return;
            }

            log.Status = StatusCompleted;
            log.LastAttemptAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(message))
            {
                log.Message = message;
            }
            log.LastError = null; // Başarılı olunca hata temizlenir

            await _repository.UpdateLogAsync(log, cancellationToken);

            _logger.LogDebug(
                "[SyncLogger] Operasyon tamamlandı: {EntityType} {Direction}, LogId: {LogId}",
                log.EntityType, log.Direction, logId);
        }

        /// <inheritdoc />
        public async Task FailOperationAsync(
            int logId,
            string error,
            CancellationToken cancellationToken = default)
        {
            var log = await GetLogByIdAsync(logId, cancellationToken);
            if (log == null)
            {
                _logger.LogWarning("[SyncLogger] Log bulunamadı: {LogId}", logId);
                return;
            }

            log.Status = StatusFailed;
            log.LastAttemptAt = DateTime.UtcNow;
            log.LastError = TruncateError(error);

            // 3+ deneme sonrası Dead Letter'a taşı
            if (log.Attempts >= 3)
            {
                log.Status = StatusDeadLetter;
                log.Message = $"Dead Letter: {log.Attempts} başarısız deneme sonrası";

                _logger.LogWarning(
                    "[SyncLogger] ⚠️ Dead Letter'a taşındı: {EntityType} {Direction}, " +
                    "LogId: {LogId}, Hata: {Error}",
                    log.EntityType, log.Direction, logId, error);
            }
            else
            {
                _logger.LogWarning(
                    "[SyncLogger] Operasyon başarısız (deneme {Attempt}/3): {EntityType} {Direction}, " +
                    "LogId: {LogId}, Hata: {Error}",
                    log.Attempts, log.EntityType, log.Direction, logId, error);
            }

            await _repository.UpdateLogAsync(log, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RetryOperationAsync(
            int logId,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            var log = await GetLogByIdAsync(logId, cancellationToken);
            if (log == null)
            {
                _logger.LogWarning("[SyncLogger] Log bulunamadı: {LogId}", logId);
                return;
            }

            log.Status = StatusPending;
            log.Attempts++;
            log.LastAttemptAt = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(reason))
            {
                log.Message = $"Retry #{log.Attempts}: {reason}";
            }

            await _repository.UpdateLogAsync(log, cancellationToken);

            _logger.LogInformation(
                "[SyncLogger] Retry kuyruğa alındı (deneme {Attempt}): {EntityType} {Direction}, LogId: {LogId}",
                log.Attempts, log.EntityType, log.Direction, logId);
        }

        #endregion

        #region Toplu Log İşlemleri

        /// <inheritdoc />
        public async Task<IEnumerable<MicroSyncLog>> StartBatchOperationAsync(
            IEnumerable<SyncLogItem> items,
            CancellationToken cancellationToken = default)
        {
            var logs = new List<MicroSyncLog>();
            var itemList = items.ToList();

            _logger.LogDebug(
                "[SyncLogger] Batch operasyon başlatılıyor: {Count} kayıt",
                itemList.Count);

            foreach (var item in itemList)
            {
                var log = await StartOperationAsync(
                    item.EntityType,
                    item.Direction,
                    item.ExternalId,
                    item.InternalId,
                    item.Message,
                    cancellationToken);

                logs.Add(log);
            }

            return logs;
        }

        /// <inheritdoc />
        public async Task CompleteBatchOperationAsync(
            IEnumerable<int> logIds,
            CancellationToken cancellationToken = default)
        {
            var idList = logIds.ToList();

            _logger.LogDebug(
                "[SyncLogger] Batch operasyon tamamlanıyor: {Count} kayıt",
                idList.Count);

            foreach (var logId in idList)
            {
                await CompleteOperationAsync(logId, cancellationToken: cancellationToken);
            }
        }

        #endregion

        #region Çakışma Loglama

        /// <inheritdoc />
        public async Task LogConflictAsync<T>(
            ConflictContext<T> context,
            ConflictResolutionResult<T> result,
            CancellationToken cancellationToken = default) where T : class
        {
            var conflictDetails = string.Join("; ", result.FieldConflicts.Select(c =>
                $"{c.FieldName}: {c.SourceValue} vs {c.TargetValue}" +
                (c.PercentDifference.HasValue ? $" (%{c.PercentDifference:F1} fark)" : "")));

            var message = result.HadConflict
                ? $"Çakışma çözüldü ({result.Strategy}): {result.Reason}. Detay: {conflictDetails}"
                : "Çakışma yok";

            var log = new MicroSyncLog
            {
                EntityType = context.EntityType,
                Direction = context.Direction,
                ExternalId = context.Identifier,
                Status = "Conflict",
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                LastAttemptAt = DateTime.UtcNow,
                Message = message
            };

            await _repository.CreateLogAsync(log, cancellationToken);

            if (result.HadConflict)
            {
                _logger.LogInformation(
                    "[SyncLogger] Çakışma kaydedildi: {EntityType} {Identifier}, " +
                    "Kazanan: {Winner}, Strateji: {Strategy}",
                    context.EntityType, context.Identifier, result.Winner, result.Strategy);
            }
        }

        #endregion

        #region Sorgulama

        /// <inheritdoc />
        public async Task<IEnumerable<MicroSyncLog>> GetPendingRetryLogsAsync(
            string? entityType = null,
            int maxAttempts = 3,
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetPendingLogsAsync(entityType, maxAttempts, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MicroSyncLog>> GetRecentFailuresAsync(
            int hours = 24,
            CancellationToken cancellationToken = default)
        {
            var since = DateTime.UtcNow.AddHours(-hours);
            return await _repository.GetLogsByDateRangeAsync(
                since,
                DateTime.UtcNow,
                status: StatusFailed,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task<SyncStatistics> GetStatisticsAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var startDate = since ?? DateTime.UtcNow.AddDays(-7);
            var logs = await _repository.GetLogsByDateRangeAsync(
                startDate,
                DateTime.UtcNow,
                cancellationToken: cancellationToken);

            var logList = logs.ToList();

            var stats = new SyncStatistics
            {
                Since = startDate,
                TotalOperations = logList.Count,
                SuccessfulOperations = logList.Count(l => l.Status == StatusCompleted),
                FailedOperations = logList.Count(l => l.Status == StatusFailed || l.Status == StatusDeadLetter),
                PendingRetries = logList.Count(l => l.Status == StatusPending && l.Attempts > 1),
                ConflictCount = logList.Count(l => l.Status == "Conflict")
            };

            // Entity tipine göre grupla
            var byEntityType = logList.GroupBy(l => l.EntityType);
            foreach (var group in byEntityType)
            {
                stats.ByEntityType[group.Key] = new EntitySyncStats
                {
                    EntityType = group.Key,
                    Total = group.Count(),
                    Success = group.Count(l => l.Status == StatusCompleted),
                    Failed = group.Count(l => l.Status == StatusFailed || l.Status == StatusDeadLetter),
                    Pending = group.Count(l => l.Status == StatusPending),
                    LastSyncAt = group.Max(l => l.LastAttemptAt)
                };
            }

            // Yöne göre grupla
            var byDirection = logList.GroupBy(l => l.Direction);
            foreach (var group in byDirection)
            {
                stats.ByDirection[group.Key] = group.Count();
            }

            return stats;
        }

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Log ID'sine göre log kaydını getirir.
        /// </summary>
        private async Task<MicroSyncLog?> GetLogByIdAsync(
            int logId,
            CancellationToken cancellationToken)
        {
            // Repository'de GetByIdAsync yok, GetPendingLogsAsync ile al ve filtrele
            // veya direkt DbContext kullan
            var logs = await _repository.GetLogsByDateRangeAsync(
                DateTime.MinValue,
                DateTime.MaxValue,
                cancellationToken: cancellationToken);

            return logs.FirstOrDefault(l => l.Id == logId);
        }

        /// <summary>
        /// Hata mesajını maksimum uzunluğa kısaltır.
        /// </summary>
        private static string TruncateError(string error, int maxLength = 2000)
        {
            if (string.IsNullOrEmpty(error)) return string.Empty;
            if (error.Length <= maxLength) return error;
            return error.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
