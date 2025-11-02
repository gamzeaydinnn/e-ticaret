using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class WeightReportsController : ControllerBase
    {
        private readonly IWeightReportRepository _weightReportRepository;
        private readonly IWeightService _weightService;
        private readonly ILogger<WeightReportsController> _logger;

        public WeightReportsController(
            IWeightReportRepository weightReportRepository,
            IWeightService weightService,
            ILogger<WeightReportsController> logger)
        {
            _weightReportRepository = weightReportRepository;
            _weightService = weightService;
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

                // Tüm durumlar için - sadece bekleyen raporları göster
                var pendingReports = await _weightReportRepository.GetPendingReportsAsync();
                return Ok(new
                {
                    data = pendingReports.Select(MapToDto),
                    pagination = new
                    {
                        page = 1,
                        pageSize = pendingReports.Count(),
                        totalCount = pendingReports.Count(),
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
    }
}
