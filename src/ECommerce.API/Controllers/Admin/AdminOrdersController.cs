using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using System.Threading.Tasks;
using System.Security.Claims;
using ECommerce.API.Authorization;
/*OrdersController
•	POST /api/orders (guest veya userId ile) -> sipariş oluşturma. Eğer guest ise, zorunlu alanlar: name, phone, email, address, paymentMethod
•	GET /api/orders/{id} -> sipariş detay (admin, ilgili kullanıcı veya kurye görmeli)
•	GET /api/orders/user/{userId} -> kullanıcı siparişleri
*/
namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRealTimeNotificationService _notificationService;

        public AdminOrdersController(
            IOrderService orderService,
            IAuditLogService auditLogService,
            IRealTimeNotificationService notificationService)
        {
            _orderService = orderService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        [HttpPatch("{id:int}/tracking-number")]
        [HasPermission(Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> UpdateTrackingNumber(int id, [FromBody] Dictionary<string, string>? body)
        {
            // Sadece TrackingNumber alanını güncelle
            var db = HttpContext.RequestServices.GetService(typeof(ECommerce.Data.Context.ECommerceDbContext)) as ECommerce.Data.Context.ECommerceDbContext;
            if (db == null) return StatusCode(500, new { message = "Database context not available" });

            var order = await db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            string? tracking = null;
            if (body != null && body.TryGetValue("trackingNumber", out var val)) tracking = val;

            order.TrackingNumber = tracking;
            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [HasPermission(Permissions.Orders.View)]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Orders.Cancel)]
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
        [HasPermission(Permissions.Orders.ViewDetails)]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [HasPermission(Permissions.Orders.View)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            var order = await _orderService.CreateAsync(dto);
            try
            {
                var orderNumber = string.IsNullOrWhiteSpace(order.OrderNumber)
                    ? $"#{order.Id}"
                    : order.OrderNumber;
                var customerName = string.IsNullOrWhiteSpace(order.CustomerName)
                    ? "Müşteri"
                    : order.CustomerName;

                await _notificationService.NotifyNewOrderAsync(
                    order.Id,
                    orderNumber,
                    customerName,
                    order.FinalPrice,
                    order.TotalItems);

                await _notificationService.NotifyStoreAttendantNewOrderAsync(
                    order.Id,
                    orderNumber,
                    customerName,
                    order.TotalItems,
                    order.FinalPrice,
                    order.OrderDate);
            }
            catch
            {
                // Bildirim hatası admin akışını bozmasın
            }
            return Ok(order);
        }

        [HttpPut("{id}/status")]
        [HasPermission(Permissions.Orders.UpdateStatus)]
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
                if (updatedOrder != null)
                {
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        updatedOrder.Id,
                        updatedOrder.OrderNumber ?? $"#{updatedOrder.Id}",
                        oldOrder.Status,
                        updatedOrder.Status,
                        "Admin");
                }
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

        // ============================================================
        // KURYE ATAMA ENDPOINT
        // ============================================================
        /// <summary>
        /// Siparişe kurye atar.
        /// POST /api/admin/orders/{id}/assign-courier
        /// Body: { "courierId": 123 }
        /// </summary>
        [HttpPost("{id:int}/assign-courier")]
        [HasPermission(Permissions.Orders.UpdateStatus)]
        public async Task<IActionResult> AssignCourier(int id, [FromBody] AssignCourierDto dto)
        {
            // Validation: DTO null veya courierId eksik mi?
            if (dto == null || dto.CourierId <= 0)
            {
                return BadRequest(new { message = "Geçerli bir kurye ID'si gereklidir." });
            }

            try
            {
                // Önceki durumu al (audit için)
                var oldOrder = await _orderService.GetByIdAsync(id);
                if (oldOrder == null)
                {
                    return NotFound(new { message = "Sipariş bulunamadı." });
                }

                // Kurye atamasını gerçekleştir
                var updatedOrder = await _orderService.AssignCourierAsync(id, dto.CourierId);
                
                if (updatedOrder == null)
                {
                    return NotFound(new { message = "Sipariş güncellenemedi." });
                }

                // Audit log yaz
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "CourierAssigned",
                    "Order",
                    id.ToString(),
                    new { oldOrder.Status, OldCourierId = (int?)null },
                    new { updatedOrder.Status, CourierId = dto.CourierId });

                return Ok(updatedOrder);
            }
            catch (InvalidOperationException ex)
            {
                // Kurye bulunamadı gibi iş mantığı hataları
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Beklenmeyen hatalar
                return StatusCode(500, new { message = "Kurye atama sırasında bir hata oluştu.", detail = ex.Message });
            }
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
                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    order.Id,
                    order.OrderNumber ?? $"#{order.Id}",
                    oldOrder.Status,
                    order.Status,
                    "Admin");
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
