using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Ödeme başlat
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentCreateDto dto)
        {
            var result = await _paymentService.ProcessPaymentAsync(dto.OrderId, dto.Amount);
            if (!result)
                return BadRequest(new { message = "Ödeme başarısız" });

            return Ok(new { message = "Ödeme başarılı", orderId = dto.OrderId, amount = dto.Amount });
        }

        /// <summary>
        /// Ödeme durumunu sorgula
        /// </summary>
        [HttpGet("status/{paymentId}")]
        public async Task<IActionResult> CheckPaymentStatus(string paymentId)
        {
            var status = await _paymentService.CheckPaymentStatusAsync(paymentId);
            return Ok(new { paymentId, status });
        }
    }
}
