using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ağırlık raporu repository interface
    /// </summary>
    public interface IWeightReportRepository : IRepository<WeightReport>
    {
        /// <summary>
        /// Rapor ID'sine göre rapor getir (idempotency kontrolü için)
        /// </summary>
        Task<WeightReport?> GetByExternalReportIdAsync(string externalReportId);

        /// <summary>
        /// Sipariş ID'sine göre raporları getir
        /// </summary>
        Task<IEnumerable<WeightReport>> GetByOrderIdAsync(int orderId);

        /// <summary>
        /// Duruma göre raporları getir (sayfalanmış)
        /// </summary>
        Task<(IEnumerable<WeightReport> Reports, int TotalCount)> GetByStatusAsync(
            WeightReportStatus status, 
            int page = 1, 
            int pageSize = 20);

        /// <summary>
        /// Bekleyen raporları getir
        /// </summary>
        Task<IEnumerable<WeightReport>> GetPendingReportsAsync();

        /// <summary>
        /// Rapor durumunu güncelle
        /// </summary>
        Task<bool> UpdateStatusAsync(int reportId, WeightReportStatus newStatus);

        /// <summary>
        /// Rapor istatistikleri
        /// </summary>
        Task<WeightReportStats> GetStatsAsync();
    }

    /// <summary>
    /// Ağırlık raporu istatistikleri
    /// </summary>
    public class WeightReportStats
    {
        public int TotalReports { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int ChargedCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalOverageAmount { get; set; }
        public decimal TotalChargedAmount { get; set; }
    }
}
