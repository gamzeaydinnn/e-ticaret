using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ECommerce.API.DTOs.Newsletter
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // NEWSLETTER REQUEST DTO'LARI
    // Kullanıcıdan gelen istekleri validate etmek için kullanılır
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Newsletter abonelik isteği DTO.
    /// Kullanıcının bültene abone olması için gerekli bilgileri içerir.
    /// 
    /// GÜVENLİK:
    /// - Email formatı regex ile validate edilir
    /// - HTML/Script injection'a karşı service katmanında sanitize edilir
    /// </summary>
    public class NewsletterSubscribeRequestDto
    {
        /// <summary>
        /// Abonenin e-posta adresi.
        /// Zorunlu alan, geçerli email formatında olmalı.
        /// </summary>
        [Required(ErrorMessage = "E-posta adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Abonenin tam adı (opsiyonel).
        /// Kişiselleştirilmiş içerik için kullanılır.
        /// </summary>
        [MaxLength(100, ErrorMessage = "İsim en fazla 100 karakter olabilir.")]
        public string? FullName { get; set; }

        /// <summary>
        /// Abonelik kaynağı.
        /// Frontend'den otomatik olarak gönderilir: "web_footer", "web_popup", "mobile_app", "checkout"
        /// </summary>
        [MaxLength(50)]
        public string? Source { get; set; }
    }

    /// <summary>
    /// Token bazlı abonelik iptal isteği DTO.
    /// Email içindeki "Abonelikten Çık" linki için kullanılır.
    /// Login gerektirmez - GDPR uyumlu.
    /// </summary>
    public class NewsletterUnsubscribeRequestDto
    {
        /// <summary>
        /// Benzersiz abonelik iptal token'ı.
        /// Her aboneye özel, tahmin edilemez GUID formatında.
        /// </summary>
        [Required(ErrorMessage = "Unsubscribe token gereklidir.")]
        [MaxLength(64)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// İptal sebebi (opsiyonel).
        /// Analitik için kullanılır - neden iptal edildiğini anlamak için.
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Toplu mail gönderim isteği DTO.
    /// Admin panelinden newsletter gönderimi için kullanılır.
    /// </summary>
    public class SendBulkEmailRequestDto
    {
        /// <summary>
        /// E-posta konusu.
        /// Zorunlu alan, en az 3, en fazla 200 karakter.
        /// </summary>
        [Required(ErrorMessage = "E-posta konusu gereklidir.")]
        [MinLength(3, ErrorMessage = "Konu en az 3 karakter olmalıdır.")]
        [MaxLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// E-posta içeriği (HTML destekli).
        /// Zorunlu alan, en az 10 karakter.
        /// TinyMCE/Quill editör çıktısı HTML formatında olacak.
        /// </summary>
        [Required(ErrorMessage = "E-posta içeriği gereklidir.")]
        [MinLength(10, ErrorMessage = "İçerik en az 10 karakter olmalıdır.")]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// HTML formatında mı?
        /// true: HTML içerik (varsayılan)
        /// false: Plain text içerik
        /// </summary>
        public bool IsHtml { get; set; } = true;

        /// <summary>
        /// Test modu - sadece belirli email'lere gönderir.
        /// true: TestEmails listesine gönderir
        /// false: Tüm aktif abonelere gönderir
        /// </summary>
        public bool IsTestMode { get; set; } = false;

        /// <summary>
        /// Test modunda gönderilecek email adresleri.
        /// Gerçek gönderim öncesi test için kullanılır.
        /// </summary>
        public List<string>? TestEmails { get; set; }

        /// <summary>
        /// Filtre: Sadece belirli kaynaklardan gelen abonelere gönder.
        /// Boş ise tüm kaynaklara gönderir.
        /// Örnek: ["web_footer", "mobile_app"]
        /// </summary>
        public List<string>? SourceFilter { get; set; }

        /// <summary>
        /// Filtre: Minimum abonelik tarihi.
        /// Bu tarihten sonra abone olanlar dahil edilir.
        /// </summary>
        public DateTime? SubscribedAfter { get; set; }

        /// <summary>
        /// Filtre: Maksimum abonelik tarihi.
        /// Bu tarihten önce abone olanlar dahil edilir.
        /// </summary>
        public DateTime? SubscribedBefore { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // NEWSLETTER RESPONSE DTO'LARI
    // API yanıtlarını standartlaştırmak için kullanılır
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Newsletter abonelik yanıt DTO.
    /// Başarılı abonelik sonrası döndürülür.
    /// </summary>
    public class NewsletterSubscribeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Abonelik ID'si (opsiyonel).
        /// Frontend'de tracking için kullanılabilir.
        /// </summary>
        public int? SubscriberId { get; set; }

        /// <summary>
        /// Önceden abone miydi?
        /// true: Daha önce abone olmuş, tekrar aktivasyon
        /// false: Yeni abonelik
        /// </summary>
        public bool WasAlreadySubscribed { get; set; } = false;
    }

    /// <summary>
    /// Newsletter abonelik iptal yanıt DTO.
    /// </summary>
    public class NewsletterUnsubscribeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Toplu mail gönderim yanıt DTO.
    /// </summary>
    public class SendBulkEmailResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Toplam abone sayısı (filtreleme sonrası).
        /// </summary>
        public int TotalSubscribers { get; set; }
        
        /// <summary>
        /// Mail kuyruğuna eklenen sayı.
        /// </summary>
        public int QueuedCount { get; set; }
        
        /// <summary>
        /// Hata nedeniyle kuyruka eklenemeyen sayı.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Toplu mail gönderim işlemi ID'si.
        /// Durum takibi için kullanılır.
        /// </summary>
        public string? BatchId { get; set; }
    }

    /// <summary>
    /// Admin paneli için abone listesi DTO.
    /// Hassas bilgiler filtrelenerek döndürülür.
    /// </summary>
    public class NewsletterSubscriberDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime SubscribedAt { get; set; }
        public DateTime? UnsubscribedAt { get; set; }
        public DateTime? LastEmailSentAt { get; set; }
        public int EmailsSentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// İlişkili kullanıcı ID'si (varsa).
        /// Kayıtlı kullanıcı bilgisi için.
        /// </summary>
        public int? UserId { get; set; }
    }

    /// <summary>
    /// Newsletter istatistikleri DTO.
    /// Admin dashboard için özet bilgiler.
    /// </summary>
    public class NewsletterStatsDto
    {
        /// <summary>
        /// Toplam abone sayısı (aktif + pasif).
        /// </summary>
        public int TotalSubscribers { get; set; }

        /// <summary>
        /// Aktif abone sayısı.
        /// </summary>
        public int ActiveSubscribers { get; set; }

        /// <summary>
        /// Abonelikten çıkan sayısı.
        /// </summary>
        public int UnsubscribedCount { get; set; }

        /// <summary>
        /// Son 7 günde yeni abone sayısı.
        /// </summary>
        public int NewSubscribersLast7Days { get; set; }

        /// <summary>
        /// Son 30 günde yeni abone sayısı.
        /// </summary>
        public int NewSubscribersLast30Days { get; set; }

        /// <summary>
        /// Gönderilen toplam mail sayısı.
        /// </summary>
        public int TotalEmailsSent { get; set; }

        /// <summary>
        /// Kaynak bazlı dağılım.
        /// Key: Kaynak adı, Value: Abone sayısı
        /// </summary>
        public Dictionary<string, int> SubscribersBySource { get; set; } = new();
    }

    /// <summary>
    /// Sayfalı abone listesi için sorgu DTO.
    /// </summary>
    public class NewsletterQueryDto
    {
        /// <summary>
        /// Sayfa numarası (1'den başlar).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Sayfa numarası 1'den büyük olmalıdır.")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Sayfa başına kayıt sayısı.
        /// </summary>
        [Range(1, 100, ErrorMessage = "Sayfa boyutu 1-100 arasında olmalıdır.")]
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Arama terimi (email veya isim).
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Sadece aktif aboneleri getir.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Kaynak filtresi.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Sıralama alanı.
        /// </summary>
        public string SortBy { get; set; } = "SubscribedAt";

        /// <summary>
        /// Sıralama yönü.
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Sayfalı yanıt wrapper DTO.
    /// </summary>
    public class PagedNewsletterResponseDto
    {
        public List<NewsletterSubscriberDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
