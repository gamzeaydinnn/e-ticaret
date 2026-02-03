namespace ECommerce.Core.Interfaces.Jobs
{
    /// <summary>
    /// Mikro ERP senkronizasyon job'ları için temel interface.
    /// 
    /// NEDEN: Hangfire job'ları için ortak bir sözleşme sağlar.
    /// Bu sayede tüm job'lar tutarlı bir yapıda çalışır.
    /// 
    /// ÖZELLİKLER:
    /// - Retry mekanizması desteği
    /// - CancellationToken desteği
    /// - İlerleme takibi
    /// </summary>
    public interface IMikroSyncJob
    {
        /// <summary>
        /// Job'ın benzersiz adı.
        /// Hangfire dashboard'da görünür.
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Job'ın açıklaması.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Job'ı çalıştırır.
        /// </summary>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Job sonucu</returns>
        Task<JobResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Stok senkronizasyon job'ı.
    /// Her 15 dakikada çalışır.
    /// Mikro'dan güncel stok bilgilerini çeker.
    /// </summary>
    public interface IStokSyncJob : IMikroSyncJob
    {
        /// <summary>
        /// Belirli SKU'lar için stok senkronizasyonu yapar.
        /// </summary>
        /// <param name="skuList">SKU listesi (boş ise tümü)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> ExecuteForSkusAsync(
            IEnumerable<string>? skuList = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Fiyat senkronizasyon job'ı.
    /// Her 1 saatte çalışır.
    /// Mikro'dan güncel fiyat bilgilerini çeker.
    /// </summary>
    public interface IFiyatSyncJob : IMikroSyncJob
    {
        /// <summary>
        /// Belirli ürünler için fiyat senkronizasyonu yapar.
        /// </summary>
        /// <param name="productIds">Ürün ID listesi (boş ise tümü)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> ExecuteForProductsAsync(
            IEnumerable<int>? productIds = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Tam senkronizasyon job'ı.
    /// Her gün 06:00'da çalışır.
    /// Tüm ürün, stok ve fiyat verilerini senkronize eder.
    /// </summary>
    public interface IFullSyncJob : IMikroSyncJob
    {
        /// <summary>
        /// Delta senkronizasyon yapar.
        /// Sadece belirli tarihten sonra değişen kayıtları senkronize eder.
        /// </summary>
        /// <param name="sinceDate">Bu tarihten sonraki değişiklikler</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> ExecuteDeltaAsync(
            DateTime? sinceDate = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sipariş push job'ı.
    /// Event-driven çalışır.
    /// Online siparişleri Mikro'ya gönderir.
    /// </summary>
    public interface ISiparisPushJob : IMikroSyncJob
    {
        /// <summary>
        /// Tek bir siparişi Mikro'ya gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID'si</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> PushOrderAsync(
            int orderId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bekleyen tüm siparişleri Mikro'ya gönderir.
        /// Retry mekanizması ile başarısız olanları tekrar dener.
        /// </summary>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> PushPendingOrdersAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Müşteri senkronizasyon job'ı.
    /// Yeni müşterileri Mikro'ya kaydeder.
    /// </summary>
    public interface ICariSyncJob : IMikroSyncJob
    {
        /// <summary>
        /// Tek bir müşteriyi Mikro'ya kaydeder.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<JobResult> SyncCustomerAsync(
            int userId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Job sonuç sınıfı.
    /// Job çalışmasının detaylı sonucunu içerir.
    /// </summary>
    public class JobResult
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Sonuç mesajı.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// İşlenen kayıt sayısı.
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// Başarılı kayıt sayısı.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Hatalı kayıt sayısı.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Atlanan kayıt sayısı (değişiklik yok).
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// İşlem başlangıç zamanı.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// İşlem bitiş zamanı.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// İşlem süresi (ms).
        /// </summary>
        public long DurationMs => CompletedAt.HasValue 
            ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds 
            : 0;

        /// <summary>
        /// Hata detayları.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Ek bilgiler.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Başarılı sonuç oluşturur.
        /// </summary>
        public static JobResult Successful(string message, int processed = 0, int success = 0)
        {
            return new JobResult
            {
                Success = true,
                Message = message,
                ProcessedCount = processed,
                SuccessCount = success,
                CompletedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Başarısız sonuç oluşturur.
        /// </summary>
        public static JobResult Failed(string message, List<string>? errors = null)
        {
            return new JobResult
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                CompletedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Yeni job başlatır.
        /// </summary>
        public static JobResult Start()
        {
            return new JobResult
            {
                StartedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Job scheduler interface'i.
    /// Hangfire recurring job'larını yönetir.
    /// </summary>
    public interface IMikroJobScheduler
    {
        /// <summary>
        /// Tüm recurring job'ları kaydeder.
        /// Uygulama başlangıcında çağrılır.
        /// </summary>
        void RegisterAllJobs();

        /// <summary>
        /// Belirli bir job'ı hemen tetikler.
        /// Dashboard'dan veya API'den tetikleme için.
        /// </summary>
        /// <param name="jobName">Job adı</param>
        /// <returns>Hangfire job ID</returns>
        string TriggerJob(string jobName);

        /// <summary>
        /// Belirli bir job'ı devre dışı bırakır.
        /// </summary>
        /// <param name="jobName">Job adı</param>
        void DisableJob(string jobName);

        /// <summary>
        /// Belirli bir job'ı aktif eder.
        /// </summary>
        /// <param name="jobName">Job adı</param>
        void EnableJob(string jobName);

        /// <summary>
        /// Tüm job durumlarını getirir.
        /// </summary>
        Task<IEnumerable<JobStatusInfo>> GetJobStatusesAsync();
    }

    /// <summary>
    /// Job durum bilgisi.
    /// </summary>
    public class JobStatusInfo
    {
        public string JobName { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string? LastStatus { get; set; }
        public bool IsEnabled { get; set; }
    }
}
