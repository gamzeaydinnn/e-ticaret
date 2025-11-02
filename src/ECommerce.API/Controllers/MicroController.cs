//MikroController (senkron endpoint'leri).
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// ... using'ler
namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MicroController : ControllerBase
    {
        private readonly IMicroService _microService;
        private readonly IWeightService _weightService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MicroController> _logger;

        public MicroController(
            IMicroService microService,
            IWeightService weightService,
            IConfiguration configuration,
            ILogger<MicroController> logger)
        {
            _microService = microService;
            _weightService = weightService;
            _configuration = configuration;
            _logger = logger;
        }

        /* Kaldırıldı: SyncProducts() 
        Kaldırıldı: ExportOrders() 
        */

        /// <summary>
        /// Mikro ERP’den ürünleri getir (Gerekirse burada kalır)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _microService.GetProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP’den stokları getir (Gerekirse burada kalır)
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks()
        {
            var stocks = await _microService.GetStocksAsync();
            return Ok(stocks);
        }

        /// <summary>
        /// Tartı cihazından ağırlık raporu al (Webhook)
        /// POST /api/micro/weight
        /// Header: X-Micro-Signature (HMAC-SHA256)
        /// </summary>
        [HttpPost("weight")]
        public async Task<IActionResult> ReceiveWeightReport([FromBody] MicroWeightReportDto dto)
        {
            try
            {
                // HMAC imza doğrulama
                var signature = Request.Headers["X-Micro-Signature"].ToString();
                if (!ValidateSignature(dto, signature))
                {
                    _logger.LogWarning("Geçersiz HMAC imzası");
                    return Unauthorized(new { error = "Geçersiz imza" });
                }

                // Zaman damgası doğrulama (replay attack önleme - 5 dakika tolerans)
                if (Math.Abs((DateTimeOffset.UtcNow - dto.Timestamp).TotalMinutes) > 5)
                {
                    _logger.LogWarning($"Geçersiz zaman damgası: {dto.Timestamp}");
                    return BadRequest(new { error = "Geçersiz zaman damgası" });
                }

                // Raporu işle
                var report = await _weightService.ProcessReportAsync(dto);

                _logger.LogInformation($"Ağırlık raporu alındı: {report.ExternalReportId}");

                return Accepted(new
                {
                    reportId = report.Id,
                    externalReportId = report.ExternalReportId,
                    status = report.Status.ToString(),
                    message = "Rapor başarıyla alındı ve işleniyor"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Geçersiz rapor verisi");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ağırlık raporu işlenirken hata");
                return StatusCode(500, new { error = "Rapor işlenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// HMAC-SHA256 imza doğrulama
        /// </summary>
        private bool ValidateSignature(MicroWeightReportDto dto, string signature)
        {
            try
            {
                var secret = _configuration["Micro:SharedSecret"];
                if (string.IsNullOrEmpty(secret))
                {
                    _logger.LogWarning("Micro:SharedSecret yapılandırılmamış (DEVELOPMENT ONLY!)");
                    return true; // Development için
                }

                if (string.IsNullOrEmpty(signature)) return false;

                var payload = System.Text.Json.JsonSerializer.Serialize(dto);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);
                var secretBytes = Encoding.UTF8.GetBytes(secret);

                using var hmac = new HMACSHA256(secretBytes);
                var hashBytes = hmac.ComputeHash(payloadBytes);
                var computedSignature = Convert.ToBase64String(hashBytes);

                return signature == computedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İmza doğrulama hatası");
                return false;
            }
        }
    }
}