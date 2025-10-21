using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using ECommerce.API.Hubs;

namespace ECommerce.API.Realtime
{
    public class SignalRStockUpdatePublisher : IStockUpdatePublisher
    {
        private readonly IHubContext<StockHub> _hubContext;

        public SignalRStockUpdatePublisher(IHubContext<StockHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishAsync(int productId, int newQuantity)
        {
            var payload = new { productId, quantity = newQuantity };
            // Broadcast globally
            await _hubContext.Clients.All.SendAsync("StockUpdated", payload);
            // And to product-specific group for efficiency
            await _hubContext.Clients.Group($"product-{productId}").SendAsync("StockUpdated", payload);
        }
    }
}

