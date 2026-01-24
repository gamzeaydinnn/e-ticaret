using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Newsletter (Bülten) servisi interface'i.
    /// 
    /// AMAÇ:
    /// - Newsletter abonelik işlemlerini yönetir (Subscribe/Unsubscribe)
    /// - Abone listesi CRUD operasyonlarını sağlar
    /// - Toplu mail gönderim işlemlerini koordine eder
    /// 
    /// SOLID PRENSİBİ:
    /// - Interface Segregation: Sadece newsletter işlemleri için özelleştirilmiş
    /// - Dependency Inversion: Controller'lar bu interface'e bağımlı, somut sınıfa değil
    /// 
    /// TEST EDİLEBİLİRLİK:
    /// - Bu interface Mock'lanarak unit test yazılabilir
    /// - Dependency Injection ile inject edilir
    /// </summary>
    public interface INewsletterService
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // ABONELİK İŞLEMLERİ
        // Public endpoint'lerden çağrılır - authentication gerektirmez
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Yeni newsletter aboneliği oluşturur.
        /// 
        /// İŞ KURALLARI:
        /// - Email zaten varsa ve aktifse: "Zaten abone" mesajı döner
        /// - Email varsa ama pasifse: Yeniden aktif eder (resubscribe)
        /// - Yeni email: Yeni kayıt oluşturur
        /// 
        /// GÜVENLİK:
        /// - Email lowercase'e dönüştürülür
        /// - HTML/Script sanitize edilir
        /// - IP adresi kaydedilir (KVKK kanıtı)
        /// </summary>
        /// <param name="email">Abone email adresi</param>
        /// <param name="fullName">Abone adı (opsiyonel)</param>
        /// <param name="source">Abonelik kaynağı (web_footer, popup, vs.)</param>
        /// <param name="ipAddress">Kullanıcı IP adresi</param>
        /// <param name="userId">Kayıtlı kullanıcı ID'si (opsiyonel)</param>
        /// <returns>Abonelik sonucu (başarı durumu, mesaj, abone ID)</returns>
        Task<NewsletterSubscribeResult> SubscribeAsync(
            string email, 
            string? fullName = null, 
            string? source = null, 
            string? ipAddress = null,
            int? userId = null);

        /// <summary>
        /// Token bazlı abonelik iptali - GDPR uyumlu.
        /// Login gerektirmez, mail içindeki link ile çalışır.
        /// 
        /// GÜVENLİK:
        /// - Token tahmin edilemez (GUID formatında)
        /// - Zaman aşımı yok - kalıcı token
        /// - Soft delete - veri silinmez, pasif yapılır
        /// </summary>
        /// <param name="unsubscribeToken">Benzersiz abonelik iptal token'ı</param>
        /// <param name="reason">İptal sebebi (opsiyonel, analitik için)</param>
        /// <returns>İşlem sonucu</returns>
        Task<NewsletterUnsubscribeResult> UnsubscribeByTokenAsync(string unsubscribeToken, string? reason = null);

        /// <summary>
        /// Email adresi ile abonelik iptali - Admin veya kullanıcı profili için.
        /// </summary>
        /// <param name="email">Abone email adresi</param>
        /// <returns>İşlem sonucu</returns>
        Task<NewsletterUnsubscribeResult> UnsubscribeByEmailAsync(string email);

        // ═══════════════════════════════════════════════════════════════════════════════
        // ADMIN İŞLEMLERİ
        // Sadece Admin rolündeki kullanıcılar erişebilir
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tüm aboneleri sayfalı olarak getirir.
        /// 
        /// PERFORMANS:
        /// - Sayfalama ile bellek optimizasyonu
        /// - Index'li sorgular (Email, IsActive)
        /// - Lazy loading kullanılmaz (Select projection)
        /// </summary>
        /// <param name="page">Sayfa numarası (1'den başlar)</param>
        /// <param name="pageSize">Sayfa başına kayıt</param>
        /// <param name="search">Email veya isim araması</param>
        /// <param name="isActive">Aktiflik filtresi</param>
        /// <param name="source">Kaynak filtresi</param>
        /// <param name="sortBy">Sıralama alanı</param>
        /// <param name="sortDescending">Azalan sıralama mı</param>
        /// <returns>Sayfalı abone listesi</returns>
        Task<PagedNewsletterResult> GetAllSubscribersAsync(
            int page = 1, 
            int pageSize = 20, 
            string? search = null, 
            bool? isActive = null,
            string? source = null,
            string sortBy = "SubscribedAt",
            bool sortDescending = true);

        /// <summary>
        /// Tek bir aboneyi ID ile getirir.
        /// </summary>
        /// <param name="id">Abone ID</param>
        /// <returns>Abone bilgisi veya null</returns>
        Task<NewsletterSubscriber?> GetSubscriberByIdAsync(int id);

        /// <summary>
        /// Tek bir aboneyi email ile getirir.
        /// </summary>
        /// <param name="email">Abone email adresi</param>
        /// <returns>Abone bilgisi veya null</returns>
        Task<NewsletterSubscriber?> GetSubscriberByEmailAsync(string email);

        /// <summary>
        /// Newsletter istatistiklerini getirir.
        /// Admin dashboard için özet bilgiler.
        /// </summary>
        /// <returns>İstatistik özeti</returns>
        Task<NewsletterStatsResult> GetStatisticsAsync();

        /// <summary>
        /// Abone kaydını siler (hard delete).
        /// GDPR "Unutulma Hakkı" için kullanılır.
        /// </summary>
        /// <param name="id">Silinecek abone ID</param>
        /// <returns>İşlem sonucu</returns>
        Task<bool> DeleteSubscriberAsync(int id);

        // ═══════════════════════════════════════════════════════════════════════════════
        // TOPLU MAİL İŞLEMLERİ
        // Mevcut MailQueue sistemini kullanır - MessageWorker tarafından işlenir
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Toplu mail gönderim işlemini başlatır.
        /// 
        /// ÇALIŞMA PRENSİBİ:
        /// - Filtrelere göre aktif aboneleri çeker
        /// - Her abone için MailQueue'ya EmailJob ekler
        /// - MessageWorker background'da asenkron işler
        /// - Retry mekanizması mevcut (3 deneme)
        /// 
        /// PERFORMANS:
        /// - Streaming ile bellek optimizasyonu (ToAsyncEnumerable)
        /// - Batch halinde queue'ya ekleme
        /// - Rate limiting ile SMTP koruması
        /// 
        /// GÜVENLİK:
        /// - Mail içeriği XSS için sanitize edilmeli (controller seviyesinde)
        /// - Unsubscribe linki otomatik eklenir
        /// </summary>
        /// <param name="subject">Mail konusu</param>
        /// <param name="body">Mail içeriği (HTML destekli)</param>
        /// <param name="isHtml">HTML formatında mı</param>
        /// <param name="sourceFilter">Kaynak filtresi (opsiyonel)</param>
        /// <param name="subscribedAfter">Minimum abonelik tarihi filtresi</param>
        /// <param name="subscribedBefore">Maksimum abonelik tarihi filtresi</param>
        /// <returns>Gönderim sonucu (kuyruğa eklenen sayı, hata sayısı)</returns>
        Task<BulkEmailResult> SendBulkEmailAsync(
            string subject,
            string body,
            bool isHtml = true,
            List<string>? sourceFilter = null,
            DateTime? subscribedAfter = null,
            DateTime? subscribedBefore = null);

        /// <summary>
        /// Test modu - belirli email adreslerine gönderir.
        /// Gerçek gönderim öncesi önizleme için kullanılır.
        /// </summary>
        /// <param name="subject">Mail konusu</param>
        /// <param name="body">Mail içeriği</param>
        /// <param name="testEmails">Test email adresleri</param>
        /// <param name="isHtml">HTML formatında mı</param>
        /// <returns>Gönderim sonucu</returns>
        Task<BulkEmailResult> SendTestEmailAsync(
            string subject,
            string body,
            List<string> testEmails,
            bool isHtml = true);

        /// <summary>
        /// Mail içeriğine unsubscribe linkini ekler.
        /// Her mailde GDPR uyumlu iptal linki bulunmalı.
        /// </summary>
        /// <param name="body">Orijinal mail içeriği</param>
        /// <param name="unsubscribeToken">Abonenin unsubscribe token'ı</param>
        /// <param name="baseUrl">Site base URL'i</param>
        /// <returns>Unsubscribe linki eklenmiş içerik</returns>
        string AppendUnsubscribeLink(string body, string unsubscribeToken, string baseUrl);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // RESULT SINFLARI
    // İşlem sonuçlarını standartlaştırmak için kullanılır
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Abonelik işlemi sonucu
    /// </summary>
    public class NewsletterSubscribeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? SubscriberId { get; set; }
        public bool WasAlreadySubscribed { get; set; }

        public static NewsletterSubscribeResult Succeeded(int subscriberId, string message = "Bültene başarıyla abone oldunuz.", bool wasAlreadySubscribed = false)
            => new() { Success = true, Message = message, SubscriberId = subscriberId, WasAlreadySubscribed = wasAlreadySubscribed };

        public static NewsletterSubscribeResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Abonelik iptal sonucu
    /// </summary>
    public class NewsletterUnsubscribeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static NewsletterUnsubscribeResult Succeeded(string message = "Aboneliğiniz başarıyla iptal edildi.")
            => new() { Success = true, Message = message };

        public static NewsletterUnsubscribeResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Sayfalı abone listesi sonucu
    /// </summary>
    public class PagedNewsletterResult
    {
        public List<NewsletterSubscriber> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Newsletter istatistikleri
    /// </summary>
    public class NewsletterStatsResult
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int UnsubscribedCount { get; set; }
        public int NewSubscribersLast7Days { get; set; }
        public int NewSubscribersLast30Days { get; set; }
        public int TotalEmailsSent { get; set; }
        public Dictionary<string, int> SubscribersBySource { get; set; } = new();
    }

    /// <summary>
    /// Toplu mail gönderim sonucu
    /// </summary>
    public class BulkEmailResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalSubscribers { get; set; }
        public int QueuedCount { get; set; }
        public int FailedCount { get; set; }
        public string? BatchId { get; set; }

        public static BulkEmailResult Succeeded(int totalSubscribers, int queuedCount, string? batchId = null)
            => new() 
            { 
                Success = true, 
                Message = $"{queuedCount} mail kuyruğa eklendi.",
                TotalSubscribers = totalSubscribers,
                QueuedCount = queuedCount,
                BatchId = batchId
            };

        public static BulkEmailResult Failed(string message, int failedCount = 0)
            => new() { Success = false, Message = message, FailedCount = failedCount };
    }
}
