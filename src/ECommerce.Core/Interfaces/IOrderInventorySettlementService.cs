using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ödeme başarı/başarısızlık sonrası yerel stok settle + Mikro sipariş kuyruğu.
    /// Infrastructure (Posnet callback) Core arayüzü üzerinden çağırır.
    /// </summary>
    public interface IOrderInventorySettlementService
    {
        /// <summary>
        /// Ödeme başarılı (Paid / PreAuthorized): rezervasyonu commit et, Mikro sipariş push kuyruğa al.
        /// </summary>
        Task SettlePaymentSuccessAsync(int orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ödeme başarısız: commit yoksa rezervasyonu bırak; commit varsa stok restore.
        /// </summary>
        Task SettlePaymentFailureAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
