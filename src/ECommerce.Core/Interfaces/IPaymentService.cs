using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Ödeme işlemi başlatır
        /// </summary> 
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="amount">Ödenecek tutar</param>
        /// <returns>Ödeme başarılı mı?</returns>
        Task<bool> ProcessPaymentAsync(int orderId, decimal amount);

        /// <summary>
        /// Ödeme durumu sorgular
        /// </summary>
        /// <param name="paymentId">Ödeme ID</param>
        /// <returns>Ödeme durumu (true = başarılı, false = başarısız)</returns>
        Task<bool> CheckPaymentStatusAsync(string paymentId);
        Task<int> GetPaymentCountAsync(); 

        // Tip güvenliği için ayrıntılı sürümler
        Task<ECommerce.Entities.Enums.PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount);
        Task<ECommerce.Entities.Enums.PaymentStatus> GetPaymentStatusAsync(string paymentId);

        // Hosted checkout / 3DS başlatma (Stripe Checkout / Iyzico Checkout Form)
        Task<ECommerce.Core.DTOs.Payment.PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency);
    }
}
