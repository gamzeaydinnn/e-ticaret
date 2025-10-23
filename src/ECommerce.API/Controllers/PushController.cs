using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushController : ControllerBase
    {
        private readonly IPushService _push;

        public PushController(IPushService push)
        {
            _push = push;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromQuery] string userId, [FromBody] PushSubscriptionDto sub)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("userId required");
            await _push.SubscribeAsync(userId, sub);
            return Ok();
        }

        // Public endpoint to retrieve the VAPID public key for client subscription.
        // Returns { publicKey: '...' }.
        [HttpGet("vapidPublicKey")]
        public IActionResult GetVapidPublicKey([FromServices] Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var key = config["Push:VapidPublicKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) return NotFound(new { error = "VAPID public key not configured" });
            return Ok(new { publicKey = key });
        }

        // Admin/test: send a push to a user
        [HttpPost("send/{userId}")]
        public async Task<IActionResult> Send(string userId, [FromBody] string message)
        {
            await _push.SendNotificationAsync(userId, message ?? "Test push");
            return Accepted();
        }
    }
}
