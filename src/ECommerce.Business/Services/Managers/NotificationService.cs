using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Infrastructure.Services.Email;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Business.Services.Managers
{
    public class NotificationService : INotificationService
    {
        private readonly EmailSender _emailSender;
        private readonly ECommerceDbContext _db;
        private readonly ECommerce.Core.Messaging.MailQueue? _mailQueue;

        public NotificationService(EmailSender emailSender, ECommerceDbContext db, ECommerce.Core.Messaging.MailQueue? mailQueue = null)
        {
            _emailSender = emailSender;
            _db = db;
            _mailQueue = mailQueue;
        }

        public async Task SendOrderConfirmationAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerEmail)) return;

            var subject = $"Siparişiniz alındı - {order.OrderNumber}";
            var body = $"Merhaba {order.CustomerName},<br/><br/>Siparişiniz alındı. Sipariş numaranız: <strong>{order.OrderNumber}</strong>.<br/>Toplam: {order.TotalPrice:C}.<br/><br/>Teşekkürler.";

            if (_mailQueue != null)
            {
                await _mailQueue.EnqueueAsync(new ECommerce.Core.Messaging.EmailJob
                {
                    To = order.CustomerEmail,
                    Subject = subject,
                    Body = body,
                    IsHtml = true
                });
            }
            else
            {
                // Fallback for tests or environments without queue configured
                await _emailSender.SendEmailAsync(order.CustomerEmail, subject, body);
            }
        }

        public async Task SendShipmentNotificationAsync(int orderId, string trackingNumber)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerEmail)) return;

            var subject = $"Siparişiniz gönderildi - {order.OrderNumber}";
            var body = $"Merhaba {order.CustomerName},<br/><br/>Siparişiniz kargoya verildi. Takip numaranız: <strong>{trackingNumber}</strong>.<br/><br/>İyi günler.";

            if (_mailQueue != null)
            {
                await _mailQueue.EnqueueAsync(new ECommerce.Core.Messaging.EmailJob
                {
                    To = order.CustomerEmail,
                    Subject = subject,
                    Body = body,
                    IsHtml = true
                });
            }
            else
            {
                await _emailSender.SendEmailAsync(order.CustomerEmail, subject, body);
            }
        }
    }
}
