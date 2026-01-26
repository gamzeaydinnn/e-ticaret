using System.Collections.Generic;
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
        /// Webhook'tan gelen tartı verisini işle
        /// </summary>
        Task<MicroWeightWebhookResponseDto> ProcessWebhookAsync(MicroWeightWebhookRequestDto dto);

        /// <summary>
        /// Kurye tarafından manuel girilen tartı farkını işle
        /// </summary>
        Task<CourierWeightAdjustmentResponseDto> ProcessCourierAdjustmentAsync(
            CourierWeightAdjustmentDto dto, int courierId);

        /// <summary>
        /// Sipariş için tartı bazlı final tutarı hesapla
        /// </summary>
        Task<decimal> CalculateFinalAmountForOrderAsync(int orderId);

        /// <summary>
        /// Sipariş için toplam tartı farkı tutarını getir
        /// </summary>
        Task<decimal> GetTotalWeightDifferenceAmountAsync(int orderId);

        /// <summary>
        /// Sipariş için bekleyen tartı raporlarını getir
        /// </summary>
        Task<IEnumerable<WeightReport>> GetPendingReportsForOrderAsync(int orderId);

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
