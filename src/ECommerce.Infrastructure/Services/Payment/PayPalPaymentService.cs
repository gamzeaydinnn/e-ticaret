using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Entities.Enums;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Core.DTOs.Payment;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class PayPalPaymentService : IPaymentService
    {
        private readonly PaymentSettings _settings;

        public PayPalPaymentService(IOptions<PaymentSettings> options)
        {
            _settings = options.Value;
        }
        public virtual async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            // Burada PayPal API çağrısı yapılır
            await Task.Delay(100);
            return true;
        }

        public virtual async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            // Burada PayPal ödeme durumu sorgulanır
            await Task.Delay(100);
            return true;
        }

        public virtual Task<int> GetPaymentCountAsync() => Task.FromResult(0);

        public virtual async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public virtual async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var ok = await CheckPaymentStatusAsync(paymentId);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public virtual Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            // Gelecekte PayPal Checkout (Orders API) ile başlatılacak
            return Task.FromResult(new PaymentInitResult
            {
                Provider = "paypal",
                RequiresRedirect = false,
                OrderId = orderId,
                Amount = amount,
                Currency = currency
            });
        }
    }
    /*F. Ödeme entegrasyon (Stripe / iyzico / PayTR)
	• Stripe: .NET server-side SDK var; PaymentIntent veya Checkout Sessions kullan. Webhook ile ödeme tamamlandığında işle. (Stripe server-side .NET SDK docs). (docs.stripe.com)
	• iyzico: .NET client (iyzipay) mevcut; abonelik, tokenization özellikleri var. (nuget.org)
	• PayTR: iframe/direct API dökümanları var — callback / token üretimi süreçlerine dikkat et. (PayTR)
*/
}
