// ==========================================================================
// IDeliveryReportService.cs - Teslimat Raporlama Servis Interface'i
// ==========================================================================
// Bu interface, teslimat istatistikleri ve raporlarını yönetir.
// Dashboard metrikleri, performans analizleri ve export işlemleri.
// Admin ve kurye panelleri için veri sağlar.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat raporlama servis interface'i.
    /// İstatistik ve analiz işlemleri.
    /// </summary>
    public interface IDeliveryReportService
    {
        // =====================================================================
        // DASHBOARD METRİKLERİ
        // =====================================================================

        /// <summary>
        /// Admin dashboard için özet metrikleri getirir.
        /// </summary>
        Task<DeliveryDashboardMetrics> GetDashboardMetricsAsync(DateTime? date = null);

        /// <summary>
        /// Kurye dashboard için özet metrikleri getirir.
        /// </summary>
        Task<CourierDashboardMetrics> GetCourierDashboardMetricsAsync(int courierId, DateTime? date = null);

        /// <summary>
        /// Anlık durum özeti getirir.
        /// SignalR ile real-time güncelleme için.
        /// </summary>
        Task<RealTimeDeliveryStats> GetRealTimeStatsAsync();

        // =====================================================================
        // TESLİMAT İSTATİSTİKLERİ
        // =====================================================================

        /// <summary>
        /// Teslimat istatistiklerini getirir.
        /// </summary>
        Task<DeliveryStatistics> GetDeliveryStatisticsAsync(
            DateTime startDate, 
            DateTime endDate,
            int? courierId = null,
            int? zoneId = null);

        /// <summary>
        /// Günlük teslimat trendlerini getirir.
        /// </summary>
        Task<IEnumerable<DailyDeliveryTrend>> GetDailyTrendsAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Saatlik teslimat dağılımını getirir.
        /// </summary>
        Task<IEnumerable<HourlyDistribution>> GetHourlyDistributionAsync(DateTime date);

        /// <summary>
        /// Bölge bazlı teslimat istatistiklerini getirir.
        /// </summary>
        Task<IEnumerable<ZoneDeliveryStats>> GetZoneStatisticsAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Başarısızlık nedenlerinin dağılımını getirir.
        /// </summary>
        Task<IEnumerable<FailureReasonStats>> GetFailureReasonDistributionAsync(
            DateTime startDate, 
            DateTime endDate);

        // =====================================================================
        // KURYE PERFORMANSI
        // =====================================================================

        /// <summary>
        /// Kurye performans raporunu getirir.
        /// </summary>
        Task<CourierPerformanceReport> GetCourierPerformanceAsync(
            int courierId, 
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Tüm kuryelerin performans karşılaştırmasını getirir.
        /// </summary>
        Task<IEnumerable<CourierPerformanceSummary>> GetCourierPerformanceRankingAsync(
            DateTime startDate, 
            DateTime endDate,
            int topN = 10);

        /// <summary>
        /// Kuryenin günlük performans geçmişini getirir.
        /// </summary>
        Task<IEnumerable<CourierDailyPerformance>> GetCourierDailyPerformanceHistoryAsync(
            int courierId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Kuryenin SLA performansını getirir.
        /// </summary>
        Task<CourierSlaPerformance> GetCourierSlaPerformanceAsync(
            int courierId,
            DateTime startDate,
            DateTime endDate);

        // =====================================================================
        // SLA ANALİZİ
        // =====================================================================

        /// <summary>
        /// SLA performans raporunu getirir.
        /// </summary>
        Task<SlaPerformanceReport> GetSlaPerformanceReportAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// SLA ihlali olan teslimatları getirir.
        /// </summary>
        Task<IEnumerable<SlaBreachDetail>> GetSlaBreachesAsync(
            DateTime startDate, 
            DateTime endDate,
            int? courierId = null);

        /// <summary>
        /// Ortalama teslimat sürelerini getirir.
        /// </summary>
        Task<AverageDeliveryTimes> GetAverageDeliveryTimesAsync(
            DateTime startDate, 
            DateTime endDate);

        // =====================================================================
        // EXPORT İŞLEMLERİ
        // =====================================================================

        /// <summary>
        /// Teslimat listesini Excel olarak export eder.
        /// </summary>
        Task<byte[]> ExportDeliveryListToExcelAsync(
            DateTime startDate, 
            DateTime endDate,
            DeliveryStatus? statusFilter = null,
            int? courierId = null);

        /// <summary>
        /// Kurye performans raporunu Excel olarak export eder.
        /// </summary>
        Task<byte[]> ExportCourierPerformanceToExcelAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Teslimat listesini PDF olarak export eder.
        /// </summary>
        Task<byte[]> ExportDeliveryListToPdfAsync(
            DateTime startDate, 
            DateTime endDate);

        // =====================================================================
        // GELİŞMİŞ ANALİZ
        // =====================================================================

        /// <summary>
        /// Yoğunluk haritası verilerini getirir.
        /// </summary>
        Task<IEnumerable<HeatmapPoint>> GetDeliveryHeatmapDataAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Gecikme analizi yapar.
        /// </summary>
        Task<DelayAnalysis> AnalyzeDelaysAsync(
            DateTime startDate, 
            DateTime endDate);

        /// <summary>
        /// Tahminleme verileri oluşturur.
        /// </summary>
        Task<DeliveryForecast> GenerateForecastAsync(DateTime targetDate);
    }

    // =========================================================================
    // DTO SINIFLAR
    // =========================================================================

    /// <summary>
    /// Admin dashboard metrikleri DTO'su.
    /// </summary>
    public class DeliveryDashboardMetrics
    {
        // Günlük Özet
        public int TodayTotalDeliveries { get; set; }
        public int TodayCompletedDeliveries { get; set; }
        public int TodayPendingDeliveries { get; set; }
        public int TodayFailedDeliveries { get; set; }
        public decimal TodayCompletionRate { get; set; }

        // Kurye Durumu
        public int OnlineCouriers { get; set; }
        public int OfflineCouriers { get; set; }
        public int BusyCouriers { get; set; }
        public int AvailableCouriers { get; set; }

        // SLA
        public int SlaBreachCount { get; set; }
        public decimal SlaComplianceRate { get; set; }

        // Karşılaştırma
        public decimal? YesterdayCompletionRate { get; set; }
        public decimal? WeeklyAverageCompletionRate { get; set; }

        // Finansal
        public decimal TodayCodCollected { get; set; }
        public decimal TodayDeliveryFees { get; set; }
    }

    /// <summary>
    /// Kurye dashboard metrikleri DTO'su.
    /// </summary>
    public class CourierDashboardMetrics
    {
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;

        // Bugünkü Performans
        public int TodayAssigned { get; set; }
        public int TodayCompleted { get; set; }
        public int TodayFailed { get; set; }
        public int TodayPending { get; set; }

        // Kazanç
        public decimal TodayEarnings { get; set; }
        public decimal WeeklyEarnings { get; set; }
        public decimal MonthlyEarnings { get; set; }

        // Performans
        public decimal CompletionRate { get; set; }
        public decimal SlaComplianceRate { get; set; }
        public int AverageDeliveryTimeMinutes { get; set; }
        public decimal Rating { get; set; }

        // Streak
        public int CurrentStreak { get; set; } // Ardışık başarılı teslimat
        public int BestStreak { get; set; }
    }

    /// <summary>
    /// Anlık durum istatistikleri DTO'su.
    /// </summary>
    public class RealTimeDeliveryStats
    {
        public DateTime Timestamp { get; set; }
        public int ActiveDeliveries { get; set; }
        public int WaitingForAssignment { get; set; }
        public int InTransit { get; set; }
        public int NearSlaBreachCount { get; set; }
        public int OnlineCouriersCount { get; set; }
        public Dictionary<DeliveryStatus, int> ByStatus { get; set; } = new();
    }

    /// <summary>
    /// Teslimat istatistikleri DTO'su.
    /// </summary>
    public class DeliveryStatistics
    {
        public int TotalDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int CancelledDeliveries { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal FailureRate { get; set; }
        public int AverageDeliveryTimeMinutes { get; set; }
        public int MedianDeliveryTimeMinutes { get; set; }
        public decimal TotalCodCollected { get; set; }
        public decimal TotalDeliveryFees { get; set; }
    }

    /// <summary>
    /// Günlük trend DTO'su.
    /// </summary>
    public class DailyDeliveryTrend
    {
        public DateTime Date { get; set; }
        public int TotalDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageTimeMinutes { get; set; }
    }

    /// <summary>
    /// Saatlik dağılım DTO'su.
    /// </summary>
    public class HourlyDistribution
    {
        public int Hour { get; set; }
        public int DeliveryCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal AverageTimeMinutes { get; set; }
    }

    /// <summary>
    /// Bölge istatistikleri DTO'su.
    /// </summary>
    public class ZoneDeliveryStats
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int TotalDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageTimeMinutes { get; set; }
        public int ActiveCouriers { get; set; }
    }

    /// <summary>
    /// Başarısızlık nedeni istatistikleri DTO'su.
    /// </summary>
    public class FailureReasonStats
    {
        public DeliveryFailureReason Reason { get; set; }
        public string ReasonName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Kurye performans raporu DTO'su.
    /// </summary>
    public class CourierPerformanceReport
    {
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Teslimat Metrikleri
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalFailed { get; set; }
        public decimal CompletionRate { get; set; }

        // Zaman Metrikleri
        public int AverageDeliveryTimeMinutes { get; set; }
        public int FastestDeliveryMinutes { get; set; }
        public int SlowestDeliveryMinutes { get; set; }

        // SLA
        public int OnTimeDeliveries { get; set; }
        public int LateDeliveries { get; set; }
        public decimal SlaComplianceRate { get; set; }

        // Finansal
        public decimal TotalEarnings { get; set; }
        public decimal TotalCodCollected { get; set; }
        public decimal AverageEarningsPerDelivery { get; set; }

        // Müşteri Memnuniyeti
        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FiveStarRatings { get; set; }
    }

    /// <summary>
    /// Kurye performans özeti DTO'su (sıralama için).
    /// </summary>
    public class CourierPerformanceSummary
    {
        public int Rank { get; set; }
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public int TotalDeliveries { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal SlaComplianceRate { get; set; }
        public decimal AverageRating { get; set; }
        public decimal PerformanceScore { get; set; } // Birleşik skor
    }

    /// <summary>
    /// Kurye günlük performans DTO'su.
    /// </summary>
    public class CourierDailyPerformance
    {
        public DateTime Date { get; set; }
        public int Assigned { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public decimal Earnings { get; set; }
        public int WorkedMinutes { get; set; }
        public decimal AverageDeliveryTimeMinutes { get; set; }
    }

    /// <summary>
    /// Kurye SLA performansı DTO'su.
    /// </summary>
    public class CourierSlaPerformance
    {
        public int CourierId { get; set; }
        public int TotalDeliveries { get; set; }
        public int OnTimeDeliveries { get; set; }
        public int LateBy15MinutesOrLess { get; set; }
        public int LateBy30MinutesOrLess { get; set; }
        public int LateByMoreThan30Minutes { get; set; }
        public decimal SlaComplianceRate { get; set; }
        public int AverageDelayMinutes { get; set; }
    }

    /// <summary>
    /// SLA performans raporu DTO'su.
    /// </summary>
    public class SlaPerformanceReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDeliveries { get; set; }
        public int OnTimeDeliveries { get; set; }
        public int LateDeliveries { get; set; }
        public decimal OverallSlaComplianceRate { get; set; }
        public int AverageDeliveryTimeMinutes { get; set; }
        public Dictionary<DeliveryPriority, decimal> SlaByPriority { get; set; } = new();
    }

    /// <summary>
    /// SLA ihlali detayı DTO'su.
    /// </summary>
    public class SlaBreachDetail
    {
        public int DeliveryTaskId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int? CourierId { get; set; }
        public string? CourierName { get; set; }
        public DateTime DueTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int DelayMinutes { get; set; }
        public string? DelayReason { get; set; }
    }

    /// <summary>
    /// Ortalama teslimat süreleri DTO'su.
    /// </summary>
    public class AverageDeliveryTimes
    {
        public decimal OverallAverageMinutes { get; set; }
        public decimal AssignmentToAcceptanceMinutes { get; set; }
        public decimal AcceptanceToPickupMinutes { get; set; }
        public decimal PickupToDeliveryMinutes { get; set; }
        public Dictionary<DeliveryPriority, decimal> ByPriority { get; set; } = new();
        public Dictionary<int, decimal> ByZone { get; set; } = new();
    }

    /// <summary>
    /// Yoğunluk haritası noktası DTO'su.
    /// </summary>
    public class HeatmapPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Intensity { get; set; }
    }

    /// <summary>
    /// Gecikme analizi DTO'su.
    /// </summary>
    public class DelayAnalysis
    {
        public int TotalDelayedDeliveries { get; set; }
        public int AverageDelayMinutes { get; set; }
        public Dictionary<string, int> DelaysByReason { get; set; } = new();
        public Dictionary<int, int> DelaysByHour { get; set; } = new();
        public Dictionary<int, int> DelaysByZone { get; set; } = new();
        public IEnumerable<string> TopDelayReasons { get; set; } = new List<string>();
    }

    /// <summary>
    /// Teslimat tahmini DTO'su.
    /// </summary>
    public class DeliveryForecast
    {
        public DateTime TargetDate { get; set; }
        public int ExpectedDeliveryCount { get; set; }
        public int RecommendedCourierCount { get; set; }
        public Dictionary<int, int> ExpectedByHour { get; set; } = new();
        public Dictionary<int, int> ExpectedByZone { get; set; } = new();
        public decimal ConfidenceScore { get; set; }
    }
}
