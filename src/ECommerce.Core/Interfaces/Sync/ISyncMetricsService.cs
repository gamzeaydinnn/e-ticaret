namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Mikro ↔ E-Ticaret senkronizasyon sağlık metrikleri ve alert sözleşmesi.
    /// 
    /// NEDEN: Senkronizasyon hataları sessizce birikebilir ve tutarsızlık oluşturabilir.
    /// Bu servis aktif izleme, metrik toplama ve eşik bazlı uyarı mekanizması sunar.
    /// 
    /// KULLANIM:
    /// - Admin dashboard sync health widget'ı
    /// - HotPoll/UnifiedSync sonrası otomatik metrik güncelleme
    /// - Ardışık hata eşiği aşıldığında admin bildirimi
    /// </summary>
    public interface ISyncMetricsService
    {
        /// <summary>
        /// Tüm sync kanallarının anlık sağlık özetini döner.
        /// Admin dashboard widget'ı için.
        /// </summary>
        Task<SyncHealthSummary> GetHealthSummaryAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir zaman aralığındaki sync metriklerini döner.
        /// Trend analizi ve performans takibi için.
        /// </summary>
        Task<SyncMetricsReport> GetMetricsAsync(
            int hours = 24,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktif alert'leri listeler. Admin dashboard uyarı göstergesi için.
        /// </summary>
        Task<List<SyncAlert>> GetActiveAlertsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Alert değerlendirmesi yapar — eşikleri kontrol eder ve yeni alert üretir.
        /// HotPoll/UnifiedSync sonrası otomatik çağrılır.
        /// </summary>
        Task EvaluateAlertsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Tüm sync kanallarının anlık sağlık özeti.
    /// </summary>
    public class SyncHealthSummary
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Genel sağlık durumu: Healthy, Degraded, Unhealthy</summary>
        public string OverallStatus { get; set; } = "Healthy";

        /// <summary>Kanal bazlı durum bilgileri</summary>
        public List<SyncChannelHealth> Channels { get; set; } = new();

        /// <summary>Aktif alert sayısı</summary>
        public int ActiveAlertCount { get; set; }

        /// <summary>Son 1 saatteki toplam hata sayısı</summary>
        public int RecentErrorCount { get; set; }

        /// <summary>Son 1 saatteki toplam başarılı sync sayısı</summary>
        public int RecentSuccessCount { get; set; }
    }

    /// <summary>
    /// Tek bir sync kanalının sağlık durumu.
    /// </summary>
    public class SyncChannelHealth
    {
        public string ChannelName { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown"; // Healthy, Degraded, Unhealthy
        public DateTime? LastSuccessTime { get; set; }
        public DateTime? LastFailureTime { get; set; }
        public int ConsecutiveFailures { get; set; }
        public long LastDurationMs { get; set; }
        public int Last24hSuccessCount { get; set; }
        public int Last24hFailureCount { get; set; }
        public decimal SuccessRate { get; set; }
    }

    /// <summary>
    /// Belirli bir periyottaki detaylı sync metrikleri.
    /// </summary>
    public class SyncMetricsReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public decimal OverallSuccessRate { get; set; }
        public long AvgDurationMs { get; set; }
        public long MaxDurationMs { get; set; }
        public long MinDurationMs { get; set; }
        public int TotalItemsSynced { get; set; }

        /// <summary>Saatlik kırılım (trend grafiği için)</summary>
        public List<HourlySyncMetric> HourlyBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Saatlik sync metrik verisi — trend grafiği için.
    /// </summary>
    public class HourlySyncMetric
    {
        public DateTime Hour { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public long AvgDurationMs { get; set; }
        public int ItemsSynced { get; set; }
    }

    /// <summary>
    /// Sync alert bilgisi.
    /// </summary>
    public class SyncAlert
    {
        public string AlertType { get; set; } = string.Empty; // ConsecutiveFailure, SyncDelay, HighErrorRate, LowStock
        public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
        public string Channel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
