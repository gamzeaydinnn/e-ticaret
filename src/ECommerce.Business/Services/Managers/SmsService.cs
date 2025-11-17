using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.Business.Services.Managers
{
    public class SmsService : ISmsService
    {
        private readonly ECommerce.Core.Messaging.SmsQueue? _smsQueue;

        public SmsService(ECommerce.Core.Messaging.SmsQueue? smsQueue = null)
        {
            _smsQueue = smsQueue;
        }

        public Task SendAsync(string phoneNumber, string message)
        {
            if (_smsQueue != null)
            {
                // enqueue and return
                _ = _smsQueue.EnqueueAsync(new ECommerce.Core.Messaging.SmsJob
                {
                    PhoneNumber = phoneNumber,
                    Message = message
                });
                return Task.CompletedTask;
            }

            // Fallback: just write to console
            System.Console.WriteLine($"[SMS] To={phoneNumber} Message={message}");
            return Task.CompletedTask;
        }
    }
}
