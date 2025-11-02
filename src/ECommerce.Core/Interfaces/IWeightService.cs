using System.Threading.Tasks;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ağırlık raporu işleme servisi
    /// </summary>
    public interface IWeightService
    {
        /// <summary>
        /// Tartı cihazından gelen raporu işle
        /// </summary>
        Task<WeightReport> ProcessReportAsync(MicroWeightReportDto dto);

        /// <summary>
        /// Raporu onayla ve ödeme işlemini başlat
        /// </summary>
        Task<bool> ApproveReportAsync(int reportId, int approvedByUserId, string? note = null);

        /// <summary>
        /// Raporu reddet
        /// </summary>
        Task<bool> RejectReportAsync(int reportId, int rejectedByUserId, string? reason = null);

        /// <summary>
        /// Fazla tutarı tahsil et (ödeme işlemi)
        /// </summary>
        Task<bool> ChargeOverageAsync(int reportId);

        /// <summary>
        /// Rapor ID'sine göre rapor getir
        /// </summary>
        Task<WeightReport?> GetReportByIdAsync(int reportId);

        /// <summary>
        /// External ID'ye göre rapor getir (idempotency kontrolü)
        /// </summary>
        Task<WeightReport?> GetReportByExternalIdAsync(string externalReportId);
    }
}
