using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Messaging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Newsletter (Bülten) servisi implementasyonu.
    /// 
    /// SORUMLULUKLAR:
    /// - Newsletter abonelik yönetimi (subscribe/unsubscribe)
    /// - Abone listesi CRUD operasyonları
    /// - Toplu mail gönderimi (MailQueue üzerinden)
    /// 
    /// MİMARİ:
    /// - INewsletterService interface'ini implemente eder
    /// - ECommerceDbContext ile veritabanı erişimi
    /// - MailQueue ile asenkron mail gönderimi
    /// - ILogger ile kurumsal loglama
    /// 
    /// PERFORMANS OPTİMİZASYONLARI:
    /// - AsNoTracking() read-only sorgularda
    /// - Sayfalama ile büyük veri setleri
    /// - Index'li alanlar üzerinden sorgulama
    /// </summary>
    public class NewsletterManager : INewsletterService
    {
        private readonly ECommerceDbContext _context;
        private readonly MailQueue _mailQueue;
        private readonly ILogger<NewsletterManager> _logger;

        // Email sanitization için regex pattern - basit HTML tag temizliği
        private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);
        
        // Email validation regex - RFC 5322 standardına yakın
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public NewsletterManager(
            ECommerceDbContext context,
            MailQueue mailQueue,
            ILogger<NewsletterManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mailQueue = mailQueue ?? throw new ArgumentNullException(nameof(mailQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Abonelik İşlemleri

        /// <inheritdoc />
        public async Task<NewsletterSubscribeResult> SubscribeAsync(
            string email,
            string? fullName = null,
            string? source = null,
            string? ipAddress = null,
            int? userId = null)
        {
            try
            {
                // ══════════════════════════════════════════════════════════════════════
                // VALIDASYON
                // Email formatı ve güvenlik kontrolleri
                // ══════════════════════════════════════════════════════════════════════

                if (string.IsNullOrWhiteSpace(email))
                {
                    return NewsletterSubscribeResult.Failed("E-posta adresi gereklidir.");
                }

                // Email'i normalize et (lowercase, trim)
                email = NormalizeEmail(email);

                // Email format validasyonu
                if (!IsValidEmail(email))
                {
                    return NewsletterSubscribeResult.Failed("Geçerli bir e-posta adresi giriniz.");
                }

                // FullName sanitization - XSS önleme
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = SanitizeInput(fullName);
                    if (fullName.Length > 100)
                    {
                        fullName = fullName.Substring(0, 100);
                    }
                }

                // Source normalizasyonu
                source = string.IsNullOrWhiteSpace(source) ? "web_footer" : source.ToLowerInvariant().Trim();
                if (source.Length > 50)
                {
                    source = source.Substring(0, 50);
                }

                // ══════════════════════════════════════════════════════════════════════
                // MEVCUT ABONE KONTROLÜ
                // Email benzersiz olmalı - varsa resubscribe senaryosu
                // ══════════════════════════════════════════════════════════════════════

                var existingSubscriber = await _context.NewsletterSubscribers
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (existingSubscriber != null)
                {
                    // Zaten aktif abone
                    if (existingSubscriber.IsActive)
                    {
                        _logger.LogInformation(
                            "Newsletter resubscribe attempt for already active email: {Email}", 
                            email);
                        
                        return NewsletterSubscribeResult.Succeeded(
                            existingSubscriber.Id,
                            "Bu e-posta adresi zaten bültenimize kayıtlı.",
                            wasAlreadySubscribed: true);
                    }

                    // Daha önce iptal etmiş - yeniden aktif et (resubscribe)
                    existingSubscriber.IsActive = true;
                    existingSubscriber.UnsubscribedAt = null;
                    existingSubscriber.SubscribedAt = DateTime.UtcNow;
                    existingSubscriber.UpdatedAt = DateTime.UtcNow;
                    existingSubscriber.Source = source; // Yeni kaynağı kaydet
                    existingSubscriber.IpAddress = ipAddress;
                    
                    // Yeni unsubscribe token oluştur (güvenlik için)
                    existingSubscriber.UnsubscribeToken = Guid.NewGuid().ToString("N");

                    // Kullanıcı bağlantısını güncelle (varsa)
                    if (userId.HasValue)
                    {
                        existingSubscriber.UserId = userId;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Newsletter resubscribe successful for email: {Email}, Source: {Source}", 
                        email, source);

                    return NewsletterSubscribeResult.Succeeded(
                        existingSubscriber.Id,
                        "Bültene tekrar abone oldunuz. Hoş geldiniz!",
                        wasAlreadySubscribed: true);
                }

                // ══════════════════════════════════════════════════════════════════════
                // YENİ ABONELİK
                // Yeni kayıt oluştur ve kaydet
                // ══════════════════════════════════════════════════════════════════════

                var newSubscriber = new NewsletterSubscriber
                {
                    Email = email,
                    FullName = fullName,
                    Source = source,
                    IpAddress = ipAddress,
                    UserId = userId,
                    SubscribedAt = DateTime.UtcNow,
                    UnsubscribeToken = Guid.NewGuid().ToString("N"),
                    IsActive = true,
                    IsConfirmed = true, // Single opt-in - direkt aktif
                    CreatedAt = DateTime.UtcNow
                };

                await _context.NewsletterSubscribers.AddAsync(newSubscriber);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "New newsletter subscription: {Email}, Source: {Source}, IP: {IP}", 
                    email, source, ipAddress ?? "unknown");

                return NewsletterSubscribeResult.Succeeded(
                    newSubscriber.Id,
                    "Bültene başarıyla abone oldunuz. Teşekkür ederiz!");
            }
            catch (DbUpdateException dbEx)
            {
                // Unique constraint violation kontrolü (race condition durumu)
                if (dbEx.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true ||
                    dbEx.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning(
                        "Duplicate newsletter subscription attempt (race condition): {Email}", 
                        email);
                    
                    return NewsletterSubscribeResult.Failed(
                        "Bu e-posta adresi zaten kayıtlı. Lütfen başka bir e-posta deneyin.");
                }

                _logger.LogError(dbEx, "Database error during newsletter subscription: {Email}", email);
                return NewsletterSubscribeResult.Failed("Abonelik işlemi sırasında bir hata oluştu. Lütfen tekrar deneyin.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during newsletter subscription: {Email}", email);
                return NewsletterSubscribeResult.Failed("Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        /// <inheritdoc />
        public async Task<NewsletterUnsubscribeResult> UnsubscribeByTokenAsync(string unsubscribeToken, string? reason = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(unsubscribeToken))
                {
                    return NewsletterUnsubscribeResult.Failed("Geçersiz abonelik iptal token'ı.");
                }

                // Token ile abone bul
                var subscriber = await _context.NewsletterSubscribers
                    .FirstOrDefaultAsync(s => s.UnsubscribeToken == unsubscribeToken);

                if (subscriber == null)
                {
                    _logger.LogWarning("Unsubscribe attempt with invalid token: {Token}", unsubscribeToken);
                    return NewsletterUnsubscribeResult.Failed("Abonelik bulunamadı veya geçersiz link.");
                }

                // Zaten pasif mi kontrol et
                if (!subscriber.IsActive)
                {
                    return NewsletterUnsubscribeResult.Succeeded("Aboneliğiniz zaten iptal edilmiş durumda.");
                }

                // Soft delete - veriyi silme, pasif yap
                subscriber.IsActive = false;
                subscriber.UnsubscribedAt = DateTime.UtcNow;
                subscriber.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Newsletter unsubscribe by token successful: {Email}, Reason: {Reason}", 
                    subscriber.Email, reason ?? "not specified");

                return NewsletterUnsubscribeResult.Succeeded(
                    "Aboneliğiniz başarıyla iptal edildi. Artık bülten almayacaksınız.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unsubscribe by token: {Token}", unsubscribeToken);
                return NewsletterUnsubscribeResult.Failed("Abonelik iptal işlemi sırasında bir hata oluştu.");
            }
        }

        /// <inheritdoc />
        public async Task<NewsletterUnsubscribeResult> UnsubscribeByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return NewsletterUnsubscribeResult.Failed("E-posta adresi gereklidir.");
                }

                email = NormalizeEmail(email);

                var subscriber = await _context.NewsletterSubscribers
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (subscriber == null)
                {
                    return NewsletterUnsubscribeResult.Failed("Bu e-posta adresi ile kayıtlı abonelik bulunamadı.");
                }

                if (!subscriber.IsActive)
                {
                    return NewsletterUnsubscribeResult.Succeeded("Aboneliğiniz zaten iptal edilmiş durumda.");
                }

                subscriber.IsActive = false;
                subscriber.UnsubscribedAt = DateTime.UtcNow;
                subscriber.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Newsletter unsubscribe by email successful: {Email}", email);

                return NewsletterUnsubscribeResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during unsubscribe by email: {Email}", email);
                return NewsletterUnsubscribeResult.Failed("Abonelik iptal işlemi sırasında bir hata oluştu.");
            }
        }

        #endregion

        #region Admin İşlemleri

        /// <inheritdoc />
        public async Task<PagedNewsletterResult> GetAllSubscribersAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool? isActive = null,
            string? source = null,
            string sortBy = "SubscribedAt",
            bool sortDescending = true)
        {
            try
            {
                // Sayfa parametrelerini normalize et
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                // Base query - AsNoTracking performans için
                var query = _context.NewsletterSubscribers.AsNoTracking();

                // ══════════════════════════════════════════════════════════════════════
                // FİLTRELER
                // ══════════════════════════════════════════════════════════════════════

                // Aktiflik filtresi
                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                // Kaynak filtresi
                if (!string.IsNullOrWhiteSpace(source))
                {
                    var normalizedSource = source.ToLowerInvariant().Trim();
                    query = query.Where(s => s.Source == normalizedSource);
                }

                // Arama filtresi (email veya isim)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.ToLowerInvariant().Trim();
                    query = query.Where(s => 
                        s.Email.Contains(searchTerm) || 
                        (s.FullName != null && s.FullName.Contains(searchTerm)));
                }

                // ══════════════════════════════════════════════════════════════════════
                // SIRALAMA
                // ══════════════════════════════════════════════════════════════════════

                query = sortBy.ToLowerInvariant() switch
                {
                    "email" => sortDescending 
                        ? query.OrderByDescending(s => s.Email) 
                        : query.OrderBy(s => s.Email),
                    "fullname" => sortDescending 
                        ? query.OrderByDescending(s => s.FullName) 
                        : query.OrderBy(s => s.FullName),
                    "source" => sortDescending 
                        ? query.OrderByDescending(s => s.Source) 
                        : query.OrderBy(s => s.Source),
                    "createdat" => sortDescending 
                        ? query.OrderByDescending(s => s.CreatedAt) 
                        : query.OrderBy(s => s.CreatedAt),
                    "emailssentcount" => sortDescending 
                        ? query.OrderByDescending(s => s.EmailsSentCount) 
                        : query.OrderBy(s => s.EmailsSentCount),
                    _ => sortDescending 
                        ? query.OrderByDescending(s => s.SubscribedAt) 
                        : query.OrderBy(s => s.SubscribedAt)
                };

                // ══════════════════════════════════════════════════════════════════════
                // SAYFALAMA
                // ══════════════════════════════════════════════════════════════════════

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedNewsletterResult
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching newsletter subscribers");
                return new PagedNewsletterResult();
            }
        }

        /// <inheritdoc />
        public async Task<NewsletterSubscriber?> GetSubscriberByIdAsync(int id)
        {
            try
            {
                return await _context.NewsletterSubscribers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscriber by ID: {Id}", id);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<NewsletterSubscriber?> GetSubscriberByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email)) return null;
                
                email = NormalizeEmail(email);
                
                return await _context.NewsletterSubscribers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscriber by email: {Email}", email);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<NewsletterStatsResult> GetStatisticsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var sevenDaysAgo = now.AddDays(-7);
                var thirtyDaysAgo = now.AddDays(-30);

                // Tek sorgu ile tüm istatistikleri çek (performans için)
                var stats = await _context.NewsletterSubscribers
                    .AsNoTracking()
                    .GroupBy(s => 1) // Tüm kayıtları grupla
                    .Select(g => new
                    {
                        TotalSubscribers = g.Count(),
                        ActiveSubscribers = g.Count(s => s.IsActive),
                        UnsubscribedCount = g.Count(s => !s.IsActive),
                        NewLast7Days = g.Count(s => s.SubscribedAt >= sevenDaysAgo),
                        NewLast30Days = g.Count(s => s.SubscribedAt >= thirtyDaysAgo),
                        TotalEmailsSent = g.Sum(s => s.EmailsSentCount)
                    })
                    .FirstOrDefaultAsync();

                // Kaynak bazlı dağılım
                var sourceDistribution = await _context.NewsletterSubscribers
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .GroupBy(s => s.Source)
                    .Select(g => new { Source = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Source, x => x.Count);

                return new NewsletterStatsResult
                {
                    TotalSubscribers = stats?.TotalSubscribers ?? 0,
                    ActiveSubscribers = stats?.ActiveSubscribers ?? 0,
                    UnsubscribedCount = stats?.UnsubscribedCount ?? 0,
                    NewSubscribersLast7Days = stats?.NewLast7Days ?? 0,
                    NewSubscribersLast30Days = stats?.NewLast30Days ?? 0,
                    TotalEmailsSent = stats?.TotalEmailsSent ?? 0,
                    SubscribersBySource = sourceDistribution
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching newsletter statistics");
                return new NewsletterStatsResult();
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteSubscriberAsync(int id)
        {
            try
            {
                var subscriber = await _context.NewsletterSubscribers.FindAsync(id);
                
                if (subscriber == null)
                {
                    _logger.LogWarning("Delete attempt for non-existent subscriber: {Id}", id);
                    return false;
                }

                _context.NewsletterSubscribers.Remove(subscriber);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Newsletter subscriber deleted (GDPR): {Email}, ID: {Id}", 
                    subscriber.Email, id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscriber: {Id}", id);
                return false;
            }
        }

        #endregion

        #region Toplu Mail İşlemleri

        /// <inheritdoc />
        public async Task<BulkEmailResult> SendBulkEmailAsync(
            string subject,
            string body,
            bool isHtml = true,
            List<string>? sourceFilter = null,
            DateTime? subscribedAfter = null,
            DateTime? subscribedBefore = null)
        {
            try
            {
                // ══════════════════════════════════════════════════════════════════════
                // VALIDASYON
                // ══════════════════════════════════════════════════════════════════════

                if (string.IsNullOrWhiteSpace(subject))
                {
                    return BulkEmailResult.Failed("Mail konusu gereklidir.");
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return BulkEmailResult.Failed("Mail içeriği gereklidir.");
                }

                // ══════════════════════════════════════════════════════════════════════
                // AKTİF ABONELERİ SORGULA
                // Filtrelere göre gönderim yapılacak listeyi oluştur
                // ══════════════════════════════════════════════════════════════════════

                var query = _context.NewsletterSubscribers
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.IsConfirmed);

                // Kaynak filtresi
                if (sourceFilter != null && sourceFilter.Any())
                {
                    var normalizedSources = sourceFilter
                        .Select(s => s.ToLowerInvariant().Trim())
                        .ToList();
                    query = query.Where(s => normalizedSources.Contains(s.Source));
                }

                // Tarih filtreleri
                if (subscribedAfter.HasValue)
                {
                    query = query.Where(s => s.SubscribedAt >= subscribedAfter.Value);
                }

                if (subscribedBefore.HasValue)
                {
                    query = query.Where(s => s.SubscribedAt <= subscribedBefore.Value);
                }

                // Aboneleri çek
                var subscribers = await query
                    .Select(s => new { s.Id, s.Email, s.UnsubscribeToken })
                    .ToListAsync();

                if (!subscribers.Any())
                {
                    return BulkEmailResult.Failed("Gönderilecek aktif abone bulunamadı.");
                }

                // ══════════════════════════════════════════════════════════════════════
                // MAİL KUYRUĞUNA EKLE
                // Her abone için unsubscribe linki eklenerek queue'ya yazılır
                // MessageWorker background'da asenkron işler
                // ══════════════════════════════════════════════════════════════════════

                var batchId = Guid.NewGuid().ToString("N")[..12]; // Kısa batch ID
                var queuedCount = 0;
                var failedCount = 0;

                // Base URL - production'da environment'tan alınmalı
                var baseUrl = "https://localhost:5001"; // TODO: Configuration'dan al

                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        // Her abone için unsubscribe linki ekle
                        var personalizedBody = AppendUnsubscribeLink(body, subscriber.UnsubscribeToken, baseUrl);

                        await _mailQueue.EnqueueAsync(new EmailJob
                        {
                            To = subscriber.Email,
                            Subject = subject,
                            Body = personalizedBody,
                            IsHtml = isHtml
                        });

                        queuedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to queue email for: {Email}", subscriber.Email);
                        failedCount++;
                    }
                }

                // ══════════════════════════════════════════════════════════════════════
                // ABONELERİN GÖNDERİM SAYACINI GÜNCELLE
                // Toplu güncelleme - performans için tek sorgu
                // ══════════════════════════════════════════════════════════════════════

                var subscriberIds = subscribers.Select(s => s.Id).ToList();
                await _context.NewsletterSubscribers
                    .Where(s => subscriberIds.Contains(s.Id))
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(s => s.EmailsSentCount, s => s.EmailsSentCount + 1)
                        .SetProperty(s => s.LastEmailSentAt, DateTime.UtcNow));

                _logger.LogInformation(
                    "Bulk email queued: BatchId={BatchId}, Total={Total}, Queued={Queued}, Failed={Failed}",
                    batchId, subscribers.Count, queuedCount, failedCount);

                return BulkEmailResult.Succeeded(subscribers.Count, queuedCount, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk email send");
                return BulkEmailResult.Failed("Toplu mail gönderimi sırasında bir hata oluştu.");
            }
        }

        /// <inheritdoc />
        public async Task<BulkEmailResult> SendTestEmailAsync(
            string subject,
            string body,
            List<string> testEmails,
            bool isHtml = true)
        {
            try
            {
                if (testEmails == null || !testEmails.Any())
                {
                    return BulkEmailResult.Failed("Test için en az bir e-posta adresi gereklidir.");
                }

                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                {
                    return BulkEmailResult.Failed("Konu ve içerik gereklidir.");
                }

                var queuedCount = 0;
                var testToken = "TEST_TOKEN_PREVIEW";

                foreach (var email in testEmails)
                {
                    if (!IsValidEmail(email)) continue;

                    var normalizedEmail = NormalizeEmail(email);
                    var personalizedBody = AppendUnsubscribeLink(body, testToken, "https://localhost:5001");

                    await _mailQueue.EnqueueAsync(new EmailJob
                    {
                        To = normalizedEmail,
                        Subject = $"[TEST] {subject}",
                        Body = personalizedBody,
                        IsHtml = isHtml
                    });

                    queuedCount++;
                }

                _logger.LogInformation("Test email queued to {Count} addresses", queuedCount);

                return BulkEmailResult.Succeeded(testEmails.Count, queuedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during test email send");
                return BulkEmailResult.Failed("Test maili gönderilirken bir hata oluştu.");
            }
        }

        /// <inheritdoc />
        public string AppendUnsubscribeLink(string body, string unsubscribeToken, string baseUrl)
        {
            // GDPR uyumlu unsubscribe footer
            var unsubscribeUrl = $"{baseUrl}/api/newsletter/unsubscribe?token={unsubscribeToken}";
            
            var unsubscribeFooter = $@"
                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; font-size: 12px; color: #666; text-align: center;'>
                    <p>Bu e-postayı bültenimize abone olduğunuz için aldınız.</p>
                    <p>
                        <a href='{unsubscribeUrl}' style='color: #007bff; text-decoration: underline;'>
                            Abonelikten çıkmak için tıklayın
                        </a>
                    </p>
                </div>";

            // Body içinde </body> tag'i varsa onun öncesine ekle, yoksa sona ekle
            if (body.Contains("</body>", StringComparison.OrdinalIgnoreCase))
            {
                return body.Replace("</body>", $"{unsubscribeFooter}</body>", StringComparison.OrdinalIgnoreCase);
            }

            return body + unsubscribeFooter;
        }

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Email adresini normalize eder (lowercase, trim).
        /// Tutarlı karşılaştırma için gerekli.
        /// </summary>
        private static string NormalizeEmail(string email)
        {
            return email.ToLowerInvariant().Trim();
        }

        /// <summary>
        /// Email formatını validate eder.
        /// RFC 5322 standardına yakın regex kullanır.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            if (email.Length > 256) return false;
            
            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Kullanıcı girdisini sanitize eder.
        /// XSS saldırılarına karşı basit HTML temizliği.
        /// </summary>
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            // HTML tag'lerini temizle
            var sanitized = HtmlTagRegex.Replace(input, string.Empty);
            
            // Tehlikeli karakterleri encode et
            sanitized = System.Net.WebUtility.HtmlEncode(sanitized);
            
            return sanitized.Trim();
        }

        #endregion
    }
}
