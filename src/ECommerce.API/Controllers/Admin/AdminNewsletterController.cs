using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ECommerce.Core.Constants;
using ECommerce.Business.Services.Interfaces;
using ECommerce.API.DTOs.Newsletter;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Newsletter (Bülten) Admin API Controller.
    /// 
    /// AMAÇ:
    /// - Abone listesi yönetimi (listeleme, filtreleme, silme)
    /// - Toplu mail gönderimi
    /// - Newsletter istatistikleri
    /// 
    /// YETKİLENDİRME:
    /// - Sadece Admin rolündeki kullanıcılar erişebilir
    /// - [Authorize(Roles = Roles.AdminLike)] attribute'u ile korunur
    /// 
    /// ENDPOINT'LER:
    /// - GET    /api/admin/newsletter              - Abone listesi (sayfalı)
    /// - GET    /api/admin/newsletter/stats        - İstatistikler
    /// - GET    /api/admin/newsletter/{id}         - Tek abone detayı
    /// - DELETE /api/admin/newsletter/{id}         - Abone silme (GDPR)
    /// - POST   /api/admin/newsletter/send         - Toplu mail gönderimi
    /// - POST   /api/admin/newsletter/send-test    - Test mail gönderimi
    /// </summary>
    [Authorize(Roles = Roles.AdminLike)]
    [ApiController]
    [Route("api/admin/newsletter")]
    public class AdminNewsletterController : ControllerBase
    {
        private readonly INewsletterService _newsletterService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AdminNewsletterController> _logger;
        private readonly IConfiguration _configuration;

        public AdminNewsletterController(
            INewsletterService newsletterService,
            IAuditLogService auditLogService,
            ILogger<AdminNewsletterController> logger,
            IConfiguration configuration)
        {
            _newsletterService = newsletterService ?? throw new ArgumentNullException(nameof(newsletterService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // LİSTELEME ENDPOINT'LERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tüm newsletter abonelerini sayfalı olarak getirir.
        /// Filtreleme ve sıralama destekler.
        /// 
        /// QUERY PARAMETRELERİ:
        /// - page: Sayfa numarası (varsayılan: 1)
        /// - pageSize: Sayfa başına kayıt (varsayılan: 20, max: 100)
        /// - search: Email veya isim araması
        /// - isActive: Aktiflik filtresi (true/false/null)
        /// - source: Kaynak filtresi (web_footer, popup, vb.)
        /// - sortBy: Sıralama alanı (SubscribedAt, Email, FullName, vb.)
        /// - sortDescending: Azalan sıralama (varsayılan: true)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedNewsletterResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSubscribers([FromQuery] NewsletterQueryDto query)
        {
            try
            {
                var result = await _newsletterService.GetAllSubscribersAsync(
                    query.Page,
                    query.PageSize,
                    query.Search,
                    query.IsActive,
                    query.Source,
                    query.SortBy,
                    query.SortDescending);

                // Entity'leri DTO'lara dönüştür (hassas bilgileri filtrele)
                var response = new PagedNewsletterResponseDto
                {
                    Items = result.Items.Select(s => new NewsletterSubscriberDto
                    {
                        Id = s.Id,
                        Email = s.Email,
                        FullName = s.FullName,
                        Source = s.Source,
                        IsActive = s.IsActive,
                        IsConfirmed = s.IsConfirmed,
                        SubscribedAt = s.SubscribedAt,
                        UnsubscribedAt = s.UnsubscribedAt,
                        LastEmailSentAt = s.LastEmailSentAt,
                        EmailsSentCount = s.EmailsSentCount,
                        CreatedAt = s.CreatedAt,
                        UserId = s.UserId
                    }).ToList(),
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalPages = result.TotalPages,
                    HasNextPage = result.HasNextPage,
                    HasPreviousPage = result.HasPreviousPage
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching newsletter subscribers");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Abone listesi alınırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Newsletter istatistiklerini getirir.
        /// Dashboard için özet bilgiler.
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(NewsletterStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _newsletterService.GetStatisticsAsync();

                return Ok(new NewsletterStatsDto
                {
                    TotalSubscribers = stats.TotalSubscribers,
                    ActiveSubscribers = stats.ActiveSubscribers,
                    UnsubscribedCount = stats.UnsubscribedCount,
                    NewSubscribersLast7Days = stats.NewSubscribersLast7Days,
                    NewSubscribersLast30Days = stats.NewSubscribersLast30Days,
                    TotalEmailsSent = stats.TotalEmailsSent,
                    SubscribersBySource = stats.SubscribersBySource
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching newsletter statistics");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "İstatistikler alınırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Tek bir aboneyi ID ile getirir.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(NewsletterSubscriberDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubscriberById(int id)
        {
            try
            {
                var subscriber = await _newsletterService.GetSubscriberByIdAsync(id);

                if (subscriber == null)
                {
                    return NotFound(new { message = "Abone bulunamadı." });
                }

                return Ok(new NewsletterSubscriberDto
                {
                    Id = subscriber.Id,
                    Email = subscriber.Email,
                    FullName = subscriber.FullName,
                    Source = subscriber.Source,
                    IsActive = subscriber.IsActive,
                    IsConfirmed = subscriber.IsConfirmed,
                    SubscribedAt = subscriber.SubscribedAt,
                    UnsubscribedAt = subscriber.UnsubscribedAt,
                    LastEmailSentAt = subscriber.LastEmailSentAt,
                    EmailsSentCount = subscriber.EmailsSentCount,
                    CreatedAt = subscriber.CreatedAt,
                    UserId = subscriber.UserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscriber by ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Abone bilgisi alınırken bir hata oluştu." });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // SİLME ENDPOINT'İ
        // GDPR "Unutulma Hakkı" için hard delete
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Abone kaydını kalıcı olarak siler.
        /// GDPR "Unutulma Hakkı" talebi için kullanılır.
        /// 
        /// DİKKAT: Bu işlem geri alınamaz!
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSubscriber(int id)
        {
            try
            {
                // Silinmeden önce bilgilerini al (audit log için)
                var subscriber = await _newsletterService.GetSubscriberByIdAsync(id);
                
                if (subscriber == null)
                {
                    return NotFound(new { message = "Abone bulunamadı." });
                }

                var success = await _newsletterService.DeleteSubscriberAsync(id);

                if (success)
                {
                    // Audit log yaz
                    await _auditLogService.WriteAsync(
                        GetAdminUserId(),
                        "NewsletterSubscriberDeleted",
                        "NewsletterSubscriber",
                        id.ToString(),
                        new { subscriber.Email, subscriber.FullName, subscriber.Source },
                        null);

                    _logger.LogInformation(
                        "Newsletter subscriber deleted by admin: ID={Id}, Email={Email}, AdminUserId={AdminId}",
                        id, subscriber.Email, GetAdminUserId());

                    return Ok(new { message = "Abone başarıyla silindi." });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Abone silinirken bir hata oluştu." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscriber: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Abone silinirken bir hata oluştu." });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TOPLU MAİL GÖNDERİM ENDPOINT'LERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Toplu mail gönderim işlemini başlatır.
        /// Tüm aktif abonelere veya filtrelenmiş listeye mail gönderir.
        /// 
        /// ÇALIŞMA PRENSİBİ:
        /// - Mailler MailQueue'ya eklenir
        /// - MessageWorker background'da asenkron işler
        /// - Her mail için unsubscribe linki otomatik eklenir
        /// 
        /// FİLTRELER:
        /// - sourceFilter: Belirli kaynaklardan gelen aboneler
        /// - subscribedAfter: Minimum abonelik tarihi
        /// - subscribedBefore: Maksimum abonelik tarihi
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(SendBulkEmailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SendBulkEmailResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendBulkEmail([FromBody] SendBulkEmailRequestDto request)
        {
            try
            {
                // ══════════════════════════════════════════════════════════════════════
                // VALİDASYON
                // ══════════════════════════════════════════════════════════════════════

                if (!ModelState.IsValid)
                {
                    return BadRequest(new SendBulkEmailResponseDto
                    {
                        Success = false,
                        Message = "Geçersiz veri. Konu ve içerik zorunludur."
                    });
                }

                // Test modu kontrolü
                if (request.IsTestMode)
                {
                    return await SendTestEmailInternal(request);
                }

                // ══════════════════════════════════════════════════════════════════════
                // TOPLU MAİL GÖNDERİMİ
                // ══════════════════════════════════════════════════════════════════════

                var result = await _newsletterService.SendBulkEmailAsync(
                    request.Subject,
                    request.Body,
                    request.IsHtml,
                    request.SourceFilter,
                    request.SubscribedAfter,
                    request.SubscribedBefore);

                if (result.Success)
                {
                    // Audit log yaz
                    await _auditLogService.WriteAsync(
                        GetAdminUserId(),
                        "NewsletterBulkEmailSent",
                        "Newsletter",
                        result.BatchId ?? "unknown",
                        null,
                        new
                        {
                            Subject = request.Subject,
                            TotalSubscribers = result.TotalSubscribers,
                            QueuedCount = result.QueuedCount,
                            SourceFilter = request.SourceFilter,
                            SubscribedAfter = request.SubscribedAfter,
                            SubscribedBefore = request.SubscribedBefore
                        });

                    _logger.LogInformation(
                        "Newsletter bulk email sent: BatchId={BatchId}, Total={Total}, Queued={Queued}, Admin={AdminId}",
                        result.BatchId, result.TotalSubscribers, result.QueuedCount, GetAdminUserId());

                    return Ok(new SendBulkEmailResponseDto
                    {
                        Success = true,
                        Message = result.Message,
                        TotalSubscribers = result.TotalSubscribers,
                        QueuedCount = result.QueuedCount,
                        BatchId = result.BatchId
                    });
                }

                return BadRequest(new SendBulkEmailResponseDto
                {
                    Success = false,
                    Message = result.Message,
                    FailedCount = result.FailedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk email send");
                return StatusCode(StatusCodes.Status500InternalServerError, new SendBulkEmailResponseDto
                {
                    Success = false,
                    Message = "Toplu mail gönderimi sırasında bir hata oluştu."
                });
            }
        }

        /// <summary>
        /// Test mail gönderimi endpoint'i.
        /// Belirli email adreslerine önizleme maili gönderir.
        /// Gerçek gönderimden önce kontrol için kullanılır.
        /// </summary>
        [HttpPost("send-test")]
        [ProducesResponseType(typeof(SendBulkEmailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SendBulkEmailResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendTestEmail([FromBody] SendBulkEmailRequestDto request)
        {
            return await SendTestEmailInternal(request);
        }

        /// <summary>
        /// Test mail gönderimi - internal method.
        /// </summary>
        private async Task<IActionResult> SendTestEmailInternal(SendBulkEmailRequestDto request)
        {
            try
            {
                if (request.TestEmails == null || !request.TestEmails.Any())
                {
                    return BadRequest(new SendBulkEmailResponseDto
                    {
                        Success = false,
                        Message = "Test için en az bir e-posta adresi gereklidir."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                {
                    return BadRequest(new SendBulkEmailResponseDto
                    {
                        Success = false,
                        Message = "Konu ve içerik zorunludur."
                    });
                }

                var result = await _newsletterService.SendTestEmailAsync(
                    request.Subject,
                    request.Body,
                    request.TestEmails,
                    request.IsHtml);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Newsletter test email sent: Count={Count}, Admin={AdminId}",
                        result.QueuedCount, GetAdminUserId());

                    return Ok(new SendBulkEmailResponseDto
                    {
                        Success = true,
                        Message = $"Test maili {result.QueuedCount} adrese gönderildi.",
                        TotalSubscribers = request.TestEmails.Count,
                        QueuedCount = result.QueuedCount
                    });
                }

                return BadRequest(new SendBulkEmailResponseDto
                {
                    Success = false,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during test email send");
                return StatusCode(StatusCodes.Status500InternalServerError, new SendBulkEmailResponseDto
                {
                    Success = false,
                    Message = "Test maili gönderilirken bir hata oluştu."
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Giriş yapmış admin kullanıcısının ID'sini alır.
        /// Audit log için kullanılır.
        /// </summary>
        private int GetAdminUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
