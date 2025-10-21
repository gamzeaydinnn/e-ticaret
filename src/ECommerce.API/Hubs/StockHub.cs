using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ECommerce.API.Hubs
{
    public class StockHub : Hub
    {
        // Optional: Allow joining product-specific groups for targeted updates
        public Task JoinProductGroup(string productId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"product-{productId}");

        public Task LeaveProductGroup(string productId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product-{productId}");
    }
}

