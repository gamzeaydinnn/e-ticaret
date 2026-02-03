using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Başarısız sync işlemlerini yeniden deneyen Hangfire job'ı.
    /// 
    /// GÖREV: Her 5 dakikada bekleyen retry'ları işler.
    /// 
    /// ÇALIŞMA PRENSIBI:
    /// 1. Pending durumundaki logları al
    /// 2. Retry delay geçmiş mi kontrol et (exponential backoff)
    /// 3. Her log için ilgili sync servisini çağır
    /// 4. Başarılı/başarısız durumu güncelle
    /// 5. 3+ başarısız olanları Dead Letter'a taşı
    /// 
    /// CRON: */5 * * * * (Her 5 dakika)
    /// QUEUE: mikro-retry (ayrı kuyruk, sync'leri bloke etmesin)
    /// </summary>
    public class RetryJob : IMikroSyncJob
    {
        private readonly IRetryService _retryService;
        private readonly ILogger<RetryJob> _logger;

        /// <inheritdoc />
        public string JobName => "mikro-retry";

        /// <inheritdoc />
        public string Description => "Başarısız sync işlemlerini yeniden dener (5 dk)";

        public RetryJob(
            IRetryService retryService,
            ILogger<RetryJob> logger)
        {
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();

            _logger.LogInformation(
                "[{JobName}] Retry job başlıyor...",
                JobName);

            try
            {
                // Tüm entity tipleri için retry yap
                var retryResult = await _retryService.ProcessPendingRetriesAsync(
                    entityType: null, // Tüm tipler
                    maxItems: 50,     // Her seferde max 50 kayıt
                    cancellationToken: cancellationToken);

                result.Success = retryResult.IsSuccess;
                result.ProcessedCount = retryResult.TotalProcessed;
                result.SuccessCount = retryResult.SuccessCount;
                result.ErrorCount = retryResult.FailedCount;
                result.CompletedAt = DateTime.UtcNow;

                // Metadata
                result.Metadata["DeadLetterCount"] = retryResult.DeadLetterCount;
                result.Metadata["DurationMs"] = retryResult.DurationMs;

                if (retryResult.TotalProcessed == 0)
                {
                    result.Message = "Bekleyen retry yok";
                }
                else
                {
                    result.Message = $"Retry tamamlandı: {retryResult.SuccessCount}/{retryResult.TotalProcessed} başarılı";
                    
                    if (retryResult.DeadLetterCount > 0)
                    {
                        result.Message += $", {retryResult.DeadLetterCount} Dead Letter'a taşındı";
                    }
                }

                if (retryResult.Errors.Any())
                {
                    result.Errors.AddRange(retryResult.Errors);
                }

                _logger.LogInformation(
                    "[{JobName}] Retry tamamlandı. İşlenen: {Processed}, Başarılı: {Success}, " +
                    "Başarısız: {Failed}, Dead Letter: {DeadLetter}",
                    JobName, result.ProcessedCount, result.SuccessCount, 
                    result.ErrorCount, retryResult.DeadLetterCount);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobName}] Retry job iptal edildi", JobName);
                return JobResult.Failed("İşlem iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] Retry job sırasında hata",
                    JobName);

                return JobResult.Failed(
                    $"Retry hatası: {ex.Message}",
                    new List<string> { ex.ToString() });
            }
        }

        /// <summary>
        /// Belirli bir entity tipi için retry yapar.
        /// Dashboard'dan manuel tetikleme için.
        /// </summary>
        public async Task<JobResult> ExecuteForEntityTypeAsync(
            string entityType,
            CancellationToken cancellationToken = default)
        {
            var result = JobResult.Start();

            _logger.LogInformation(
                "[{JobName}] {EntityType} için retry başlıyor...",
                JobName, entityType);

            try
            {
                var retryResult = await _retryService.ProcessPendingRetriesAsync(
                    entityType: entityType,
                    maxItems: 100,
                    cancellationToken: cancellationToken);

                result.Success = retryResult.IsSuccess;
                result.ProcessedCount = retryResult.TotalProcessed;
                result.SuccessCount = retryResult.SuccessCount;
                result.ErrorCount = retryResult.FailedCount;
                result.CompletedAt = DateTime.UtcNow;
                result.Message = $"{entityType} retry: {retryResult.SuccessCount}/{retryResult.TotalProcessed}";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobName}] {EntityType} retry sırasında hata",
                    JobName, entityType);

                return JobResult.Failed($"{entityType} retry hatası: {ex.Message}");
            }
        }
    }
}
