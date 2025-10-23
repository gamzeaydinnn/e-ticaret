using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifier;

        public NotificationsController(INotificationService notifier)
        {
            _notifier = notifier;
        }

        // POST api/notifications/order-confirmation
        [HttpPost("order-confirmation/{orderId}")]
        public async Task<IActionResult> SendOrderConfirmation(int orderId)
        {
            // fire-and-forget pattern: start the send and return accepted
            _ = _notifier.SendOrderConfirmationAsync(orderId);
            return Accepted(new { orderId });
        }

        // POST api/notifications/shipment/{orderId}
        [HttpPost("shipment/{orderId}")]
        public async Task<IActionResult> SendShipmentNotification(int orderId, [FromQuery] string tracking = "")
        {
            _ = _notifier.SendShipmentNotificationAsync(orderId, tracking);
            return Accepted(new { orderId, tracking });
        }
    }
}
