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
using Microsoft.Extensions.Logging;
using ECommerce.Entities.Enums;
/*OrdersController
•	POST /api/orders (guest veya userId ile) -> sipariş oluşturma. Eğer guest ise, zorunlu alanlar: name, phone, email, address, paymentMethod
•	GET /api/orders/{id} -> sipariş detay (admin, ilgili kullanıcı veya kurye görmeli)
•	GET /api/orders/user/{userId} -> kullanıcı siparişleri
*/
namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.OrderManagement)] // CustomerSupport ve Logistics de erişebilsin
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IRefundService _refundService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<AdminOrdersController> _logger;

        public AdminOrdersController(
            IOrderService orderService,
            IRefundService refundService,
            IAuditLogService auditLogService,
            IRealTimeNotificationService notificationService,
            ILogger<AdminOrdersController> logger)
        {
            _orderService = orderService;
            _refundService = refundService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _logger = logger;
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

        /// <summary>
        /// Admin/Market görevlisi siparişi iptal eder ve para iadesini tetikler.
        /// Ödeme yapılmışsa otomatik POSNET reverse/return yapılır.
        /// POST /api/admin/orders/{id}/cancel-with-refund
        /// Body: { "reason": "İptal sebebi" }
        /// </summary>
        [HttpPost("{id:int}/cancel-with-refund")]
        public async Task<IActionResult> CancelOrderWithRefund(int id, [FromBody] Dictionary<string, string>? body)
        {
            var reason = "Admin tarafından iptal edildi";
            if (body != null && body.TryGetValue("reason", out var r) && !string.IsNullOrWhiteSpace(r))
                reason = r;

            var adminUserId = GetAdminUserId();
            var result = await _refundService.AdminCancelOrderWithRefundAsync(id, adminUserId, reason);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errorCode = result.ErrorCode
                });
            }

            // Audit log
            await _auditLogService.WriteAsync(
                adminUserId,
                "OrderCancelledWithRefund",
                "Order",
                id.ToString(),
                null,
                new { reason, result.RefundRequest?.TransactionType });

            return Ok(new
            {
                success = true,
                message = result.Message,
                autoCancelled = result.AutoCancelled,
                refundRequest = result.RefundRequest
            });
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

                // =====================================================================
                // KURYE BİLDİRİMİ GÖNDER
                // NEDEN: Kurye siparişin kendisine atandığını anlık olarak bilmeli
                // Bu sayede sipariş hazır olduğunda hemen harekete geçebilir
                // =====================================================================
                try
                {
                    await _notificationService.NotifyOrderAssignedToCourierAsync(
                        dto.CourierId,
                        updatedOrder.Id,
                        updatedOrder.OrderNumber ?? $"#{updatedOrder.Id}",
                        updatedOrder.ShippingAddress,
                        updatedOrder.CustomerPhone,
                        updatedOrder.FinalPrice,
                        "online" // Ödeme yöntemi - API'den alınabilir
                    );

                    // Durum değişikliği bildirimi (tüm taraflara)
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        updatedOrder.Id,
                        updatedOrder.OrderNumber ?? $"#{updatedOrder.Id}",
                        oldOrder.Status,
                        updatedOrder.Status,
                        GetAdminUserId().ToString()
                    );
                }
                catch (Exception notifyEx)
                {
                    // Bildirim hatası kurye atamasını engellemez
                    // Sadece loglama yapılır
                    _logger.LogWarning(notifyEx, 
                        "Kurye bildirimi gönderilemedi. OrderId={OrderId}, CourierId={CourierId}", 
                        id, dto.CourierId);
                }

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

        // ============================================================================
        // İADE TALEBİ YÖNETİM ENDPOINTLERİ (ADMİN / MÜŞTERİ HİZMETLERİ)
        // Bekleyen iade taleplerini listeleme, onaylama, reddetme ve yeniden deneme
        // ============================================================================

        /// <summary>
        /// Tüm iade taleplerini listeler.
        /// Opsiyonel status filtresi: pending, approved, rejected, refunded, autoCancelled, refundFailed
        /// </summary>
        [HttpGet("refund-requests")]
        public async Task<IActionResult> GetRefundRequests([FromQuery] string? status = null)
        {
            RefundRequestStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RefundRequestStatus>(status, true, out var parsed))
            {
                statusFilter = parsed;
            }

            var requests = await _refundService.GetAllRefundRequestsAsync(statusFilter);
            return Ok(new { success = true, data = requests });
        }

        /// <summary>
        /// Bekleyen iade taleplerini listeler (dashboard widget).
        /// </summary>
        [HttpGet("refund-requests/pending")]
        public async Task<IActionResult> GetPendingRefundRequests()
        {
            var requests = await _refundService.GetPendingRefundRequestsAsync();
            return Ok(new { success = true, data = requests, count = requests.Count() });
        }

        /// <summary>
        /// Admin iade talebi onaylar veya reddeder.
        /// Onay durumunda POSNET üzerinden para iadesi tetiklenir.
        /// </summary>
        [HttpPost("refund-requests/{refundRequestId:int}/process")]
        public async Task<IActionResult> ProcessRefundRequest(
            int refundRequestId, [FromBody] ProcessRefundDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var adminUserId = GetAdminUserId();
            var result = await _refundService.ProcessRefundRequestAsync(refundRequestId, adminUserId, dto);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errorCode = result.ErrorCode
                });
            }

            // Audit log
            await _auditLogService.WriteAsync(
                adminUserId,
                dto.Approve ? "RefundApproved" : "RefundRejected",
                "RefundRequest",
                refundRequestId.ToString(),
                null,
                new { dto.Approve, dto.AdminNote, dto.RefundAmount });

            return Ok(new
            {
                success = true,
                message = result.Message,
                refundRequest = result.RefundRequest
            });
        }

        /// <summary>
        /// Başarısız para iadesini yeniden dener.
        /// RefundFailed durumundaki talepler için admin müdahalesi.
        /// </summary>
        [HttpPost("refund-requests/{refundRequestId:int}/retry")]
        public async Task<IActionResult> RetryRefund(int refundRequestId)
        {
            var adminUserId = GetAdminUserId();
            var result = await _refundService.RetryRefundAsync(refundRequestId, adminUserId);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errorCode = result.ErrorCode
                });
            }

            await _auditLogService.WriteAsync(
                adminUserId,
                "RefundRetry",
                "RefundRequest",
                refundRequestId.ToString(),
                null,
                new { result.RefundRequest?.Status });

            return Ok(new
            {
                success = true,
                message = result.Message,
                refundRequest = result.RefundRequest
            });
        }

        /// <summary>
        /// Belirli bir siparişin iade taleplerini getirir.
        /// </summary>
        [HttpGet("{orderId:int}/refund-requests")]
        public async Task<IActionResult> GetOrderRefundRequests(int orderId)
        {
            var requests = await _refundService.GetRefundRequestsByOrderAsync(orderId);
            return Ok(new { success = true, data = requests });
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
