using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.Extensions;
using ECommerce.API.Infrastructure;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Kullanıcının siparişlerini listeler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] int? userId = null)
        {
            var effectiveUserId = ResolveUserId(userId);
            if (!effectiveUserId.HasValue)
                return BadRequest(new { message = "userId bulunamadı. Giriş yapın veya userId parametresi gönderin." });

            var orders = await _orderService.GetOrdersAsync(effectiveUserId);
            return Ok(orders);
        }

        /// <summary>
        /// Tek bir siparişin detayını döner.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id, [FromQuery] int? userId = null)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return NotFound();

            var effectiveUserId = ResolveUserId(userId);
            if (effectiveUserId.HasValue && order.UserId != effectiveUserId.Value)
                return Forbid();

            return Ok(order);
        }

        /// <summary>
        /// Admin panelinden manuel sipariş oluşturma senaryoları için basit endpoint.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek gövdesi" });

            if (dto.ClientOrderId.HasValue)
            {
                var existing = await _orderService.GetByClientOrderIdAsync(dto.ClientOrderId.Value);
                if (existing != null)
                {
                    return Conflict(new
                    {
                        success = true,
                        message = "Bu işlem için zaten bir sipariş oluşturulmuş.",
                        orderId = existing.Id,
                        orderNumber = existing.OrderNumber,
                        status = existing.Status,
                        vatAmount = existing.VatAmount,
                        totalPrice = existing.TotalPrice,
                        finalPrice = existing.FinalPrice,
                        discountAmount = existing.DiscountAmount,
                        couponDiscountAmount = existing.CouponDiscountAmount,
                        campaignDiscountAmount = existing.CampaignDiscountAmount,
                        couponCode = existing.CouponCode,
                        clientOrderId = dto.ClientOrderId,
                        order = existing
                    });
                }
            }

            var authenticatedUserId = User.GetUserId();
            dto.UserId = authenticatedUserId > 0 ? authenticatedUserId : null;

            var order = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        /// <summary>
        /// Üye olmadan veya üye olarak checkout işlemi
        /// </summary>
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] OrderCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek gövdesi" });

            // Validation handled by FluentValidation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                var errorMessage = string.Join("; ", errors);
                
                // Log validation errors
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<OrdersController>)) as ILogger<OrdersController>;
                logger?.LogError("[CHECKOUT] Validation hatası: {Errors}", errorMessage);
                
                return BadRequest(new 
                { 
                    message = "Validation hatası",
                    errors = ModelState.ToDictionary(
                        x => x.Key, 
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            if (dto.ClientOrderId.HasValue)
            {
                var existing = await _orderService.GetByClientOrderIdAsync(dto.ClientOrderId.Value);
                if (existing != null)
                {
                    return Conflict(new
                    {
                        success = true,
                        message = "Bu işlem için zaten bir sipariş oluşturulmuş.",
                        orderId = existing.Id,
                        orderNumber = existing.OrderNumber,
                        status = existing.Status,
                        vatAmount = existing.VatAmount,
                        totalPrice = existing.TotalPrice,
                        finalPrice = existing.FinalPrice,
                        discountAmount = existing.DiscountAmount,
                        couponDiscountAmount = existing.CouponDiscountAmount,
                        campaignDiscountAmount = existing.CampaignDiscountAmount,
                        couponCode = existing.CouponCode,
                        clientOrderId = dto.ClientOrderId,
                        order = existing
                    });
                }
            }

            var authenticatedUserId = User.GetUserId();
            dto.UserId = authenticatedUserId > 0 ? authenticatedUserId : null;

            try
            {
                var result = await _orderService.CheckoutAsync(dto);

                return Ok(new
                {
                    success = true,
                    orderId = result.Id,
                    orderNumber = result.OrderNumber,
                    status = result.Status,
                    vatAmount = result.VatAmount,
                    totalPrice = result.TotalPrice,
                    finalPrice = result.FinalPrice,
                    discountAmount = result.DiscountAmount,
                    couponDiscountAmount = result.CouponDiscountAmount,
                    campaignDiscountAmount = result.CampaignDiscountAmount,
                    couponCode = result.CouponCode
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// Sipariş iptali (kullanıcı kendi siparişini iptal eder)
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.GetUserId(); // extension ile alınıyor
            var result = await _orderService.CancelOrderAsync(orderId, userId);
            if (!result)
                return BadRequest(new { message = "Sipariş iptal edilemedi veya yetkiniz yok." });
            return Ok(new { success = true, message = "Sipariş başarıyla iptal edildi." });
        }

        /// <summary>
        /// Ödeme sağlayıcısından veya frontend'den tetiklenen ödeme başarısız senaryosu.
        /// Sipariş durumu PaymentFailed olarak işaretlenir.
        /// </summary>
        [HttpPost("{orderId:int}/payment-failed")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentFailed(int orderId)
        {
            var ok = await _orderService.MarkPaymentFailedAsync(orderId);
            if (!ok)
            {
                return NotFound(new { message = "Sipariş bulunamadı.", orderId });
            }

            return BadRequest(new { message = "Ödeme işlemi başarısız oldu.", orderId });
        }
        /// <summary>
        /// Sipariş için PDF fatura indir
        /// </summary>
        [HttpGet("{orderId}/invoice")]
        [Authorize]
        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            var orderDetail = await _orderService.GetDetailByIdAsync(orderId);
            if (orderDetail == null)
                return NotFound();

            // Sadece sipariş sahibi veya admin/super admin fatura indirebilsin
            var currentUserId = User.GetUserId();
            var isAdminLike = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
            if (!isAdminLike && orderDetail.UserId != currentUserId)
            {
                return Forbid();
            }

            var pdfBytes = InvoiceGenerator.Generate(orderDetail);
            return File(pdfBytes, "application/pdf", $"invoice-{orderId}.pdf");
        }

        /// <summary>
        /// Sipariş durumunu günceller (sadece adminler).
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Status alanı zorunludur." });

            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            return NoContent();
        }

        private int? ResolveUserId(int? incomingUserId)
        {
            if (incomingUserId.HasValue && incomingUserId.Value > 0)
                return incomingUserId;

            var claimUserId = User.GetUserId();
            if (claimUserId > 0)
                return claimUserId;

            return null;
        }
    }
}
