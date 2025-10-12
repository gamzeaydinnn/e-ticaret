using ECommerce.Core.Interfaces;
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

        public Task<int> GetPaymentCountAsync()
        {
            throw new NotImplementedException();
        }
    }
}
