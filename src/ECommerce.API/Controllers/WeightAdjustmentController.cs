/*
 * WeightAdjustmentController - Ağırlık Bazlı Ödeme Yönetim API'si
 * 
 * Kurye İşlevleri:
 * - POST /api/weight-adjustment/orders/{orderId}/items/{orderItemId}/weigh → Ürün tartma
 * - POST /api/weight-adjustment/orders/{orderId}/finalize → Teslimat tamamlama ve ödeme hesaplama
 * - GET /api/weight-adjustment/orders/{orderId}/summary → Sipariş ağırlık özeti
 * 
 * Admin İşlevleri:
 * - GET /api/weight-adjustment/admin/pending → Onay bekleyen ayarlamalar
 * - POST /api/weight-adjustment/admin/{adjustmentId}/decision → Admin kararı
 * - GET /api/weight-adjustment/admin/statistics → İstatistikler
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/weight-adjustment")]
    [Authorize]
    public class WeightAdjustmentController : ControllerBase
    {
        private readonly IWeightAdjustmentService _weightAdjustmentService;
        private readonly IOrderService _orderService;
        private readonly ICourierService _courierService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<WeightAdjustmentController>? _logger;

        public WeightAdjustmentController(
            IWeightAdjustmentService weightAdjustmentService,
            IOrderService orderService,
            ICourierService courierService,
            UserManager<User> userManager,
            ILogger<WeightAdjustmentController>? logger = null)
        {
            _weightAdjustmentService = weightAdjustmentService;
            _orderService = orderService;
            _courierService = courierService;
            _userManager = userManager;
            _logger = logger;
        }

        #region Kurye İşlevleri

        /// <summary>
        /// Kurye - Ürün tartma işlemi
        /// POST /api/weight-adjustment/orders/{orderId}/items/{orderItemId}/weigh
        /// </summary>
        [HttpPost("orders/{orderId}/items/{orderItemId}/weigh")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> WeighOrderItem(
            int orderId, 
            int orderItemId, 
            [FromBody] CourierWeighItemRequest request)
        {
            try
            {
                // Kurye bilgisini al
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                var couriers = await _courierService.GetAllAsync();
                var courier = couriers.FirstOrDefault(c => c.UserId == userId);
                if (courier == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                // Ağırlık girişini kaydet (interface 4 parametre alıyor)
                var result = await _weightAdjustmentService.RecordCourierWeightEntryAsync(
                    orderId,
                    orderItemId,
                    request.ActualWeight,
                    courier.Id);

                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage });

                _logger?.LogInformation(
                    "[WEIGHT-API] Kurye ağırlık girişi: Order={OrderId}, Item={ItemId}, Weight={Weight}, Courier={CourierId}",
                    orderId, orderItemId, request.ActualWeight, courier.Id);

                return Ok(new { 
                    success = true, 
                    message = "Ağırlık başarıyla kaydedildi",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Ağırlık girişi hatası: Order={OrderId}, Item={ItemId}", orderId, orderItemId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye - Toplu ürün tartma
        /// POST /api/weight-adjustment/orders/{orderId}/weigh-bulk
        /// </summary>
        [HttpPost("orders/{orderId}/weigh-bulk")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> BulkWeighOrderItems(
            int orderId,
            [FromBody] BulkWeighRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                var couriers = await _courierService.GetAllAsync();
                var courier = couriers.FirstOrDefault(c => c.UserId == userId);
                if (courier == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                var entries = request.Items.Select(i => new WeightEntryRequestDto
                {
                    OrderItemId = i.OrderItemId,
                    ActualWeight = i.ActualWeight
                }).ToList();

                var results = await _weightAdjustmentService.RecordBulkWeightEntriesAsync(orderId, entries, courier.Id);

                _logger?.LogInformation(
                    "[WEIGHT-API] Toplu ağırlık girişi: Order={OrderId}, ItemCount={Count}, Courier={CourierId}",
                    orderId, entries.Count, courier.Id);

                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Toplu ağırlık girişi hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye - Teslimatı tamamla ve ödeme farkını hesapla
        /// POST /api/weight-adjustment/orders/{orderId}/finalize
        /// </summary>
        [HttpPost("orders/{orderId}/finalize")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> FinalizeDelivery(int orderId, [FromBody] FinalizeDeliveryRequest? request = null)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                var couriers = await _courierService.GetAllAsync();
                var courier = couriers.FirstOrDefault(c => c.UserId == userId);
                if (courier == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                // Teslimatı tamamla (FinalizeWeightBasedPaymentAsync kullan)
                var result = await _weightAdjustmentService.FinalizeWeightBasedPaymentAsync(
                    orderId,
                    courier.Id,
                    request?.CourierNotes);

                if (!result.IsSuccess)
                    return BadRequest(new { success = false, message = result.ErrorMessage, data = result });

                _logger?.LogInformation(
                    "[WEIGHT-API] Teslimat tamamlandı: Order={OrderId}, TotalDifference={Diff}, Courier={CourierId}",
                    orderId, result.DifferenceAmount, courier.Id);

                return Ok(new {
                    success = true,
                    message = "Teslimat başarıyla tamamlandı",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Teslimat tamamlama hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye - Sipariş ağırlık özeti
        /// GET /api/weight-adjustment/orders/{orderId}/summary
        /// </summary>
        [HttpGet("orders/{orderId}/summary")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> GetOrderWeightSummary(int orderId)
        {
            try
            {
                var summary = await _weightAdjustmentService.GetOrderWeightSummaryAsync(orderId);
                if (summary == null)
                    return NotFound(new { success = false, message = "Sipariş bulunamadı veya ağırlık bazlı ürün yok" });

                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Sipariş özeti hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye - Fark hesaplama önizleme
        /// GET /api/weight-adjustment/orders/{orderId}/calculate
        /// </summary>
        [HttpGet("orders/{orderId}/calculate")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> CalculateOrderDifference(int orderId)
        {
            try
            {
                var calculation = await _weightAdjustmentService.CalculateOrderWeightDifferenceAsync(orderId);
                return Ok(new { success = true, data = calculation });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Fark hesaplama hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye - Bekleyen tartımları getir
        /// GET /api/weight-adjustment/courier/pending
        /// </summary>
        [HttpGet("courier/pending")]
        [Authorize(Roles = "Courier")]
        public async Task<IActionResult> GetPendingWeightEntries()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                var couriers = await _courierService.GetAllAsync();
                var courier = couriers.FirstOrDefault(c => c.UserId == userId);
                if (courier == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                var pendingOrders = await _weightAdjustmentService.GetPendingWeightEntriesForCourierAsync(courier.Id);
                return Ok(new { success = true, data = pendingOrders });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Bekleyen tartımlar hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Nakit fark tahsilatı kaydı
        /// POST /api/weight-adjustment/orders/{orderId}/cash-settlement
        /// </summary>
        [HttpPost("orders/{orderId}/cash-settlement")]
        [Authorize(Roles = "Courier")]
        public async Task<IActionResult> RecordCashSettlement(int orderId, [FromBody] CashSettlementRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                var couriers = await _courierService.GetAllAsync();
                var courier = couriers.FirstOrDefault(c => c.UserId == userId);
                if (courier == null)
                    return Unauthorized(new { success = false, message = "Kurye kaydı bulunamadı" });

                var success = await _weightAdjustmentService.RecordCashDifferenceSettlementAsync(
                    orderId, 
                    courier.Id, 
                    request.CollectedAmount, 
                    request.Notes);

                if (!success)
                    return BadRequest(new { success = false, message = "Nakit tahsilat kaydedilemedi" });

                _logger?.LogInformation(
                    "[WEIGHT-API] Nakit tahsilat: Order={OrderId}, Amount={Amount}, Courier={CourierId}",
                    orderId, request.CollectedAmount, courier.Id);

                return Ok(new { success = true, message = "Nakit tahsilat kaydedildi" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Nakit tahsilat hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        #endregion

        #region Admin İşlevleri

        /// <summary>
        /// Admin - Onay bekleyen ağırlık ayarlamaları
        /// GET /api/weight-adjustment/admin/pending
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _weightAdjustmentService.GetPendingAdminReviewsAsync(page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Admin onay listesi hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin - Ağırlık ayarlaması kararı
        /// POST /api/weight-adjustment/admin/{adjustmentId}/decision
        /// </summary>
        [HttpPost("admin/{adjustmentId}/decision")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessAdminDecision(int adjustmentId, [FromBody] AdminDecisionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Kullanıcı kimliği bulunamadı" });

                // User ID'yi int'e çevir
                if (!int.TryParse(userId, out int adminId))
                    return BadRequest(new { success = false, message = "Geçersiz admin ID" });

                var success = await _weightAdjustmentService.ProcessAdminDecisionAsync(
                    adjustmentId,
                    adminId,
                    request.Decision,
                    request.OverrideAmount,
                    request.Notes);

                if (!success)
                    return BadRequest(new { success = false, message = "Karar işlenemedi" });

                _logger?.LogInformation(
                    "[WEIGHT-API] Admin kararı: Adjustment={AdjustmentId}, Decision={Decision}, Admin={AdminId}",
                    adjustmentId, request.Decision, adminId);

                return Ok(new { success = true, message = "Karar kaydedildi" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Admin karar hatası: Adjustment={AdjustmentId}", adjustmentId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin - Ağırlık ayarlama istatistikleri
        /// GET /api/weight-adjustment/admin/statistics
        /// </summary>
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var stats = await _weightAdjustmentService.GetStatisticsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] İstatistik hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin - Filtrelenmiş ağırlık ayarlamaları listesi
        /// GET /api/weight-adjustment/admin/list
        /// </summary>
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFilteredAdjustments([FromQuery] WeightAdjustmentFilterDto filter)
        {
            try
            {
                var result = await _weightAdjustmentService.GetFilteredAdjustmentsAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Admin liste hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin - Ağırlık ayarlaması detayı
        /// GET /api/weight-adjustment/admin/{adjustmentId}
        /// </summary>
        [HttpGet("admin/{adjustmentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdjustmentDetail(int adjustmentId)
        {
            try
            {
                var detail = await _weightAdjustmentService.GetWeightAdjustmentByIdAsync(adjustmentId);
                if (detail == null)
                    return NotFound(new { success = false, message = "Ağırlık ayarlaması bulunamadı" });

                return Ok(new { success = true, data = detail });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Admin detay hatası: Adjustment={AdjustmentId}", adjustmentId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin - Siparişi admin onayına gönder
        /// POST /api/weight-adjustment/admin/request-review/{orderId}
        /// </summary>
        [HttpPost("admin/request-review/{orderId}")]
        [Authorize(Roles = "Admin,Courier")]
        public async Task<IActionResult> RequestAdminReview(int orderId, [FromBody] RequestReviewRequest request)
        {
            try
            {
                var success = await _weightAdjustmentService.RequestAdminReviewAsync(orderId, request.Reason);
                if (!success)
                    return BadRequest(new { success = false, message = "Admin onayı istenemedi" });

                return Ok(new { success = true, message = "Admin onayı talep edildi" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Admin onay talebi hatası: Order={OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        /// <summary>
        /// Kurye performans raporu
        /// GET /api/weight-adjustment/admin/courier/{courierId}/performance
        /// </summary>
        [HttpGet("admin/courier/{courierId}/performance")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCourierPerformance(
            int courierId, 
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var performance = await _weightAdjustmentService.GetCourierPerformanceAsync(courierId, startDate, endDate);
                return Ok(new { success = true, data = performance });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Kurye performans hatası: Courier={CourierId}", courierId);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        #endregion

        #region Validasyon

        /// <summary>
        /// Ağırlık değeri validasyonu
        /// POST /api/weight-adjustment/validate-weight
        /// </summary>
        [HttpPost("validate-weight")]
        [Authorize(Roles = "Courier,Admin")]
        public async Task<IActionResult> ValidateWeight([FromBody] ValidateWeightRequest request)
        {
            try
            {
                var result = await _weightAdjustmentService.ValidateWeightEntryAsync(request.OrderItemId, request.Weight);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-API] Validasyon hatası");
                return StatusCode(500, new { success = false, message = "Bir hata oluştu", error = ex.Message });
            }
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Kurye ağırlık girişi request
    /// </summary>
    public class CourierWeighItemRequest
    {
        /// <summary>
        /// Gerçek ağırlık (ürünün WeightUnit'ına göre)
        /// </summary>
        public decimal ActualWeight { get; set; }

        /// <summary>
        /// Kurye notu (opsiyonel)
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Toplu tartım request
    /// </summary>
    public class BulkWeighRequest
    {
        public List<BulkWeighItem> Items { get; set; } = new();
    }

    public class BulkWeighItem
    {
        public int OrderItemId { get; set; }
        public decimal ActualWeight { get; set; }
    }

    /// <summary>
    /// Teslimat tamamlama request
    /// </summary>
    public class FinalizeDeliveryRequest
    {
        /// <summary>
        /// Kurye notu (opsiyonel)
        /// </summary>
        public string? CourierNotes { get; set; }
    }

    /// <summary>
    /// Nakit tahsilat request
    /// </summary>
    public class CashSettlementRequest
    {
        public decimal CollectedAmount { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Admin karar request
    /// </summary>
    public class AdminDecisionRequest
    {
        /// <summary>
        /// Karar tipi
        /// </summary>
        public AdminDecisionType Decision { get; set; }

        /// <summary>
        /// Admin notu
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Manuel tutar (Override durumunda)
        /// </summary>
        public decimal? OverrideAmount { get; set; }
    }

    /// <summary>
    /// Admin onay talebi
    /// </summary>
    public class RequestReviewRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ağırlık validasyon request
    /// </summary>
    public class ValidateWeightRequest
    {
        public int OrderItemId { get; set; }
        public decimal Weight { get; set; }
    }

    #endregion
}
