using System;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using System.Threading.Tasks;
using System.Security.Claims;
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
        private readonly IAuditLogService _auditLogService;

        public AdminOrdersController(IOrderService orderService, IAuditLogService auditLogService)
        {
            _orderService = orderService;
            _auditLogService = auditLogService;
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
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "OrderDeleted",
                "Order",
                id.ToString(),
                new
                {
                    existing.Status,
                    existing.TotalPrice,
                    existing.OrderNumber
                },
                null);
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

            var oldOrder = await _orderService.GetByIdAsync(id);

            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            if (oldOrder != null)
            {
                var updatedOrder = await _orderService.GetByIdAsync(id);
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "OrderStatusChanged",
                    "Order",
                    id.ToString(),
                    new { oldOrder.Status },
                    updatedOrder != null ? new { updatedOrder.Status } : null);
            }
            return NoContent();
        }

        [HttpPost("{id:int}/prepare")]
        public Task<IActionResult> PrepareOrder(int id)
        {
            return HandleStatusChange(id, () => _orderService.MarkOrderAsPreparingAsync(id), "OrderStatusChanged");
        }

        [HttpPost("{id:int}/out-for-delivery")]
        public Task<IActionResult> MarkOutForDelivery(int id)
        {
            return HandleStatusChange(id, () => _orderService.MarkOrderOutForDeliveryAsync(id), "OrderStatusChanged");
        }

        [HttpPost("{id:int}/deliver")]
        public Task<IActionResult> DeliverOrder(int id)
        {
            return HandleStatusChange(id, () => _orderService.MarkOrderAsDeliveredAsync(id), "OrderStatusChanged");
        }

        [HttpPost("{id:int}/cancel")]
        public Task<IActionResult> CancelOrder(int id)
        {
            return HandleStatusChange(id, () => _orderService.CancelOrderByAdminAsync(id), "OrderStatusChanged");
        }

        [HttpPost("{id:int}/refund")]
        public Task<IActionResult> RefundOrder(int id)
        {
            return HandleStatusChange(id, () => _orderService.RefundOrderAsync(id), "OrderStatusChanged");
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 5)
        {
            var orders = await _orderService.GetRecentOrdersAsync(count);
            return Ok(orders);
        }

        private async Task<IActionResult> HandleStatusChange(int orderId, Func<Task<OrderListDto?>> action, string auditAction)
        {
            var oldOrder = await _orderService.GetByIdAsync(orderId);
            if (oldOrder == null)
                return NotFound();

            try
            {
                var order = await action();
                if (order == null)
                    return NotFound();
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    auditAction,
                    "Order",
                    orderId.ToString(),
                    new { oldOrder.Status },
                    new { order.Status });
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private int GetAdminUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
        }
    }
}
