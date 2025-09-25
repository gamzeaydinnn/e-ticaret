// /ECommerce.Infrastructure/Services/Shipping/ArasShippingService.cs
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.Shipping
{
    public class ArasShippingService : IShippingService
    {
        public Task<decimal> CalculateCostAsync(int orderId)
        {
            return Task.FromResult(50m);
        }

        public Task<decimal> CalculateShippingCostAsync(int orderId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetEstimatedDeliveryAsync(int orderId)
        {
            return Task.FromResult("2 gün");
        }

        public Task<bool> ShipOrderAsync(int orderId)
        {
            return Task.FromResult(true);
        }
    }
}

//Bu servis, Aras kargo entegrasyonu için bir mock / placeholder görevi görüyor.
//Gerçek projede:
//Aras API çağrısı yapılacak, Sipariş bilgileri gönderilecek, Gerçek bir kargo takip numarası alınacak
//Şu anki haliyle test ve geliştirme aşamasında kullanılır, gerçek kargo entegrasyonu yapmaz.