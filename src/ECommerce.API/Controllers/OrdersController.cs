using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;

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
    }
}

