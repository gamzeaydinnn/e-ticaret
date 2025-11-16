using System;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using System.Threading.Tasks;
/*OrdersController
•	POST /api/orders (guest veya userId ile) -> sipariş oluşturma. Eğer guest ise, zorunlu alanlar: name, phone, email, address, paymentMethod
•	GET /api/orders/{id} -> sipariş detay (admin, ilgili kullanıcı veya kurye görmeli)
•	GET /api/orders/user/{userId} -> kullanıcı siparişleri
*/
namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)] // sadece admin erişebilir
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public AdminOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var existing = await _orderService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _orderService.DeleteAsync(id);
            return NoContent();
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            var order = await _orderService.CreateAsync(dto);
            return Ok(order);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Status alanı zorunludur." });

            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            return NoContent();
        }

        [HttpPost("{id:int}/prepare")]
        public Task<IActionResult> PrepareOrder(int id)
        {
            return HandleStatusChange(() => _orderService.MarkOrderAsPreparingAsync(id));
        }

        [HttpPost("{id:int}/out-for-delivery")]
        public Task<IActionResult> MarkOutForDelivery(int id)
        {
            return HandleStatusChange(() => _orderService.MarkOrderOutForDeliveryAsync(id));
        }

        [HttpPost("{id:int}/deliver")]
        public Task<IActionResult> DeliverOrder(int id)
        {
            return HandleStatusChange(() => _orderService.MarkOrderAsDeliveredAsync(id));
        }

        [HttpPost("{id:int}/cancel")]
        public Task<IActionResult> CancelOrder(int id)
        {
            return HandleStatusChange(() => _orderService.CancelOrderByAdminAsync(id));
        }

        [HttpPost("{id:int}/refund")]
        public Task<IActionResult> RefundOrder(int id)
        {
            return HandleStatusChange(() => _orderService.RefundOrderAsync(id));
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 5)
        {
            var orders = await _orderService.GetRecentOrdersAsync(count);
            return Ok(orders);
        }

        private async Task<IActionResult> HandleStatusChange(Func<Task<OrderListDto?>> action)
        {
            try
            {
                var order = await action();
                if (order == null)
                    return NotFound();
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
