using ECommerce.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class ShippingManager : IShippingService
    {
        public ShippingManager()
        {
        }

        public async Task<decimal> CalculateShippingCostAsync(int orderId)
        {
            // Basit shipping cost simulasyonu
            await Task.Delay(100);
            return 15.99m; // Fixed shipping cost
        }

        public async Task<string> GetEstimatedDeliveryAsync(int orderId)
        {
            await Task.Delay(100);
            return "2-3 gün"; // Fixed delivery estimate
        }

        public async Task<bool> ShipOrderAsync(int orderId)
        {
            await Task.Delay(100);
            return true; // Always successful for demo
        }
    }
}
