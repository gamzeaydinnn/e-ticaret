using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using Microsoft.Extensions.Logging;
using WebPush;

namespace ECommerce.Business.Services.Managers
{
    public class PushService : IPushService
    {
        private readonly ConcurrentDictionary<string, PushSubscriptionDto> _store = new();
        private readonly VapidDetails _vapid;
        private readonly ILogger<PushService> _logger;

        public PushService(ILogger<PushService> logger, string vapidSubject, string vapidPublicKey, string vapidPrivateKey)
        {
            _logger = logger;
            _vapid = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        }

        public Task SubscribeAsync(string userId, PushSubscriptionDto subscription)
        {
            _store[userId] = subscription;
            _logger.LogInformation("Push subscription saved for user {UserId}", userId);
            return Task.CompletedTask;
        }

        public async Task SendNotificationAsync(string userId, string payload)
        {
            if (!_store.TryGetValue(userId, out var sub))
            {
                _logger.LogInformation("No push subscription for user {UserId}", userId);
                return;
            }

            var pushSub = new PushSubscription(sub.Endpoint, sub.Keys.P256dh, sub.Keys.Auth);
            var client = new WebPushClient();
            client.SetVapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);

            try
            {
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex)
            {
                _logger.LogWarning(ex, "Failed to send push to {UserId}", userId);
            }
        }
    }
}
