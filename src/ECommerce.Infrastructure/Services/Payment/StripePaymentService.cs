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

namespace ECommerce.Infrastructure.Services.Payment
{
    public class StripePaymentService : IPaymentService
    {
        private readonly PaymentSettings _settings;

        public StripePaymentService(IOptions<PaymentSettings> options)
        {
            _settings = options.Value;
        }
        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            // Stripe API çağrısı yapılacak
            await Task.Delay(100); // simülasyon
            return true;
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            // Stripe ödeme durumu sorgulanacak
            await Task.Delay(100);
            return true;
        }

        public Task<int> GetPaymentCountAsync() => Task.FromResult(0);

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
