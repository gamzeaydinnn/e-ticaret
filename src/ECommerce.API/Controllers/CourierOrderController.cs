using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Courier;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Kurye sipariş yönetimi API'leri.
    /// 
    /// Güvenlik:
    /// - Tüm endpoint'ler [Authorize(Roles = "Courier")] ile korunur
    /// - Her işlemde ownership kontrolü yapılır (order.CourierId == currentUser.CourierId)
    /// - Rate limiting ve audit logging aktif
    /// 
    /// Endpoint'ler:
    /// - GET  /api/courier/orders        - Atanan siparişleri listele
    /// - GET  /api/courier/orders/{id}   - Sipariş detayı
    /// - POST /api/courier/orders/{id}/start-delivery  - Yola çıkıldı
    /// - POST /api/courier/orders/{id}/delivered       - Teslim edildi
    /// - POST /api/courier/orders/{id}/problem         - Problem bildir
    /// - GET  /api/courier/summary       - Günlük özet
    /// </summary>
    [ApiController]
    [Route("api/courier/orders")]
    [Authorize(Roles = "Courier")]
    [Produces("application/json")]
    public class CourierOrderController : ControllerBase
    {
        private readonly ICourierOrderService _courierOrderService;
        private readonly ILogger<CourierOrderController> _logger;

        public CourierOrderController(
            ICourierOrderService courierOrderService,
            ILogger<CourierOrderController> logger)
        {
            _courierOrderService = courierOrderService ?? throw new ArgumentNullException(nameof(courierOrderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Sipariş Listeleme

        /// <summary>
        /// Kuryeye atanan siparişleri listeler.
        /// </summary>
        /// <remarks>
        /// Varsayılan olarak ASSIGNED ve OUT_FOR_DELIVERY durumundaki siparişleri getirir.
        /// Opsiyonel filtreleme parametreleri ile sonuçlar daraltılabilir.
        /// 
        /// Örnek İstek:
        ///     GET /api/courier/orders?status=assigned&amp;page=1&amp;pageSize=20
        /// </remarks>
        /// <param name="filter">Filtreleme ve sayfalama parametreleri</param>
        /// <returns>Sipariş listesi ve özet istatistikler</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CourierOrderListResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CourierOrderListResponseDto>> GetAssignedOrders([FromQuery] CourierOrderFilterDto? filter)
        {
            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                _logger.LogWarning("Kurye ID bulunamadı. UserId: {UserId}", GetCurrentUserId());
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            var result = await _courierOrderService.GetAssignedOrdersAsync(courierId.Value, filter);
            return Ok(result);
        }

        /// <summary>
        /// Belirli bir siparişin detaylı bilgisini getirir.
        /// </summary>
        /// <remarks>
        /// Sipariş detayı, müşteri bilgileri, ürün listesi ve izin verilen aksiyonları içerir.
        /// Sadece kuryeye atanan siparişlerin detayı görülebilir (ownership kontrolü).
        /// 
        /// Örnek İstek:
        ///     GET /api/courier/orders/123
        /// </remarks>
        /// <param name="id">Sipariş ID</param>
        /// <returns>Sipariş detayı veya 404 Not Found</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CourierOrderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourierOrderDetailDto>> GetOrderDetail([FromRoute] int id)
        {
            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            var order = await _courierOrderService.GetOrderDetailAsync(id, courierId.Value);
            if (order == null)
            {
                _logger.LogWarning("Sipariş #{OrderId} bulunamadı veya erişim yok. Kurye #{CourierId}", id, courierId.Value);
                return NotFound(new { message = "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok." });
            }

            return Ok(order);
        }

        #endregion

        #region Sipariş Aksiyonları

        /// <summary>
        /// Kurye teslimat için yola çıktığını bildirir (ASSIGNED → OUT_FOR_DELIVERY).
        /// </summary>
        /// <remarks>
        /// Bu işlem sonrasında:
        /// - Sipariş durumu "Yolda" olarak güncellenir
        /// - Müşteriye "Siparişiniz yola çıktı" bildirimi gönderilir
        /// - PickedUpAt timestamp kaydedilir
        /// 
        /// Sadece ASSIGNED durumundaki siparişler için kullanılabilir.
        /// 
        /// Örnek İstek:
        ///     POST /api/courier/orders/123/start-delivery
        ///     {
        ///         "currentLocation": "41.0082,28.9784",
        ///         "note": "Trafik yoğun, tahmini 30 dk"
        ///     }
        /// </remarks>
        /// <param name="id">Sipariş ID</param>
        /// <param name="dto">Yola çıkış bilgileri (opsiyonel konum ve not)</param>
        /// <returns>Başarı durumu ve yeni sipariş durumu</returns>
        [HttpPost("{id:int}/start-delivery")]
        [ProducesResponseType(typeof(CourierOrderActionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourierOrderActionResponseDto>> StartDelivery(
            [FromRoute] int id, 
            [FromBody] StartDeliveryDto? dto)
        {
            dto ??= new StartDeliveryDto();

            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            // Ownership ön kontrolü
            if (!await _courierOrderService.ValidateOrderOwnershipAsync(id, courierId.Value))
            {
                return NotFound(new { message = "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok." });
            }

            var result = await _courierOrderService.StartDeliveryAsync(id, courierId.Value, dto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Kurye siparişi teslim ettiğini bildirir (OUT_FOR_DELIVERY → DELIVERED).
        /// </summary>
        /// <remarks>
        /// Bu işlem sonrasında:
        /// - Kredi kartı ödemelerinde capture işlemi yapılır
        /// - Müşteriye "Siparişiniz teslim edildi" bildirimi gönderilir
        /// - DeliveredAt timestamp kaydedilir
        /// - Kurye istatistikleri güncellenir
        /// 
        /// Tartı farkı varsa:
        /// - WeightAdjustmentGrams ile bildirilir
        /// - Final tutar > Authorize tutar ise admin onayı beklenir (DELIVERY_PAYMENT_PENDING)
        /// 
        /// Kapıda nakit ödeme varsa:
        /// - CashCollected ve CollectedAmount ile belirtilir
        /// 
        /// Sadece OUT_FOR_DELIVERY durumundaki siparişler için kullanılabilir.
        /// 
        /// Örnek İstek:
        ///     POST /api/courier/orders/123/delivered
        ///     {
        ///         "receiverName": "Ahmet Bey",
        ///         "photoUrl": "https://storage/receipts/123.jpg",
        ///         "cashCollected": true,
        ///         "collectedAmount": 150.00,
        ///         "note": "Kapıda teslim edildi"
        ///     }
        /// </remarks>
        /// <param name="id">Sipariş ID</param>
        /// <param name="dto">Teslim bilgileri</param>
        /// <returns>Başarı durumu, yeni sipariş durumu ve ödeme bilgisi</returns>
        [HttpPost("{id:int}/delivered")]
        [ProducesResponseType(typeof(CourierOrderActionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourierOrderActionResponseDto>> MarkDelivered(
            [FromRoute] int id, 
            [FromBody] MarkDeliveredDto? dto)
        {
            dto ??= new MarkDeliveredDto();

            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            // Ownership ön kontrolü
            if (!await _courierOrderService.ValidateOrderOwnershipAsync(id, courierId.Value))
            {
                return NotFound(new { message = "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok." });
            }

            var result = await _courierOrderService.MarkDeliveredAsync(id, courierId.Value, dto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Kurye teslimat problemi bildirir (Any → DELIVERY_FAILED).
        /// </summary>
        /// <remarks>
        /// Bu işlem sonrasında:
        /// - Sipariş durumu "Teslimat Başarısız" olarak güncellenir
        /// - Admin'e problem bildirimi gönderilir
        /// - Müşteriye bilgilendirme yapılır
        /// 
        /// Sebep türleri (DeliveryProblemReason):
        /// - CustomerNotAvailable: Müşteri adreste değil
        /// - AddressNotFound: Adres bulunamadı
        /// - AccessDenied: Adrese erişim engeli
        /// - RefusedByCustomer: Müşteri teslim almak istemiyor
        /// - DamagedPackage: Paket hasarlı
        /// - PaymentIssue: Ödeme problemi
        /// - WeatherConditions: Hava koşulları
        /// - VehicleBreakdown: Araç arızası
        /// - CustomerUnreachable: Müşteriye ulaşılamıyor
        /// - RoadBlocked: Yol kapalı
        /// - Other: Diğer (açıklama zorunlu)
        /// 
        /// Terminal durumlar (DELIVERED, CANCELLED, REFUNDED) haricindeki 
        /// tüm durumlardan problem bildirilebilir.
        /// 
        /// Örnek İstek:
        ///     POST /api/courier/orders/123/problem
        ///     {
        ///         "reason": "CustomerNotAvailable",
        ///         "description": "3 kez zili çaldım, kimse açmadı",
        ///         "attemptedToContactCustomer": true,
        ///         "callAttempts": 2,
        ///         "currentLocation": "41.0082,28.9784",
        ///         "photoUrl": "https://storage/problems/123.jpg"
        ///     }
        /// </remarks>
        /// <param name="id">Sipariş ID</param>
        /// <param name="dto">Problem bilgileri</param>
        /// <returns>Başarı durumu ve yeni sipariş durumu</returns>
        [HttpPost("{id:int}/problem")]
        [ProducesResponseType(typeof(CourierOrderActionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourierOrderActionResponseDto>> ReportProblem(
            [FromRoute] int id, 
            [FromBody][Required] ReportProblemDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            // Ownership ön kontrolü
            if (!await _courierOrderService.ValidateOrderOwnershipAsync(id, courierId.Value))
            {
                return NotFound(new { message = "Sipariş bulunamadı veya bu siparişe erişim yetkiniz yok." });
            }

            var result = await _courierOrderService.ReportProblemAsync(id, courierId.Value, dto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        #endregion

        #region Özet ve İstatistikler

        /// <summary>
        /// Kurye'nin günlük istatistik özetini getirir.
        /// </summary>
        /// <remarks>
        /// Özet bilgiler:
        /// - Bugün teslim edilen sipariş sayısı
        /// - Aktif (yolda) sipariş sayısı
        /// - Bekleyen (atanmış) sipariş sayısı
        /// - Bugünkü başarısız teslimat sayısı
        /// - Bugünkü tahmini kazanç
        /// 
        /// Örnek İstek:
        ///     GET /api/courier/orders/summary
        /// </remarks>
        /// <returns>Günlük istatistik özeti</returns>
        [HttpGet("~/api/courier/summary")]
        [ProducesResponseType(typeof(CourierOrderSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CourierOrderSummaryDto>> GetDailySummary()
        {
            var courierId = await GetCurrentCourierIdAsync();
            if (courierId == null)
            {
                return Unauthorized(new { message = "Kurye hesabınız aktif değil veya yetkilendirilmemiş." });
            }

            var summary = await _courierOrderService.GetDailySummaryAsync(courierId.Value);
            return Ok(summary);
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Geçerli kullanıcının User ID'sini JWT'den alır.
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            return userId;
        }

        /// <summary>
        /// Geçerli kullanıcının Courier ID'sini bulur.
        /// </summary>
        private async Task<int?> GetCurrentCourierIdAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return null;
            }

            return await _courierOrderService.GetCourierIdByUserIdAsync(userId.Value);
        }

        #endregion
    }
}
