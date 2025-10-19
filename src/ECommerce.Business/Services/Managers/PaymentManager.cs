using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Enums;
using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class PaymentManager : IPaymentService
    {
        public PaymentManager()
        {
            // Burada DI ile ödeme sağlayıcı client ekleyebilirsin (IyzicoClient, StripeClient vb.)
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            // Basit simülasyon: ödeme başarılı ise true döndür
            await Task.Delay(500); // Simüle edilmiş API call
            Console.WriteLine($"Order {orderId} için {amount} TL ödeme işleniyor...");
            return true;
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            // Basit simülasyon: ödeme durumu sorgusu
            await Task.Delay(300); // Simüle edilmiş API call
            Console.WriteLine($"Payment {paymentId} durumu kontrol ediliyor...");
            return true; // Ödeme başarılı
        }

        public Task<int> GetPaymentCountAsync()
        {
            // Henüz gerçek bir ödeme veritabanı yoksa 0 döndür.
            // İleride repository ile entegre edilerek gerçek sayı alınabilir.
            return Task.FromResult(0);
        }

        public async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var ok = await CheckPaymentStatusAsync(paymentId);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }
    }
}
