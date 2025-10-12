using ECommerce.Entities.Concrete;        // Order, Product vs.
using ECommerce.Data.Repositories;        // Repository kullanacaksan
using ECommerce.Core.DTOs.Order;          // OrderCreateDto, OrderDetailDto
using ECommerce.Core.Helpers;             // HashingHelper, JwtTokenHelper vs.
using ECommerce.Core.Interfaces;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            //ProcessPaymentAsync(int orderId, decimal amount)
//Bir sipariş ve ödeme miktarını alıyor.
//Şu an sadece 100ms bekliyor ve true dönüyor (ödeme başarılı gibi).. Gerçek kullanımda Iyzico API’ye istek gönderilecek, ödeme yapılacak ve sonucu dönecek.
//CheckPaymentStatusAsync(string paymentId)
//Ödeme ID’si ile ödeme durumunu sorguluyor.. Şu an sadece 100ms bekliyor ve true dönüyor (ödeme tamamlandı gibi).
//Gerçek kullanımda Iyzico API’den ödeme durumu alınacak.
        }

        public Task<int> GetPaymentCountAsync()
        {
            throw new NotImplementedException();
        }
    }
}
//Ne işe yarıyor?
//Bu servis IPaymentService interface’ini implement ediyor, yani projede dependency injection ile ödeme servisleri değiştirilebilir hale geliyor.
//Şu an test ve geliştirme aşamasında ödeme akışı simüle ediliyor.
//erçek proje canlıya geçtiğinde, API çağrıları yapılacak ve ödeme gerçekten gerçekleşecek.