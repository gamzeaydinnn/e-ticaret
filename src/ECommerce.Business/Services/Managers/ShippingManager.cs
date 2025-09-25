using ECommerce.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class ShippingManager : IShippingService // <-- Bunu ekledik
    {
        private readonly IShippingService[] _shippingProviders;

        public ShippingManager(IShippingService[] shippingProviders)
        {
            _shippingProviders = shippingProviders;
        }

        public async Task<decimal> CalculateShippingCostAsync(int orderId)
        {
            var costs = await Task.WhenAll(_shippingProviders.Select(p => p.CalculateShippingCostAsync(orderId)));
            return costs.Min();
        }

        public async Task<string> GetEstimatedDeliveryAsync(int orderId)
        {
            var estimates = await Task.WhenAll(_shippingProviders.Select(p => p.GetEstimatedDeliveryAsync(orderId)));
            return estimates.OrderBy(e => e).First();
        }

        public async Task<bool> ShipOrderAsync(int orderId)
        {
            // Örnek: ilk sağlayıcı ile gönderim başlat
            return await _shippingProviders[0].ShipOrderAsync(orderId);
        }
    }
}
