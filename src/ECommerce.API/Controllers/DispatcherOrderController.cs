using System;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ECommerce.API.Hubs;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Sevkiyat Görevlisi (Dispatcher) Sipariş Yönetim API'si
    /// 
    /// Bu controller sevkiyat görevlisi panelinde kullanılacak tüm endpoint'leri içerir.
    /// Yetkilendirme: Dispatcher veya Admin rolü gerektirir.
    /// 
    /// Özellikler:
    /// - Hazır siparişleri listeleme
    /// - Müsait kuryeleri listeleme
    /// - Siparişe kurye atama
    /// - Kurye değiştirme (reassign)
    /// - Gerçek zamanlı SignalR bildirimleri
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Dispatcher,StoreAttendant,SuperAdmin")]
    public class DispatcherOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IHubContext<DispatcherHub> _dispatcherHubContext;
        private readonly IHubContext<CourierHub> _courierHubContext;
        private readonly IHubContext<StoreAttendantHub> _storeHubContext;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<DispatcherOrderController> _logger;

        public DispatcherOrderController(
            IOrderService orderService,
            IHubContext<DispatcherHub> dispatcherHubContext,
            IHubContext<CourierHub> courierHubContext,
            IHubContext<StoreAttendantHub> storeHubContext,
            IRealTimeNotificationService notificationService,
            ILogger<DispatcherOrderController> logger)
        {
            _orderService = orderService;
            _dispatcherHubContext = dispatcherHubContext;
            _courierHubContext = courierHubContext;
            _storeHubContext = storeHubContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // SİPARİŞ LİSTELEME ENDPOINT'LERİ
        // ============================================================

        /// <summary>
        /// Sevkiyat görevlisi için siparişleri listeler.
        /// Ready, Assigned, OutForDelivery ve DeliveryFailed durumundaki siparişler görüntülenir.
        /// </summary>
        /// <param name="filter">Filtre ve sayfalama parametreleri</param>
        /// <returns>Sipariş listesi ve özet istatistikler</returns>
        [HttpGet("orders")]
        [ProducesResponseType(typeof(DispatcherOrderListResponseDto), 200)]
        public async Task<IActionResult> GetOrders([FromQuery] DispatcherOrderFilterDto? filter)
        {
            try
            {
                var result = await _orderService.GetOrdersForDispatcherAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sevkiyat görevlisi siparişleri listelenirken hata oluştu");
                return StatusCode(500, new { error = "Siparişler yüklenirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Sevkiyat görevlisi için özet istatistikleri döner.
        /// </summary>
        /// <returns>Hazır, atanmış, dağıtımdaki sipariş sayıları ve kurye bilgileri</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(DispatcherSummaryDto), 200)]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var summary = await _orderService.GetDispatcherSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sevkiyat görevlisi özet bilgileri alınırken hata oluştu");
                return StatusCode(500, new { error = "Özet bilgiler yüklenirken bir hata oluştu." });
            }
        }

        // ============================================================
        // KURYE LİSTELEME ENDPOINT'LERİ
        // ============================================================

        /// <summary>
        /// Müsait kuryeleri listeler.
        /// Aktif, online ve 5'ten az aktif siparişi olan kuryeler "müsait" olarak işaretlenir.
        /// </summary>
        /// <returns>Kurye listesi ve durum özeti</returns>
        [HttpGet("couriers")]
        [ProducesResponseType(typeof(DispatcherCourierListResponseDto), 200)]
        public async Task<IActionResult> GetAvailableCouriers()
        {
            try
            {
                var result = await _orderService.GetAvailableCouriersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuryeler listelenirken hata oluştu");
                return StatusCode(500, new { error = "Kuryeler yüklenirken bir hata oluştu." });
            }
        }

        // ============================================================
        // KURYE ATAMA ENDPOINT'LERİ
        // ============================================================

        /// <summary>
        /// Siparişe kurye atar.
        /// Sadece Ready veya DeliveryFailed durumundaki siparişler için kullanılabilir.
        /// SignalR ile kuryeye sesli bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="request">Kurye ID ve opsiyonel notlar</param>
        /// <returns>Atama sonucu</returns>
        [HttpPost("orders/{orderId}/assign")]
        [ProducesResponseType(typeof(AssignCourierResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AssignCourier(int orderId, [FromBody] AssignCourierRequestDto request)
        {
            try
            {
                if (request == null || request.CourierId <= 0)
                {
                    return BadRequest(new { error = "Geçersiz kurye ID." });
                }

                var userName = GetUserName();
                var result = await _orderService.AssignCourierToOrderAsync(
                    orderId, 
                    request.CourierId, 
                    userName, 
                    request.Notes
                );

                if (!result.Success)
                {
                    return BadRequest(new { error = result.Message });
                }

                // SignalR ile TÜM TARAFLARA bildir (Admin, Store, Dispatcher, Müşteri)
                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    result.Order?.Id ?? orderId,
                    result.Order?.OrderNumber ?? $"#{orderId}",
                    "Ready",
                    "Assigned",
                    userName,
                    request.CourierId
                );

                // SignalR ile KURYEYE SESLİ BİLDİRİM gönder
                await _courierHubContext.Clients.Group($"courier-{request.CourierId}").SendAsync("NewOrderAssigned", new
                {
                    orderId = result.Order?.Id,
                    orderNumber = result.Order?.OrderNumber,
                    status = "Assigned",
                    statusText = "Yeni Sipariş Atandı",
                    totalAmount = result.Order?.TotalAmount,
                    deliveryAddress = result.Order?.DeliveryAddress,
                    notes = request.Notes,
                    timestamp = DateTime.UtcNow,
                    playSound = true, // Kurye için ses çal
                    soundType = "new_assignment" // Yeni atama sesi
                });

                _logger.LogInformation("Sipariş #{OrderId} kuryeye atandı. Kurye: {CourierId}, Görevli: {UserName}", 
                    orderId, request.CourierId, userName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} kuryeye atanırken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Siparişin kuryesini değiştirir.
        /// Sadece Assigned veya DeliveryFailed durumundaki siparişler için kullanılabilir.
        /// Hem eski hem de yeni kuryeye SignalR bildirimi gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="request">Yeni kurye ID ve değişiklik nedeni</param>
        /// <returns>Değişiklik sonucu</returns>
        [HttpPost("orders/{orderId}/reassign")]
        [ProducesResponseType(typeof(AssignCourierResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReassignCourier(int orderId, [FromBody] ReassignCourierRequestDto request)
        {
            try
            {
                if (request == null || request.NewCourierId <= 0)
                {
                    return BadRequest(new { error = "Geçersiz yeni kurye ID." });
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { error = "Kurye değişikliği için bir neden belirtmelisiniz." });
                }

                var userName = GetUserName();
                var result = await _orderService.ReassignCourierAsync(
                    orderId, 
                    request.NewCourierId, 
                    userName, 
                    request.Reason
                );

                if (!result.Success)
                {
                    return BadRequest(new { error = result.Message });
                }

                // SignalR ile sevkiyat görevlilerine bildir
                await _dispatcherHubContext.Clients.All.SendAsync("OrderReassigned", new
                {
                    orderId = result.Order?.Id,
                    orderNumber = result.Order?.OrderNumber,
                    oldCourierId = request.OldCourierId,
                    newCourierId = result.Courier?.Id,
                    newCourierName = result.Courier?.Name,
                    reason = request.Reason,
                    reassignedBy = userName,
                    timestamp = DateTime.UtcNow
                });

                // Eski kuryeye bildirim gönder (sipariş iptal edildi)
                if (request.OldCourierId > 0)
                {
                    await _courierHubContext.Clients.Group($"courier-{request.OldCourierId}").SendAsync("OrderCancelled", new
                    {
                        orderId = result.Order?.Id,
                        orderNumber = result.Order?.OrderNumber,
                        reason = request.Reason,
                        timestamp = DateTime.UtcNow,
                        playSound = true,
                        soundType = "order_cancelled"
                    });
                }

                // Yeni kuryeye SESLİ BİLDİRİM gönder
                await _courierHubContext.Clients.Group($"courier-{request.NewCourierId}").SendAsync("NewOrderAssigned", new
                {
                    orderId = result.Order?.Id,
                    orderNumber = result.Order?.OrderNumber,
                    status = "Assigned",
                    statusText = "Yeni Sipariş Atandı",
                    totalAmount = result.Order?.TotalAmount,
                    deliveryAddress = result.Order?.DeliveryAddress,
                    isReassignment = true,
                    timestamp = DateTime.UtcNow,
                    playSound = true,
                    soundType = "new_assignment"
                });

                _logger.LogInformation("Sipariş #{OrderId} yeni kuryeye atandı. Eski: {OldCourierId}, Yeni: {NewCourierId}, Neden: {Reason}", 
                    orderId, request.OldCourierId, request.NewCourierId, request.Reason);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} kuryesi değiştirilirken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Acil siparişleri listeler (30+ dakika bekleyen hazır siparişler).
        /// </summary>
        /// <returns>Acil sipariş listesi</returns>
        [HttpGet("orders/urgent")]
        [ProducesResponseType(typeof(DispatcherOrderListResponseDto), 200)]
        public async Task<IActionResult> GetUrgentOrders()
        {
            try
            {
                var filter = new DispatcherOrderFilterDto
                {
                    UrgentOnly = true,
                    PageSize = 50
                };
                var result = await _orderService.GetOrdersForDispatcherAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Acil siparişler listelenirken hata oluştu");
                return StatusCode(500, new { error = "Siparişler yüklenirken bir hata oluştu." });
            }
        }

        // ============================================================
        // YARDIMCI METODLAR
        // ============================================================

        /// <summary>
        /// Mevcut kullanıcının adını döner.
        /// </summary>
        private string GetUserName()
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) 
                ?? User.FindFirstValue("name") 
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? "Bilinmeyen Görevli";
            return userName;
        }
    }
}
