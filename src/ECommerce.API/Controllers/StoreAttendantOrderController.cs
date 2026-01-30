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
        /// Admin ve StoreAttendant yetkisine sahiptir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/confirm")]
        [Authorize(Roles = "Admin,StoreAttendant")] // Admin ve StoreAttendant onaylayabilir
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
        // GENİŞLETİLMİŞ SİPARİŞ YÖNETİM ENDPOINT'LERİ
        // Market görevlisi artık admin ile aynı yetkilere sahip
        // ============================================================

        /// <summary>
        /// Sipariş durumunu günceller (Admin ile aynı yetki).
        /// Tüm statü geçişleri için kullanılabilir.
        /// SignalR ile tüm taraflara (Admin, Store, Dispatcher, Müşteri) bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="dto">Yeni durum bilgisi</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPut("orders/{orderId}/status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] OrderStatusUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { error = "Status alanı zorunludur." });

            try
            {
                var userName = GetUserName();
                var oldOrder = await _orderService.GetByIdAsync(orderId);
                
                if (oldOrder == null)
                    return NotFound(new { error = "Sipariş bulunamadı." });

                // Durum güncelleme işlemi
                await _orderService.UpdateOrderStatusAsync(orderId, dto.Status);
                var updatedOrder = await _orderService.GetByIdAsync(orderId);

                if (updatedOrder != null)
                {
                    // Tüm taraflara bildirim gönder
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        updatedOrder.Id,
                        updatedOrder.OrderNumber ?? $"#{updatedOrder.Id}",
                        oldOrder.Status,
                        updatedOrder.Status,
                        userName
                    );
                }

                _logger.LogInformation(
                    "Sipariş #{OrderId} durumu güncellendi: {OldStatus} -> {NewStatus}. Görevli: {UserName}", 
                    orderId, oldOrder.Status, dto.Status, userName);

                return Ok(new
                {
                    success = true,
                    message = "Sipariş durumu güncellendi.",
                    order = updatedOrder
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} durumu güncellenirken hata oluştu", orderId);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Siparişe kurye atar (Admin ile aynı yetki).
        /// Kurye ataması sonrası tüm taraflara ve kuryeye bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="dto">Kurye bilgisi</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/assign-courier")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AssignCourier(int orderId, [FromBody] AssignCourierDto dto)
        {
            // Validation: DTO null veya courierId eksik mi?
            if (dto == null || dto.CourierId <= 0)
            {
                return BadRequest(new { error = "Geçerli bir kurye ID'si gereklidir." });
            }

            try
            {
                var userName = GetUserName();
                var userId = GetUserId();

                // Önceki durumu al (audit ve bildirim için)
                var oldOrder = await _orderService.GetByIdAsync(orderId);
                if (oldOrder == null)
                {
                    return NotFound(new { error = "Sipariş bulunamadı." });
                }

                // Kurye atamasını gerçekleştir
                var updatedOrder = await _orderService.AssignCourierAsync(orderId, dto.CourierId);
                
                if (updatedOrder == null)
                {
                    return NotFound(new { error = "Sipariş güncellenemedi." });
                }

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
                        "online" // Ödeme yöntemi
                    );

                    // Durum değişikliği bildirimi (tüm taraflara: Admin, Store, Dispatcher, Müşteri)
                    await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                        updatedOrder.Id,
                        updatedOrder.OrderNumber ?? $"#{updatedOrder.Id}",
                        oldOrder.Status,
                        updatedOrder.Status,
                        userName
                    );
                }
                catch (Exception notifyEx)
                {
                    // Bildirim hatası kurye atamasını engellemez, sadece log tutulur
                    _logger.LogWarning(notifyEx, 
                        "Kurye bildirimi gönderilemedi. OrderId={OrderId}, CourierId={CourierId}", 
                        orderId, dto.CourierId);
                }

                _logger.LogInformation(
                    "Sipariş #{OrderId} için kurye atandı. CourierId={CourierId}, Görevli: {UserName}", 
                    orderId, dto.CourierId, userName);

                return Ok(new
                {
                    success = true,
                    message = "Kurye başarıyla atandı.",
                    order = updatedOrder
                });
            }
            catch (InvalidOperationException ex)
            {
                // Kurye bulunamadı gibi iş mantığı hataları
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} için kurye atanırken hata oluştu", orderId);
                return StatusCode(500, new { error = "Kurye atama sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Siparişi "Dağıtımda" durumuna geçirir (Admin ile aynı yetki).
        /// Kurye atandıktan sonra teslimat sürecini başlatır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/out-for-delivery")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkOutForDelivery(int orderId)
        {
            return await HandleStatusChange(orderId, 
                () => _orderService.MarkOrderOutForDeliveryAsync(orderId), 
                "OutForDelivery");
        }

        /// <summary>
        /// Siparişi "Teslim Edildi" durumuna geçirir (Admin ile aynı yetki).
        /// Teslimat tamamlandığında kullanılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/deliver")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeliverOrder(int orderId)
        {
            return await HandleStatusChange(orderId, 
                () => _orderService.MarkOrderAsDeliveredAsync(orderId), 
                "Delivered");
        }

        /// <summary>
        /// Siparişi iptal eder (Admin ile aynı yetki).
        /// İptal edilebilir durumdaki siparişler için kullanılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/cancel")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            return await HandleStatusChange(orderId, 
                () => _orderService.CancelOrderByAdminAsync(orderId), 
                "Cancelled");
        }

        /// <summary>
        /// Sipariş iade işlemi yapar (Admin ile aynı yetki).
        /// Teslim edilmiş siparişler için iade süreci başlatır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        [HttpPost("orders/{orderId}/refund")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RefundOrder(int orderId)
        {
            return await HandleStatusChange(orderId, 
                () => _orderService.RefundOrderAsync(orderId), 
                "Refunded");
        }

        /// <summary>
        /// Sipariş detayını getirir (Admin ile aynı yetki).
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Sipariş detay bilgisi</returns>
        [HttpGet("orders/{orderId}")]
        [ProducesResponseType(typeof(OrderListDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(orderId);
                if (order == null)
                    return NotFound(new { error = "Sipariş bulunamadı." });

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} detayı alınırken hata oluştu", orderId);
                return StatusCode(500, new { error = "Sipariş detayı yüklenirken bir hata oluştu." });
            }
        }

        // ============================================================
        // YARDIMCI METODLAR
        // ============================================================

        /// <summary>
        /// Durum değişikliği işlemlerini ortak olarak yöneten yardımcı metod.
        /// DRY prensibi: Tekrar eden kodları merkezileştirir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="action">Çalıştırılacak servis metodu</param>
        /// <param name="statusName">Log için durum adı</param>
        /// <returns>API yanıtı</returns>
        private async Task<IActionResult> HandleStatusChange(
            int orderId, 
            Func<Task<OrderListDto?>> action, 
            string statusName)
        {
            try
            {
                var userName = GetUserName();
                var oldOrder = await _orderService.GetByIdAsync(orderId);
                
                if (oldOrder == null)
                    return NotFound(new { error = "Sipariş bulunamadı." });

                var order = await action();
                if (order == null)
                    return NotFound(new { error = "Sipariş güncellenemedi." });

                // Tüm taraflara bildirim gönder
                await _notificationService.NotifyAllPartiesOrderStatusChangedAsync(
                    order.Id,
                    order.OrderNumber ?? $"#{order.Id}",
                    oldOrder.Status,
                    order.Status,
                    userName
                );

                _logger.LogInformation(
                    "Sipariş #{OrderId} durumu '{Status}' olarak güncellendi. Görevli: {UserName}", 
                    orderId, statusName, userName);

                return Ok(new
                {
                    success = true,
                    message = $"Sipariş durumu '{statusName}' olarak güncellendi.",
                    order = order
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} '{Status}' durumuna güncellenirken hata oluştu", orderId, statusName);
                return StatusCode(500, new { error = "İşlem sırasında bir hata oluştu." });
            }
        }

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

        /// <summary>
        /// Mevcut kullanıcının ID'sini döner.
        /// </summary>
        private int GetUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }
    }
}
