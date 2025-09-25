using ECommerce.Core.Interfaces;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class PayPalPaymentService : IPaymentService
    {
        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            // Burada PayPal API çağrısı yapılır
            await Task.Delay(100);
            return true;
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            // Burada PayPal ödeme durumu sorgulanır
            await Task.Delay(100);
            return true;
        }
    }
}
