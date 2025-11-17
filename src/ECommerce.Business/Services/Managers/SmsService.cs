using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.Business.Services.Managers
{
    public class SmsService : ISmsService
    {
        public Task SendAsync(string phoneNumber, string message)
        {
            // Stub: Gerçek SMS gönderimi yok, sadece no-op
            return Task.CompletedTask;
        }
    }
}
