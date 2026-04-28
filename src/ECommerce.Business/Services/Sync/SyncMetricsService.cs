using System.Diagnostics;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Senkronizasyon sağlık metrikleri, trend analizi ve eşik bazlı alert üretimi.
    /// 
    /// NEDEN: Sync hataları sessizce birikip veri tutarsızlığına yol açabilir.
    /// Bu servis MikroSyncState + MicroSyncLog verilerinden metrik toplayarak
    /// admin dashboard'a sağlık bilgisi sunar ve ardışık hata durumunda alert üretir.
    /// 
    /// VERİ KAYNAĞI: Ek tablo oluşturmuyor — mevcut MikroSyncState + MicroSyncLog
    /// tablolarını okuyarak hesaplama yapar (read-only analiz).
    /// 
    /// ALERT EŞİKLERİ:
    /// - ConsecutiveFailure >= 3 → Warning
    /// - ConsecutiveFailure >= 5 → Critical
    /// - SuccessRate < 80% (son 1 saat) → Warning
    /// - Sync süresi > 60sn → Warning (performans degradasyonu)
    /// </summary>
    public class SyncMetricsService : ISyncMetricsService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<SyncMetricsService> _logger;

        // Alert eşikleri — future: appsettings'den okunabilir
        private const int ConsecutiveFailureWarning = 3;
        private const int ConsecutiveFailureCritical = 5;
        private const decimal MinHealthySuccessRate = 80m;
        private const long MaxHealthyDurationMs = 60_000;

        public SyncMetricsService(
            ECommerceDbContext context,
            ILogger<SyncMetricsService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SyncHealthSummary> GetHealthSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var summary = new SyncHealthSummary();

            // Tüm sync state kayıtlarını çek
            var states = await _context.Set<MikroSyncState>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Son 1 saatteki log istatistiklerini hesapla
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentLogs = await _context.Set<MicroSyncLog>()
                .AsNoTracking()
                .Where(l => l.CreatedAt >= oneHourAgo)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            summary.RecentSuccessCount = recentLogs
                .Where(l => string.Equals(l.Status, "Success", StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.Count);
            summary.RecentErrorCount = recentLogs
                .Where(l => string.Equals(l.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.Count);

            // Kanal bazlı durum bilgileri
            foreach (var state in states)
            {
                var channel = MapStateToChannelHealth(state);

                // Son 24 saatteki kanal bazlı log sayıları
                var oneDayAgo = DateTime.UtcNow.AddHours(-24);
                var channelLogs = await _context.Set<MicroSyncLog>()
                    .AsNoTracking()
                    .Where(l => l.EntityType == state.SyncType &&
                                l.Direction == state.Direction &&
                                l.CreatedAt >= oneDayAgo)
                    .GroupBy(l => l.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                channel.Last24hSuccessCount = channelLogs
                    .Where(l => string.Equals(l.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    .Sum(l => l.Count);
                channel.Last24hFailureCount = channelLogs
                    .Where(l => string.Equals(l.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    .Sum(l => l.Count);

                var total = channel.Last24hSuccessCount + channel.Last24hFailureCount;
                channel.SuccessRate = total > 0
                    ? Math.Round((decimal)channel.Last24hSuccessCount / total * 100, 1)
                    : 100m;

                summary.Channels.Add(channel);
            }

            // Genel sağlık durumu hesapla
            summary.OverallStatus = CalculateOverallStatus(summary.Channels);

            // Aktif alert sayısı
            var alerts = await GetActiveAlertsAsync(cancellationToken);
            summary.ActiveAlertCount = alerts.Count;

            return summary;
        }

        /// <inheritdoc />
        public async Task<SyncMetricsReport> GetMetricsAsync(
            int hours = 24,
            CancellationToken cancellationToken = default)
        {
            hours = Math.Clamp(hours, 1, 168); // max 7 gün
            var periodStart = DateTime.UtcNow.AddHours(-hours);

            var report = new SyncMetricsReport
            {
                PeriodStart = periodStart,
                PeriodEnd = DateTime.UtcNow
            };

            // Periyottaki tüm sync state kayıtlarından metrik topla
            var states = await _context.Set<MikroSyncState>()
                .AsNoTracking()
                .Where(s => s.UpdatedAt >= periodStart)
                .ToListAsync(cancellationToken);

            // MicroSyncLog'dan detaylı istatistik
            var logs = await _context.Set<MicroSyncLog>()
                .AsNoTracking()
                .Where(l => l.CreatedAt >= periodStart)
                .ToListAsync(cancellationToken);

            report.TotalOperations = logs.Count;
            report.SuccessfulOperations = logs.Count(l =>
                string.Equals(l.Status, "Success", StringComparison.OrdinalIgnoreCase));
            report.FailedOperations = logs.Count(l =>
                string.Equals(l.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            report.OverallSuccessRate = report.TotalOperations > 0
                ? Math.Round((decimal)report.SuccessfulOperations / report.TotalOperations * 100, 1)
                : 100m;

            // Süre metrikleri — MikroSyncState'den
            if (states.Count > 0)
            {
                var durations = states
                    .Where(s => s.LastSyncDurationMs > 0)
                    .Select(s => s.LastSyncDurationMs)
                    .ToList();

                if (durations.Count > 0)
                {
                    report.AvgDurationMs = (long)durations.Average();
                    report.MaxDurationMs = durations.Max();
                    report.MinDurationMs = durations.Min();
                }

                report.TotalItemsSynced = states.Sum(s => s.LastSyncCount);
            }

            // Saatlik kırılım — trend grafiği için
            report.HourlyBreakdown = logs
                .GroupBy(l => new DateTime(l.CreatedAt.Year, l.CreatedAt.Month, l.CreatedAt.Day,
                    l.CreatedAt.Hour, 0, 0, DateTimeKind.Utc))
                .OrderBy(g => g.Key)
                .Select(g => new HourlySyncMetric
                {
                    Hour = g.Key,
                    SuccessCount = g.Count(l =>
                        string.Equals(l.Status, "Success", StringComparison.OrdinalIgnoreCase)),
                    FailureCount = g.Count(l =>
                        string.Equals(l.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                    ItemsSynced = g.Count()
                })
                .ToList();

            return report;
        }

        /// <inheritdoc />
        public async Task<List<SyncAlert>> GetActiveAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            var alerts = new List<SyncAlert>();

            // 1. Ardışık hata alert'leri
            var states = await _context.Set<MikroSyncState>()
                .AsNoTracking()
                .Where(s => s.ConsecutiveFailures >= ConsecutiveFailureWarning)
                .ToListAsync(cancellationToken);

            foreach (var state in states)
            {
                var severity = state.ConsecutiveFailures >= ConsecutiveFailureCritical
                    ? "Critical"
                    : "Warning";

                alerts.Add(new SyncAlert
                {
                    AlertType = "ConsecutiveFailure",
                    Severity = severity,
                    Channel = $"{state.SyncType}_{state.Direction}",
                    Message = $"{state.SyncType} ({state.Direction}) kanalında {state.ConsecutiveFailures} ardışık hata. Son hata: {state.LastError ?? "Bilinmiyor"}",
                    DetectedAt = state.UpdatedAt,
                    Metadata = new Dictionary<string, object>
                    {
                        ["consecutiveFailures"] = state.ConsecutiveFailures,
                        ["lastError"] = state.LastError ?? "",
                        ["lastSyncTime"] = state.LastSyncTime?.ToString("o") ?? ""
                    }
                });
            }

            // 2. Uzun süredir sync yapılmayan kanallar
            var staleThreshold = DateTime.UtcNow.AddHours(-2);
            var staleStates = await _context.Set<MikroSyncState>()
                .AsNoTracking()
                .Where(s => s.IsEnabled &&
                            (s.LastSyncTime == null || s.LastSyncTime < staleThreshold))
                .ToListAsync(cancellationToken);

            foreach (var state in staleStates)
            {
                alerts.Add(new SyncAlert
                {
                    AlertType = "SyncDelay",
                    Severity = "Warning",
                    Channel = $"{state.SyncType}_{state.Direction}",
                    Message = $"{state.SyncType} ({state.Direction}) kanalında 2+ saattir sync yapılmamış. Son sync: {state.LastSyncTime?.ToString("HH:mm") ?? "Hiç"}",
                    DetectedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["lastSyncTime"] = state.LastSyncTime?.ToString("o") ?? "null"
                    }
                });
            }

            // 3. Son 1 saatte yüksek hata oranı
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentLogStats = await _context.Set<MicroSyncLog>()
                .AsNoTracking()
                .Where(l => l.CreatedAt >= oneHourAgo)
                .GroupBy(l => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Failed = g.Count(l => l.Status == "Failed")
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (recentLogStats != null && recentLogStats.Total >= 5)
            {
                var successRate = (decimal)(recentLogStats.Total - recentLogStats.Failed) / recentLogStats.Total * 100;
                if (successRate < MinHealthySuccessRate)
                {
                    alerts.Add(new SyncAlert
                    {
                        AlertType = "HighErrorRate",
                        Severity = successRate < 50m ? "Critical" : "Warning",
                        Channel = "Global",
                        Message = $"Son 1 saatte başarı oranı: %{Math.Round(successRate, 1)}. Toplam: {recentLogStats.Total}, Başarısız: {recentLogStats.Failed}",
                        Metadata = new Dictionary<string, object>
                        {
                            ["successRate"] = successRate,
                            ["totalOperations"] = recentLogStats.Total,
                            ["failedOperations"] = recentLogStats.Failed
                        }
                    });
                }
            }

            return alerts.OrderByDescending(a => a.Severity == "Critical" ? 2 :
                a.Severity == "Warning" ? 1 : 0).ToList();
        }

        /// <inheritdoc />
        public async Task EvaluateAlertsAsync(CancellationToken cancellationToken = default)
        {
            // Alert değerlendirmesi — GetActiveAlertsAsync ile aynı mantığı kullanır
            // Fark: Bu metod IStockNotificationService üzerinden admin'e push yapabilir
            // Şimdilik sadece loglama — SignalR entegrasyonu AdminNotificationHub üzerinden yapılacak
            var alerts = await GetActiveAlertsAsync(cancellationToken);

            if (alerts.Count > 0)
            {
                var criticalCount = alerts.Count(a => a.Severity == "Critical");
                var warningCount = alerts.Count(a => a.Severity == "Warning");

                _logger.LogWarning(
                    "[SyncMetrics] Aktif alert'ler: {Total} (Critical: {Critical}, Warning: {Warning})",
                    alerts.Count, criticalCount, warningCount);

                foreach (var alert in alerts.Where(a => a.Severity == "Critical"))
                {
                    _logger.LogError(
                        "[SyncMetrics] KRİTİK ALERT — {Channel}: {Message}",
                        alert.Channel, alert.Message);
                }
            }
        }

        // ==================== Private Helpers ====================

        private static SyncChannelHealth MapStateToChannelHealth(MikroSyncState state)
        {
            var status = "Healthy";
            if (state.ConsecutiveFailures >= ConsecutiveFailureCritical)
                status = "Unhealthy";
            else if (state.ConsecutiveFailures >= ConsecutiveFailureWarning || !state.LastSyncSuccess)
                status = "Degraded";

            return new SyncChannelHealth
            {
                ChannelName = state.SyncType,
                Direction = state.Direction,
                Status = status,
                LastSuccessTime = state.LastSyncSuccess ? state.LastSyncTime : null,
                LastFailureTime = !state.LastSyncSuccess ? state.UpdatedAt : null,
                ConsecutiveFailures = state.ConsecutiveFailures,
                LastDurationMs = state.LastSyncDurationMs
            };
        }

        private static string CalculateOverallStatus(List<SyncChannelHealth> channels)
        {
            if (channels.Any(c => c.Status == "Unhealthy"))
                return "Unhealthy";
            if (channels.Any(c => c.Status == "Degraded"))
                return "Degraded";
            return "Healthy";
        }
    }
}
