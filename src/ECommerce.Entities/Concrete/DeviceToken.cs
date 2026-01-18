// ==========================================================================
// DeviceToken.cs - Cihaz Token Entity
// ==========================================================================
// Push notification göndermek için kullanıcı ve kurye cihaz token'larını
// saklar. Her cihaz için platform bilgisi ve son aktiflik zamanı tutulur.
// ==========================================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Cihaz push notification token'ı.
    /// FCM, APNs, Web Push için kullanılır.
    /// </summary>
    [Table("DeviceTokens")]
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Push notification token (FCM token, APNs token vb.)
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Cihaz platformu (iOS, Android, Web)
        /// </summary>
        public DevicePlatform Platform { get; set; }

        /// <summary>
        /// İlişkili kullanıcı ID (müşteri için)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// İlişkili kurye ID
        /// </summary>
        public int? CourierId { get; set; }

        /// <summary>
        /// Cihaz adı/modeli (opsiyonel)
        /// </summary>
        [StringLength(100)]
        public string? DeviceName { get; set; }

        /// <summary>
        /// Cihaz OS versiyonu (opsiyonel)
        /// </summary>
        [StringLength(50)]
        public string? OsVersion { get; set; }

        /// <summary>
        /// Uygulama versiyonu (opsiyonel)
        /// </summary>
        [StringLength(20)]
        public string? AppVersion { get; set; }

        /// <summary>
        /// Token aktif mi?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Token oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Token son güncellenme zamanı
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Son bildirim gönderilme zamanı
        /// </summary>
        public DateTime? LastNotificationAt { get; set; }

        /// <summary>
        /// Başarısız gönderim sayısı (3'ten fazlaysa devre dışı bırakılır)
        /// </summary>
        public int FailedAttempts { get; set; } = 0;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CourierId")]
        public virtual Courier? Courier { get; set; }
    }
}
