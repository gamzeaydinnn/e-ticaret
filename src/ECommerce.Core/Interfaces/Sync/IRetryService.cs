namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Başarısız sync işlemlerini yeniden deneyen servis interface'i.
    /// 
    /// NEDEN: Geçici hatalar (network, timeout vb.) nedeniyle başarısız olan
    /// işlemleri otomatik olarak tekrar denemek için. Exponential backoff
    /// stratejisi ile sistem aşırı yüklenmeden retry yapılır.
    /// 
    /// RETRY STRATEJİSİ:
    /// - 1. deneme: Hemen
    /// - 2. deneme: 1 dakika sonra
    /// - 3. deneme: 5 dakika sonra
    /// - 3 başarısız deneme sonrası: Dead Letter (manuel müdahale)
    /// 
    /// KULLANIM:
    /// - Hangfire job olarak her 5 dakikada çalışır
    /// - Admin panelinden manuel tetiklenebilir
    /// </summary>
    public interface IRetryService
    {
        /// <summary>
        /// Tüm bekleyen retry'ları işler.
        /// </summary>
        /// <param name="entityType">Filtre: sadece bu entity tipini işle (opsiyonel)</param>
        /// <param name="maxItems">Maksimum işlenecek kayıt sayısı</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>İşlem sonucu</returns>
        Task<RetryResult> ProcessPendingRetriesAsync(
            string? entityType = null,
            int maxItems = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir log kaydını retry eder.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Başarılı mı?</returns>
        Task<bool> RetrySpecificLogAsync(
            int logId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dead letter kayıtlarını getirir (3+ başarısız deneme).
        /// </summary>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<IEnumerable<DeadLetterItem>> GetDeadLetterItemsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dead letter kaydını yeniden kuyruğa alır.
        /// Admin müdahalesi sonrası kullanılır.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task RequeueDeadLetterAsync(
            int logId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dead letter kaydını kalıcı olarak başarısız işaretler.
        /// Manuel inceleme sonrası "düzeltilemez" olarak işaretleme.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="reason">Neden düzeltilemez?</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task MarkAsUnrecoverableAsync(
            int logId,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bir işlemin retry edilebilir olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="exception">Oluşan exception</param>
        /// <returns>Retry edilmeli mi?</returns>
        bool IsRetryableException(Exception exception);

        /// <summary>
        /// Sonraki retry için bekleme süresini hesaplar.
        /// Exponential backoff algoritması kullanır.
        /// </summary>
        /// <param name="attemptNumber">Mevcut deneme numarası (1-based)</param>
        /// <returns>Bekleme süresi</returns>
        TimeSpan CalculateNextRetryDelay(int attemptNumber);
    }

    /// <summary>
    /// Retry işlem sonucu.
    /// </summary>
    public class RetryResult
    {
        /// <summary>İşlenen toplam kayıt sayısı.</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Başarıyla tamamlanan sayısı.</summary>
        public int SuccessCount { get; set; }

        /// <summary>Yine başarısız olan sayısı.</summary>
        public int FailedCount { get; set; }

        /// <summary>Dead letter'a taşınan sayısı.</summary>
        public int DeadLetterCount { get; set; }

        /// <summary>İşlem süresi (ms).</summary>
        public long DurationMs { get; set; }

        /// <summary>Hata mesajları.</summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>Başarılı mı? (en az bir başarı varsa).</summary>
        public bool IsSuccess => SuccessCount > 0 || TotalProcessed == 0;

        /// <summary>Boş sonuç oluşturur.</summary>
        public static RetryResult Empty() => new()
        {
            TotalProcessed = 0,
            SuccessCount = 0,
            FailedCount = 0,
            DeadLetterCount = 0
        };
    }

    /// <summary>
    /// Dead letter (ölü mektup) kuyruğundaki item.
    /// 3+ başarısız deneme sonrası buraya düşer.
    /// </summary>
    public class DeadLetterItem
    {
        /// <summary>Log kaydı ID'si.</summary>
        public int LogId { get; set; }

        /// <summary>Entity tipi.</summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Yön.</summary>
        public string Direction { get; set; } = string.Empty;

        /// <summary>Harici ID (Mikro tarafı).</summary>
        public string? ExternalId { get; set; }

        /// <summary>Dahili ID (E-Ticaret tarafı).</summary>
        public string? InternalId { get; set; }

        /// <summary>Deneme sayısı.</summary>
        public int Attempts { get; set; }

        /// <summary>Son hata mesajı.</summary>
        public string? LastError { get; set; }

        /// <summary>İlk oluşturulma zamanı.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Son deneme zamanı.</summary>
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>Açıklama mesajı.</summary>
        public string? Message { get; set; }
    }
}
