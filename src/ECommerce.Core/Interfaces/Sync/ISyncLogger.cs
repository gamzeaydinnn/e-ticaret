using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Sync işlemleri için merkezi loglama servisi.
    /// 
    /// NEDEN: Tüm Mikro ↔ E-Ticaret sync işlemlerini tek bir yerden
    /// tutarlı şekilde loglamak için. Bu sayede:
    /// - Hata takibi kolaylaşır
    /// - Retry mekanizması çalışır
    /// - Admin panelinde raporlama yapılabilir
    /// - Audit trail sağlanır
    /// 
    /// KULLANIM:
    /// 1. Sync başlamadan önce StartOperation çağrılır (log kaydı oluşur)
    /// 2. İşlem sonunda CompleteOperation veya FailOperation çağrılır
    /// 3. Retry gerekirse RetryOperation çağrılır
    /// </summary>
    public interface ISyncLogger
    {
        // ==================== TEMEL LOG İŞLEMLERİ ====================

        /// <summary>
        /// Yeni bir sync operasyonu başlatır ve log kaydı oluşturur.
        /// </summary>
        /// <param name="entityType">Entity tipi: Stok, Fiyat, Siparis, Cari</param>
        /// <param name="direction">Yön: ToERP veya FromERP</param>
        /// <param name="externalId">Mikro tarafındaki ID (sto_kod, cari_kod vb.)</param>
        /// <param name="internalId">E-Ticaret tarafındaki ID (ProductId, OrderId vb.)</param>
        /// <param name="message">Açıklayıcı mesaj</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Oluşturulan log kaydı</returns>
        Task<MicroSyncLog> StartOperationAsync(
            string entityType,
            string direction,
            string? externalId = null,
            string? internalId = null,
            string? message = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Operasyonu başarılı olarak tamamlar.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="message">Başarı mesajı</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task CompleteOperationAsync(
            int logId,
            string? message = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Operasyonu başarısız olarak işaretler.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="error">Hata mesajı veya exception</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task FailOperationAsync(
            int logId,
            string error,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Operasyonu yeniden deneme için işaretler.
        /// </summary>
        /// <param name="logId">Log kaydı ID'si</param>
        /// <param name="reason">Retry nedeni</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task RetryOperationAsync(
            int logId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        // ==================== TOPLU LOG İŞLEMLERİ ====================

        /// <summary>
        /// Birden fazla entity için tek seferde log başlatır.
        /// Batch işlemler için performans optimizasyonu.
        /// </summary>
        /// <param name="items">Log edilecek item'lar</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Oluşturulan log kayıtları</returns>
        Task<IEnumerable<MicroSyncLog>> StartBatchOperationAsync(
            IEnumerable<SyncLogItem> items,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Toplu operasyonu tamamlar.
        /// </summary>
        /// <param name="logIds">Tamamlanacak log ID'leri</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task CompleteBatchOperationAsync(
            IEnumerable<int> logIds,
            CancellationToken cancellationToken = default);

        // ==================== ÇAKIŞMA LOGLAMA ====================

        /// <summary>
        /// Çakışma durumunu loglar.
        /// </summary>
        /// <typeparam name="T">Entity tipi</typeparam>
        /// <param name="context">Çakışma bağlamı</param>
        /// <param name="result">Çözüm sonucu</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task LogConflictAsync<T>(
            ConflictContext<T> context,
            ConflictResolutionResult<T> result,
            CancellationToken cancellationToken = default) where T : class;

        // ==================== SORGULAMA ====================

        /// <summary>
        /// Retry bekleyen kayıtları getirir.
        /// </summary>
        /// <param name="entityType">Entity tipi filtresi (opsiyonel)</param>
        /// <param name="maxAttempts">Maksimum deneme sayısı</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<IEnumerable<MicroSyncLog>> GetPendingRetryLogsAsync(
            string? entityType = null,
            int maxAttempts = 3,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Son N saatteki hataları getirir.
        /// </summary>
        /// <param name="hours">Saat sayısı</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<IEnumerable<MicroSyncLog>> GetRecentFailuresAsync(
            int hours = 24,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sync istatistiklerini getirir.
        /// </summary>
        /// <param name="since">Bu tarihten itibaren</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        Task<SyncStatistics> GetStatisticsAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Batch log için item tanımı.
    /// </summary>
    public class SyncLogItem
    {
        public string EntityType { get; set; } = string.Empty;
        public string Direction { get; set; } = "FromERP";
        public string? ExternalId { get; set; }
        public string? InternalId { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Sync istatistikleri.
    /// </summary>
    public class SyncStatistics
    {
        /// <summary>İstatistik başlangıç tarihi.</summary>
        public DateTime Since { get; set; }

        /// <summary>Toplam sync operasyonu sayısı.</summary>
        public int TotalOperations { get; set; }

        /// <summary>Başarılı operasyon sayısı.</summary>
        public int SuccessfulOperations { get; set; }

        /// <summary>Başarısız operasyon sayısı.</summary>
        public int FailedOperations { get; set; }

        /// <summary>Bekleyen retry sayısı.</summary>
        public int PendingRetries { get; set; }

        /// <summary>Çakışma sayısı.</summary>
        public int ConflictCount { get; set; }

        /// <summary>Entity tipine göre dağılım.</summary>
        public Dictionary<string, EntitySyncStats> ByEntityType { get; set; } = new();

        /// <summary>Yöne göre dağılım.</summary>
        public Dictionary<string, int> ByDirection { get; set; } = new();

        /// <summary>Başarı oranı (%).</summary>
        public decimal SuccessRate => TotalOperations > 0 
            ? Math.Round((decimal)SuccessfulOperations / TotalOperations * 100, 2) 
            : 0;
    }

    /// <summary>
    /// Entity tipi bazında istatistikler.
    /// </summary>
    public class EntitySyncStats
    {
        public string EntityType { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public int Pending { get; set; }
        public DateTime? LastSyncAt { get; set; }
    }
}
