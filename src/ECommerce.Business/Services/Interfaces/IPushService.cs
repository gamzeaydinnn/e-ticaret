using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IPushService
    {
        Task SubscribeAsync(string userId, PushSubscriptionDto subscription);
        Task SendNotificationAsync(string userId, string payload);
    }

    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public PushSubscriptionKeys Keys { get; set; } = new PushSubscriptionKeys();
    }

    public class PushSubscriptionKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
