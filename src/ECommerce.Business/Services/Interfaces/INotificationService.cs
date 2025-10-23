using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendOrderConfirmationAsync(int orderId);
        Task SendShipmentNotificationAsync(int orderId, string trackingNumber);
    }
}
