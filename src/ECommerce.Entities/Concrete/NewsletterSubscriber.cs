using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Newsletter (Bülten) abonelik entity'si.
    /// 
    /// AMAÇ:
    /// - Kullanıcıların e-posta bülteni aboneliklerini yönetir
    /// - GDPR uyumlu tek tıkla abonelik iptali için token sistemi içerir
    /// - Toplu mail gönderimi için abone listesi sağlar
    /// 
    /// GÜVENLİK:
    /// - UnsubscribeToken: Benzersiz, tahmin edilemez GUID token
    /// - Email sanitization controller/service seviyesinde yapılmalı
    /// 
    /// PERFORMANS:
    /// - Email ve UnsubscribeToken alanları indexlenir (DbContext'te)
    /// - IsActive filtresi ile sadece aktif abonelere mail gönderilir
    /// </summary>
    public class NewsletterSubscriber : BaseEntity
    {
        /// <summary>
        /// Abonenin e-posta adresi.
        /// Benzersiz olmalı - aynı email ile birden fazla abonelik yapılamaz.
        /// Küçük harfe dönüştürülerek saklanmalı (service katmanında).
        /// </summary>
        [Required(ErrorMessage = "E-posta adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Abonenin tam adı (opsiyonel).
        /// Kişiselleştirilmiş mail içerikleri için kullanılır.
        /// </summary>
        [MaxLength(100, ErrorMessage = "İsim en fazla 100 karakter olabilir.")]
        public string? FullName { get; set; }

        /// <summary>
        /// Abonelik kaynağı - nereden abone olunduğunu takip eder.
        /// Değerler: "web_footer", "web_popup", "mobile_app", "checkout", "admin_import"
        /// Analitik ve pazarlama için kullanılır.
        /// </summary>
        [MaxLength(50)]
        public string Source { get; set; } = "web_footer";

        /// <summary>
        /// Abonenin IP adresi.
        /// KVKK/GDPR kanıtı için saklanır - abonelik onayının ispatı.
        /// </summary>
        [MaxLength(45)] // IPv6 adresleri için yeterli
        public string? IpAddress { get; set; }

        /// <summary>
        /// Abonelik tarihi.
        /// CreatedAt'ten farklı olarak, abonelik yenilenebilir (resubscribe).
        /// </summary>
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Abonelik iptal tarihi.
        /// Null ise aktif abonelik, doluysa iptal edilmiş demektir.
        /// Soft delete yaklaşımı - veriler silinmez, pasif yapılır.
        /// </summary>
        public DateTime? UnsubscribedAt { get; set; }

        /// <summary>
        /// GDPR uyumlu abonelik iptal token'ı.
        /// Her aboneye benzersiz token atanır.
        /// Mail içindeki "Abonelikten Çık" linki bu token'ı kullanır.
        /// Login gerektirmeden tek tıkla iptal sağlar.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string UnsubscribeToken { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Double opt-in için doğrulama token'ı (opsiyonel).
        /// KVKK'ya tam uyum için email doğrulaması gerekebilir.
        /// </summary>
        [MaxLength(64)]
        public string? ConfirmationToken { get; set; }

        /// <summary>
        /// Email doğrulanma durumu.
        /// Double opt-in aktifse, sadece doğrulanmış abonelere mail gönderilir.
        /// </summary>
        public bool IsConfirmed { get; set; } = true; // Single opt-in için varsayılan true

        /// <summary>
        /// Email doğrulanma tarihi.
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// Son gönderilen mail tarihi.
        /// Spam önleme ve frekans kontrolü için kullanılır.
        /// </summary>
        public DateTime? LastEmailSentAt { get; set; }

        /// <summary>
        /// Gönderilen toplam mail sayısı.
        /// İstatistik ve analitik için kullanılır.
        /// </summary>
        public int EmailsSentCount { get; set; } = 0;

        // ═══════════════════════════════════════════════════════════════════════════════
        // OPSIYONEL: Kayıtlı kullanıcı ile ilişkilendirme
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İlişkili kullanıcı ID'si (opsiyonel).
        /// Kayıtlı kullanıcı newsletter'a abone olduğunda bağlanır.
        /// Misafir abonelerde null kalır.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation property - İlişkili kullanıcı
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
