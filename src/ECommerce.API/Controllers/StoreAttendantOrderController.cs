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
    /// Market Görevlisi (Store Attendant) Sipariş Yönetim API'si
    /// 
    /// Bu controller market görevlisi panelinde kullanılacak tüm endpoint'leri içerir.
    /// Yetkilendirme: StoreAttendant veya Admin rolü gerektirir.
    /// 
    /// Özellikler:
    /// - Onaylanmış siparişleri listeleme
    /// - Sipariş hazırlamaya başlama
    /// - Siparişi hazır olarak işaretleme (opsiyonel ağırlık girişi)
    /// - Gerçek zamanlı SignalR bildirimleri
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,StoreAttendant")]
    public class StoreAttendantOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IHubContext<DispatcherHub> _dispatcherHubContext;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<StoreAttendantOrderController> _logger;

        public StoreAttendantOrderController(
            IOrderService orderService,
            IHubContext<DispatcherHub> dispatcherHubContext,
            IRealTimeNotificationService notificationService,
            ILogger<StoreAttendantOrderController> logger)
        {
            _orderService = orderService;
            _dispatcherHubContext = dispatcherHubContext;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ============================================================
        // SİPARİŞ LİSTELEME ENDPOINT'LERİ
        // ============================================================

        /// <summary>
        /// Market görevlisi için siparişleri listeler.
        /// Sadece Confirmed, Preparing ve Ready durumundaki siparişler görüntülenir.
        /// </summary>
        /// <param name="filter">Filtre ve sayfalama parametreleri</param>
        /// <returns>Sipariş listesi ve özet istatistikler</returns>
        [HttpGet("orders")]
        [ProducesResponseType(typeof(StoreAttendantOrderListResponseDto), 200)]
        public async Task<IActionResult> GetOrders([FromQuery] StoreAttendantOrderFilterDto? filter)
        {
            try
            {
                var result = await _orderService.GetOrdersForStoreAttendantAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Market görevlisi siparişleri listelenirken hata oluştu");
                return StatusCode(500, new { error = "Siparişler yüklenirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Market görevlisi için özet istatistikleri döner.
        /// </summary>
        /// <returns>Bekleyen, hazırlanan ve hazır sipariş sayıları</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(StoreAttendantSummaryDto), 200)]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var summary = await _orderService.GetStoreAttendantSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Market görevlisi özet bilgileri alınırken hata oluştu");
                return StatusCode(500, new { error = "Özet bilgiler yüklenirken bir hata oluştu." });
            }
        }

        // ============================================================
        // SİPARİŞ DURUM DEĞİŞİKLİK ENDPOINT'LERİ
        // ============================================================

        /// <summary>
        /// Siparişi "Hazırlanıyor" durumuna geçirir.
        /// Sadece Confirmed durumundaki siparişler için kullanılabilir.
        /// SignalR ile tüm bağlı istemcilere bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/start-preparing")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> StartPreparing(int orderId)
        {
            try
            {
                var userName = GetUserName();
                var result = await _orderService.StartPreparingAsync(orderId, userName);

                if (result == null)
                {
                    return NotFound(new { error = "Sipariş bulunamadı." });
                }

                // SignalR ile TÜM TARAFLARA bildir (Admin, Store, Dispatcher, Müşteri)
                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    result.Id,
                    result.OrderNumber,
                    "Confirmed",
                    "Preparing",
                    userName
                );

                _logger.LogInformation("Sipariş #{OrderId} hazırlanmaya başlandı. Görevli: {UserName}", orderId, userName);

                return Ok(new
                {
                    success = true,
                    message = "Sipariş hazırlanmaya başlandı.",
                    order = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} hazırlanırken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Siparişi "Hazır" durumuna geçirir.
        /// Sadece Preparing durumundaki siparişler için kullanılabilir.
        /// Opsiyonel olarak ağırlık bilgisi girilebilir.
        /// SignalR ile sevkiyat görevlilerine sesli bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="request">Ağırlık bilgisi (opsiyonel)</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/mark-ready")]
        [ProducesResponseType(typeof(MarkOrderReadyResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkReady(int orderId, [FromBody] MarkOrderReadyRequestDto? request)
        {
            try
            {
                var userName = GetUserName();
                var result = await _orderService.MarkOrderAsReadyAsync(orderId, userName, request?.WeightInGrams);

                if (result == null)
                {
                    return NotFound(new { error = "Sipariş bulunamadı." });
                }

                // SignalR ile TÜM TARAFLARA bildir (Admin, Store, Dispatcher, Müşteri)
                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    result.Id,
                    result.OrderNumber,
                    "Preparing",
                    "Ready",
                    userName
                );

                // Sevkiyat görevlilerine ek olarak SESLİ BİLDİRİM gönder
                await _dispatcherHubContext.Clients.All.SendAsync("OrderReady", new
                {
                    orderId = result.Id,
                    orderNumber = result.OrderNumber,
                    status = "Ready",
                    statusText = "Hazır",
                    totalAmount = result.FinalPrice,
                    itemCount = result.TotalItems,
                    weightInGrams = request?.WeightInGrams,
                    timestamp = DateTime.UtcNow,
                    playSound = true, // Sevkiyat görevlisi için ses çal
                    soundType = "order_ready" // Hangi sesi çalacağını belirt
                });

                _logger.LogInformation("Sipariş #{OrderId} hazır olarak işaretlendi. Görevli: {UserName}, Ağırlık: {Weight}g", 
                    orderId, userName, request?.WeightInGrams ?? 0);

                return Ok(new MarkOrderReadyResponseDto
                {
                    Success = true,
                    Message = "Sipariş hazır olarak işaretlendi.",
                    Order = new StoreAttendantOrderDto
                    {
                        Id = result.Id,
                        OrderNumber = result.OrderNumber ?? $"#{result.Id}",
                        Status = "Ready",
                        StatusText = "Hazır",
                        TotalAmount = result.FinalPrice,
                        ItemCount = result.TotalItems,
                        ReadyAt = DateTime.UtcNow,
                        WeightInGrams = request?.WeightInGrams
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} hazır işaretlenirken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Siparişi onaylar (Admin ve Confirmed durumuna geçirir).
        /// Sadece New veya Paid durumundaki siparişler için kullanılabilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/confirm")]
        [Authorize(Roles = "Admin")] // Sadece admin onaylayabilir
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConfirmOrder(int orderId)
        {
            try
            {
                var userName = GetUserName();
                var result = await _orderService.MarkOrderAsConfirmedAsync(orderId, userName);

                if (result == null)
                {
                    return NotFound(new { error = "Sipariş bulunamadı." });
                }

                await _notificationService.NotifyStoreAttendantOrderConfirmedAsync(
                    result.Id,
                    result.OrderNumber ?? $"#{result.Id}",
                    userName,
                    DateTime.UtcNow);

                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    result.Id,
                    result.OrderNumber ?? $"#{result.Id}",
                    "Pending",
                    "Confirmed",
                    userName);

                _logger.LogInformation("Sipariş #{OrderId} onaylandı. Admin: {UserName}", orderId, userName);

                return Ok(new
                {
                    success = true,
                    message = "Sipariş onaylandı.",
                    order = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} onaylanırken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
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
