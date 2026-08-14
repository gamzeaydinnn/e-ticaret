// ==========================================================================
// IRefundDiagnosticService.cs - İade teşhis servisi (Faz 0)
// ==========================================================================

using System.Threading.Tasks;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Başarısız veya şüpheli iade kayıtlarını analiz eder.
    /// Kod değişikliği yapmadan önce kök neden tespiti için kullanılır.
    /// </summary>
    public interface IRefundDiagnosticService
    {
        Task<RefundOrderDiagnosticDto?> GetOrderDiagnosticAsync(int orderId);

        Task<RefundFailedSummaryDto> GetFailedRefundsDiagnosticAsync();
    }
}
