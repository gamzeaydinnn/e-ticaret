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

            // Basit alan doğrulamaları (adres, şehir zorunlu)
            if (string.IsNullOrWhiteSpace(dto.ShippingAddress) || string.IsNullOrWhiteSpace(dto.ShippingCity))
                return BadRequest(new { message = "Adres ve şehir zorunludur." });

            if (dto.OrderItems == null || dto.OrderItems.Count == 0)
                return BadRequest(new { message = "Sepet boş olamaz." });

            var result = await _orderService.CheckoutAsync(dto);

            return Ok(new
            {
                success = true,
                orderId = result.Id,
                orderNumber = result.OrderNumber,
                status = result.Status,
                totalPrice = result.TotalPrice
            });
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
        /// Sipariş için PDF fatura indir
        /// </summary>
        [HttpGet("{orderId}/invoice")]
        [Authorize]
        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            var orderDetail = await _orderService.GetDetailByIdAsync(orderId);
            if (orderDetail == null)
                return NotFound();

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
