using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    // Publishes real-time stock changes to interested clients (e.g., via SignalR)
    public interface IStockUpdatePublisher
    {
        Task PublishAsync(int productId, int newQuantity);
    }
}

