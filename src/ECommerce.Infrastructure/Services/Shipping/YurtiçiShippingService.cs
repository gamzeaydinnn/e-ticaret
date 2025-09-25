using System.Threading.Tasks;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.Shipping
{
    public class YurtiçiShippingService : IShippingService
    {
        public Task<decimal> CalculateCostAsync(int orderId)
        {
            return Task.FromResult(55m);
        }

        public Task<decimal> CalculateShippingCostAsync(int orderId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetEstimatedDeliveryAsync(int orderId)
        {
            return Task.FromResult("4 gün");
        }

        public Task<bool> ShipOrderAsync(int orderId)
        {
            return Task.FromResult(true);
        }
    }
}
