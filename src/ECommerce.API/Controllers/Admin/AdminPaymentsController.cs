using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Payment;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentManager _paymentManager;

        public PaymentsController(PaymentManager paymentManager)
        {
            _paymentManager = paymentManager;
        }

        [HttpPost("refund")]
        public async Task<IActionResult> Refund([FromBody] PaymentRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PaymentId)) return BadRequest(new { message = "PaymentId zorunludur." });

            var ok = await _paymentManager.RefundAsync(dto);
            if (!ok) return BadRequest(new { message = "İade başarısız oldu veya işlem bulunamadı." });

            return Ok(new { message = "İade başarılı", paymentId = dto.PaymentId });
        }
    }
}
