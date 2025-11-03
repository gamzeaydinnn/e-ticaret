using ECommerce.Core.DTOs.Payment;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Stripe;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Enums;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOptions<PaymentSettings> _paymentOptions;
        private readonly ECommerceDbContext _db;

        public PaymentsController(IPaymentService paymentService, IOptions<PaymentSettings> paymentOptions, ECommerceDbContext db)
        {
            _paymentService = paymentService;
            _paymentOptions = paymentOptions;
            _db = db;
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

        /// <summary>
        /// Hosted checkout başlatır ve yönlendirme bilgisi döner (Stripe Checkout / Iyzico Form)
        /// </summary>
        [HttpPost("init")]
        public async Task<IActionResult> Initiate([FromBody] PaymentCreateDto dto)
        {
            var init = await _paymentService.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency ?? "TRY");
            return Ok(init);
        }

        /// <summary>
        /// Iyzipay callback endpoint (POST). Iyzipay token'ı ile sonucu doğrular.
        /// </summary>
        [HttpPost("iyzico/callback")]
        public async Task<IActionResult> IyzicoCallback([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest();
            var status = await _paymentService.GetPaymentStatusAsync(token);
            if (status == PaymentStatus.Successful)
            {
                var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Provider == "iyzico" && p.ProviderPaymentId == token);
                if (payment != null)
                {
                    payment.Status = "Success";
                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Paid;
                    }
                    await _db.SaveChangesAsync();

                    // Başarılı işlemden sonra frontend başarı sayfasına yönlendir (varsa)
                    var successUrl = _paymentOptions.Value.ReturnUrlSuccess;
                    if (!string.IsNullOrWhiteSpace(successUrl))
                    {
                        var sep = successUrl.Contains("?") ? "&" : "?";
                        return Redirect(successUrl + sep + $"orderId={payment.OrderId}");
                    }
                }
            }
            return Ok(new { token, status });
        }

        /// <summary>
        /// Stripe webhook endpoint (Checkout Session / PaymentIntent events)
        /// </summary>
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var sigHeader = Request.Headers["Stripe-Signature"].ToString();
            var secret = _paymentOptions.Value.StripeWebhookSecret;
            if (string.IsNullOrWhiteSpace(secret)) return BadRequest("Webhook secret not configured");

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, sigHeader, secret);
            }
            catch
            {
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                var orderIdMeta = session.Metadata != null && session.Metadata.ContainsKey("orderId") ? session.Metadata["orderId"] : null;
                if (int.TryParse(orderIdMeta, out var orderId))
                {
                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Paid;
                        await _db.SaveChangesAsync();
                    }
                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == session.Id);
                    if (payment != null)
                    {
                        payment.Status = "Success";
                        payment.RawResponse = json;
                        await _db.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }
    }
}
