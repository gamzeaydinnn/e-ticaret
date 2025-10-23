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

        public NotificationService(EmailSender emailSender, ECommerceDbContext db)
        {
            _emailSender = emailSender;
            _db = db;
        }

        public async Task SendOrderConfirmationAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerEmail)) return;

            var subject = $"Siparişiniz alındı - {order.OrderNumber}";
            var body = $"Merhaba {order.CustomerName},<br/><br/>Siparişiniz alındı. Sipariş numaranız: <strong>{order.OrderNumber}</strong>.<br/>Toplam: {order.TotalPrice:C}.<br/><br/>Teşekkürler.";
            await _emailSender.SendEmailAsync(order.CustomerEmail, subject, body);
        }

        public async Task SendShipmentNotificationAsync(int orderId, string trackingNumber)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerEmail)) return;

            var subject = $"Siparişiniz gönderildi - {order.OrderNumber}";
            var body = $"Merhaba {order.CustomerName},<br/><br/>Siparişiniz kargoya verildi. Takip numaranız: <strong>{trackingNumber}</strong>.<br/><br/>İyi günler.";
            await _emailSender.SendEmailAsync(order.CustomerEmail, subject, body);
        }
    }
}
