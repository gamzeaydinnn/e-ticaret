using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class WeightReportsController : ControllerBase
    {
        private readonly IWeightReportRepository _weightReportRepository;
        private readonly IWeightService _weightService;
        private readonly ECommerceDbContext _dbContext;
        private readonly ILogger<WeightReportsController> _logger;

        public WeightReportsController(
            IWeightReportRepository weightReportRepository,
            IWeightService weightService,
            ECommerceDbContext dbContext,
            ILogger<WeightReportsController> logger)
        {
            _weightReportRepository = weightReportRepository;
            _weightService = weightService;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Duruma göre raporları listele (sayfalanmış)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (status != null && Enum.TryParse<WeightReportStatus>(status, true, out var statusEnum))
                {
                    var (reports, totalCount) = await _weightReportRepository.GetByStatusAsync(statusEnum, page, pageSize);
                    
                    return Ok(new
                    {
                        data = reports.Select(MapToDto),
                        pagination = new
                        {
                            page,
                            pageSize,
                            totalCount,
                            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                        }
                    });
                }

                // Tüm durumlar için - bekleyen gerçek raporlar + henüz tartı raporu oluşmamış
                // tartılı siparişler. NEDEN: Manuel tartı bu ekrandan yapılacaksa, ilk rapor
                // kaydı henüz oluşmamış sipariş de listede görünmelidir.
                var pendingReports = (await _weightReportRepository.GetPendingReportsAsync()).ToList();
                var manualWeighingOrders = await GetManualWeighingOrderDtosAsync(page, pageSize);
                var data = pendingReports.Select(MapToDto).Concat(manualWeighingOrders).ToList();

                return Ok(new
                {
                    data,
                    pagination = new
                    {
                        page = 1,
                        pageSize = data.Count,
                        totalCount = data.Count,
                        totalPages = 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raporlar listelenirken hata");
                return StatusCode(500, new { error = "Raporlar listelenemedi" });
            }
        }

        /// <summary>
        /// Rapor detayı getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(int id)
        {
            try
            {
                var report = await _weightReportRepository.GetByIdAsync(id);
                if (report == null)
                {
                    return NotFound(new { error = "Rapor bulunamadı" });
                }

                return Ok(MapToDto(report));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor detayı alınırken hata: {id}");
                return StatusCode(500, new { error = "Rapor detayı alınamadı" });
            }
        }

        /// <summary>
        /// Raporu onayla
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReport(int id, [FromBody] WeightReportActionDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var success = await _weightService.ApproveReportAsync(id, userId, dto.Note);

                if (!success)
                {
                    return BadRequest(new { error = "Rapor onaylanamadı" });
                }

                _logger.LogInformation($"Rapor onaylandı: {id}, Yönetici: {userId}");
                return Ok(new { message = "Rapor başarıyla onaylandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor onaylanırken hata: {id}");
                return StatusCode(500, new { error = "Rapor onaylanamadı" });
            }
        }

        /// <summary>
        /// Raporu reddet
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectReport(int id, [FromBody] WeightReportActionDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var success = await _weightService.RejectReportAsync(id, userId, dto.Note);

                if (!success)
                {
                    return BadRequest(new { error = "Rapor reddedilemedi" });
                }

                _logger.LogInformation($"Rapor reddedildi: {id}, Yönetici: {userId}");
                return Ok(new { message = "Rapor başarıyla reddedildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor reddedilirken hata: {id}");
                return StatusCode(500, new { error = "Rapor reddedilemedi" });
            }
        }

        /// <summary>
        /// Rapor istatistikleri
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _weightReportRepository.GetStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler alınırken hata");
                return StatusCode(500, new { error = "İstatistikler alınamadı" });
            }
        }

        private WeightReportResponseDto MapToDto(WeightReport report)
        {
            return new WeightReportResponseDto
            {
                Id = report.Id,
                ExternalReportId = report.ExternalReportId,
                OrderId = report.OrderId,
                OrderNumber = report.Order?.OrderNumber ?? "",
                ExpectedWeightGrams = report.ExpectedWeightGrams,
                ReportedWeightGrams = report.ReportedWeightGrams,
                OverageGrams = report.OverageGrams,
                OverageAmount = report.OverageAmount,
                Currency = report.Currency,
                Status = report.Status.ToString(),
                Source = report.Source,
                ReceivedAt = report.ReceivedAt,
                ProcessedAt = report.ProcessedAt,
                AdminNote = report.AdminNote,
                CourierNote = report.CourierNote
            };
        }

        private async Task<List<WeightReportResponseDto>> GetManualWeighingOrderDtosAsync(int page, int pageSize)
        {
            var activeStatuses = new[]
            {
                OrderStatus.Pending,
                OrderStatus.New,
                OrderStatus.Paid,
                OrderStatus.Confirmed,
                OrderStatus.Preparing
            };

            var orders = await _dbContext.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.HasWeightBasedItems &&
                    activeStatuses.Contains(o.Status) &&
                    o.PaymentStatus == PaymentStatus.Paid)
                .OrderByDescending(o => o.OrderDate)
                .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
                .Take(Math.Clamp(pageSize, 1, 100))
                .ToListAsync();

            var result = new List<WeightReportResponseDto>();
            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems.Where(IsPendingWeightItem))
                {
                    var hasReport = await _dbContext.WeightReports
                        .AsNoTracking()
                        .AnyAsync(r => r.OrderItemId == item.Id);

                    if (hasReport)
                    {
                        continue;
                    }

                    var expectedWeight = item.EstimatedWeight > 0
                        ? item.EstimatedWeight
                        : item.Quantity * 1000m;

                    result.Add(new WeightReportResponseDto
                    {
                        Id = -item.Id,
                        ExternalReportId = $"manual-pending-{order.Id}-{item.Id}",
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber ?? $"#{order.Id}",
                        ExpectedWeightGrams = (int)Math.Round(expectedWeight, MidpointRounding.AwayFromZero),
                        ReportedWeightGrams = 0,
                        OverageGrams = 0,
                        OverageAmount = 0,
                        Currency = order.Currency ?? "TRY",
                        Status = "PendingWeighing",
                        Source = "ManualWeighing",
                        ReceivedAt = order.OrderDate,
                        ProcessedAt = null,
                        AdminNote = $"{item.Product?.Name ?? "Tartılı ürün"} manuel tartı bekliyor",
                        CourierNote = null
                    });
                }
            }

            return result;
        }

        private static bool IsPendingWeightItem(OrderItem item)
        {
            return item.IsWeightBased &&
                item.EstimatedWeight > 0 &&
                (!item.ActualWeight.HasValue || item.ActualWeight.Value <= 0);
        }
    }
}
