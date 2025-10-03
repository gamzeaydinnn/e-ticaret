using System.Threading.Tasks;

namespace  ECommerce.Core.Interfaces
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
    }
}
