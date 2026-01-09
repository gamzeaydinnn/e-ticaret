using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// SMS doğrulama kaydı entity'si.
    /// Her OTP gönderimi için bir kayıt oluşturulur.
    /// 
    /// Güvenlik Özellikleri:
    /// - Kod hashlenerek saklanır (opsiyonel, şimdilik plain text)
    /// - IP adresi kaydedilir
    /// - Deneme sayısı takip edilir
    /// - Otomatik süre sonu kontrolü
    /// </summary>
    [Table("SmsVerifications")]
    public class SmsVerification : BaseEntity
    {
        /// <summary>
        /// Doğrulama yapılacak telefon numarası.
        /// Format: 5xxxxxxxxx (başında 0 olmadan)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 6 haneli doğrulama kodu.
        /// Production'da hash'lenmiş saklanması önerilir.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Kodun hash'lenmiş hali (güvenlik için).
        /// Şu an kullanılmıyor, ileride aktif edilebilir.
        /// </summary>
        [StringLength(256)]
        public string? CodeHash { get; set; }

        /// <summary>
        /// Doğrulama amacı (Kayıt, Şifre sıfırlama, 2FA vb.)
        /// </summary>
        [Required]
        public SmsVerificationPurpose Purpose { get; set; }

        /// <summary>
        /// Doğrulama kaydının mevcut durumu
        /// </summary>
        [Required]
        public SmsVerificationStatus Status { get; set; } = SmsVerificationStatus.Pending;

        /// <summary>
        /// Kodun geçerlilik bitiş zamanı (UTC)
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Doğrulamanın yapıldığı zaman (UTC)
        /// Null ise henüz doğrulanmamış
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Yanlış kod girme deneme sayısı
        /// </summary>
        public int WrongAttempts { get; set; } = 0;

        /// <summary>
        /// Maksimum izin verilen yanlış deneme sayısı
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// İsteği yapan kullanıcının IP adresi
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Kullanıcının tarayıcı/cihaz bilgisi
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// NetGSM'den dönen görev ID'si.
        /// SMS durumu sorgulama için kullanılır.
        /// </summary>
        [StringLength(100)]
        public string? JobId { get; set; }

        /// <summary>
        /// İlişkili kullanıcı ID'si (varsa).
        /// Kayıt sırasında henüz kullanıcı olmayabilir.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// SMS'in başarıyla gönderilip gönderilmediği
        /// </summary>
        public bool SmsSent { get; set; } = false;

        /// <summary>
        /// SMS gönderim hata mesajı (varsa)
        /// </summary>
        [StringLength(500)]
        public string? SmsErrorMessage { get; set; }

        #region Navigation Properties

        /// <summary>
        /// İlişkili kullanıcı (opsiyonel)
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Kodun süresi dolmuş mu?
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Kod kullanılmış mı?
        /// </summary>
        [NotMapped]
        public bool IsUsed => Status == SmsVerificationStatus.Verified;

        /// <summary>
        /// Maksimum deneme aşıldı mı?
        /// </summary>
        [NotMapped]
        public bool IsMaxAttemptsExceeded => WrongAttempts >= MaxAttempts;

        /// <summary>
        /// Kod hala geçerli mi? (süre dolmamış, kullanılmamış, deneme aşılmamış)
        /// </summary>
        [NotMapped]
        public bool IsValid => !IsExpired && !IsUsed && !IsMaxAttemptsExceeded && Status == SmsVerificationStatus.Pending;

        /// <summary>
        /// Kalan deneme hakkı
        /// </summary>
        [NotMapped]
        public int RemainingAttempts => Math.Max(0, MaxAttempts - WrongAttempts);

        /// <summary>
        /// Kodun geçerliliğinin bitmesine kalan saniye
        /// </summary>
        [NotMapped]
        public int RemainingSeconds => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);

        #endregion
    }
}
