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
            await _notifier.SendOrderConfirmationAsync(orderId);
            return Ok(new { orderId });
        }

        // POST api/notifications/shipment/{orderId}
        [HttpPost("shipment/{orderId}")]
        public async Task<IActionResult> SendShipmentNotification(int orderId, [FromQuery] string tracking = "")
        {
            await _notifier.SendShipmentNotificationAsync(orderId, tracking);
            return Ok(new { orderId, tracking });
        }
    }
}
