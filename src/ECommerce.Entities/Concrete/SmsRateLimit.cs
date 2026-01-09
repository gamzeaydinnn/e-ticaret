using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// SMS gönderim rate limiting için kayıt entity'si.
    /// Her telefon numarası için günlük ve saatlik limit takibi yapar.
    /// 
    /// Güvenlik Amaçları:
    /// - Spam/Abuse önleme
    /// - Maliyet kontrolü (SMS ücreti)
    /// - Brute-force saldırı koruması
    /// </summary>
    [Table("SmsRateLimits")]
    public class SmsRateLimit : BaseEntity
    {
        /// <summary>
        /// Rate limit uygulanan telefon numarası.
        /// Format: 5xxxxxxxxx
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Rate limit uygulanan IP adresi.
        /// IP bazlı limit için kullanılır.
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Bugün gönderilen toplam SMS sayısı
        /// </summary>
        public int DailyCount { get; set; } = 0;

        /// <summary>
        /// Son 1 saat içinde gönderilen SMS sayısı
        /// </summary>
        public int HourlyCount { get; set; } = 0;

        /// <summary>
        /// Son SMS gönderim zamanı (UTC)
        /// </summary>
        public DateTime LastSentAt { get; set; }

        /// <summary>
        /// Günlük sayacın sıfırlanacağı zaman (UTC)
        /// </summary>
        public DateTime DailyResetAt { get; set; }

        /// <summary>
        /// Saatlik sayacın sıfırlanacağı zaman (UTC)
        /// </summary>
        public DateTime HourlyResetAt { get; set; }

        /// <summary>
        /// Geçici olarak bloklanmış mı?
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// Blokaj bitiş zamanı (varsa)
        /// </summary>
        public DateTime? BlockedUntil { get; set; }

        /// <summary>
        /// Blokaj nedeni
        /// </summary>
        [StringLength(200)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Toplam başarısız deneme sayısı (şüpheli aktivite takibi)
        /// </summary>
        public int TotalFailedAttempts { get; set; } = 0;

        #region Computed Properties

        /// <summary>
        /// Şu an bloklu mu?
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyBlocked => IsBlocked && BlockedUntil.HasValue && DateTime.UtcNow < BlockedUntil.Value;

        /// <summary>
        /// Günlük sayaç sıfırlanmalı mı?
        /// </summary>
        [NotMapped]
        public bool ShouldResetDaily => DateTime.UtcNow >= DailyResetAt;

        /// <summary>
        /// Saatlik sayaç sıfırlanmalı mı?
        /// </summary>
        [NotMapped]
        public bool ShouldResetHourly => DateTime.UtcNow >= HourlyResetAt;

        /// <summary>
        /// Tekrar SMS göndermek için beklenecek süre (saniye)
        /// Cooldown: Son gönderimden bu yana 60 saniye geçmeli
        /// </summary>
        [NotMapped]
        public int CooldownRemainingSeconds
        {
            get
            {
                const int cooldownSeconds = 60;
                var elapsed = (DateTime.UtcNow - LastSentAt).TotalSeconds;
                return Math.Max(0, (int)(cooldownSeconds - elapsed));
            }
        }

        /// <summary>
        /// Cooldown süresi dolmuş mu?
        /// </summary>
        [NotMapped]
        public bool IsCooldownExpired => CooldownRemainingSeconds <= 0;

        #endregion
    }
}
