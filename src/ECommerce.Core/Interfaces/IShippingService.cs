using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IShippingService
    {
        // Kargo tahmini ücreti ve süre
        Task<decimal> CalculateShippingCostAsync(int orderId);
        Task<string> GetEstimatedDeliveryAsync(int orderId);

        // Sipariş gönderimini başlat
        Task<bool> ShipOrderAsync(int orderId);
    }
}
