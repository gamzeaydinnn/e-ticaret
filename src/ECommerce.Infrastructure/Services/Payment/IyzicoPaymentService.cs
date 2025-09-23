using ECommerce.Entities.Concrete;// <-- Order, Product vs. için
using ECommerce.Data.Repositories.Concrete; // <-- repository kullanacaksan
using ECommerce.Core.DTOs.Order; // <-- OrderCreateDto, OrderDetailDto
using ECommerce.Core.Helpers; // <-- HashingHelper, JwtTokenHelper vs.
using System;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class IyzicoPaymentService : IPaymentService
    {
        public Task ProcessPayment(Order order)
        {
            // ödeme işlemleri
            throw new NotImplementedException();
        }
    }
}
