using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// MikroSyncState ve MicroSyncLog repository interface'i.
    /// 
    /// NEDEN: Sync durumlarını ve loglarını veritabanında yönetmek için.
    /// Unit of Work pattern ile kullanılabilir.
    /// </summary>
    public interface IMikroSyncRepository
    {
        // ==================== SYNC STATE İŞLEMLERİ ====================

        /// <summary>
        /// Belirli bir sync tipi için son durumu getirir.
        /// </summary>
        /// <param name="syncType">Stok, Fiyat, Siparis, Cari</param>
        /// <param name="direction">ToERP veya FromERP</param>
        Task<MikroSyncState?> GetSyncStateAsync(
            string syncType, 
            string direction, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sync durumunu günceller veya oluşturur (upsert).
        /// </summary>
        Task<MikroSyncState> UpsertSyncStateAsync(
            MikroSyncState state, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Başarılı sync sonrası durumu günceller.
        /// Kısa yol metodu - sık kullanılan senaryo için.
        /// </summary>
        Task UpdateSyncSuccessAsync(
            string syncType,
            string direction,
            int processedCount,
            long durationMs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Başarısız sync sonrası durumu günceller.
        /// </summary>
        Task UpdateSyncFailureAsync(
            string syncType,
            string direction,
            string errorMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tüm sync durumlarını listeler (admin panel için).
        /// </summary>
        Task<IEnumerable<MikroSyncState>> GetAllSyncStatesAsync(
            CancellationToken cancellationToken = default);

        // ==================== SYNC LOG İŞLEMLERİ ====================

        /// <summary>
        /// Yeni sync log kaydı oluşturur.
        /// </summary>
        Task<MicroSyncLog> CreateLogAsync(
            MicroSyncLog log, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log kaydını günceller (retry durumunda).
        /// </summary>
        Task UpdateLogAsync(
            MicroSyncLog log, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir entity için son log kaydını getirir.
        /// </summary>
        Task<MicroSyncLog?> GetLastLogAsync(
            string entityType,
            string? externalId = null,
            string? internalId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bekleyen (Pending) log kayıtlarını getirir.
        /// Retry job'ı için kullanılır.
        /// </summary>
        Task<IEnumerable<MicroSyncLog>> GetPendingLogsAsync(
            string? entityType = null,
            int maxAttempts = 3,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir tarih aralığındaki logları getirir.
        /// Admin panel için raporlama.
        /// </summary>
        Task<IEnumerable<MicroSyncLog>> GetLogsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? entityType = null,
            string? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hatalı logların sayısını döndürür.
        /// Dashboard widget için.
        /// </summary>
        Task<int> GetFailedLogCountAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default);
    }
}
