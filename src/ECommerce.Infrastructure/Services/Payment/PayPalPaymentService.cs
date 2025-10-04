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

        public Task<int> GetPaymentCountAsync()
        {
            throw new NotImplementedException();
        }
    }
    /*F. Ödeme entegrasyon (Stripe / iyzico / PayTR)
	• Stripe: .NET server-side SDK var; PaymentIntent veya Checkout Sessions kullan. Webhook ile ödeme tamamlandığında işle. (Stripe server-side .NET SDK docs). (docs.stripe.com)
	• iyzico: .NET client (iyzipay) mevcut; abonelik, tokenization özellikleri var. (nuget.org)
	• PayTR: iframe/direct API dökümanları var — callback / token üretimi süreçlerine dikkat et. (PayTR)
*/
}
