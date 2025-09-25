using ECommerce.Entities.Concrete;        // Order, Product vs.
using ECommerce.Data.Repositories;        // Repository kullanacaksan
using ECommerce.Core.DTOs.Order;          // OrderCreateDto, OrderDetailDto
using ECommerce.Core.Helpers;             // HashingHelper, JwtTokenHelper vs.
using ECommerce.Business.Services.Interfaces; 
using System;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class IyzicoPaymentService : IPaymentService
    {
        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            // Burada Iyzico API çağrısı yapılır
            await Task.Delay(100);
            return true;
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            // Burada ödeme durumu sorgulanır
            await Task.Delay(100);
            return true;
        }
    }
}
