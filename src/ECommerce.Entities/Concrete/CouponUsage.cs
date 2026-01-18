// =============================================================================
// CouponUsage Entity - Kupon Kullanım Geçmişi
// =============================================================================
// Bu entity, kuponların kullanım geçmişini takip eder.
// Hangi kullanıcı, hangi siparişte, hangi kuponu ne zaman kullandı bilgisini tutar.
// Kullanıcı başına kullanım limiti kontrolü için kritik öneme sahiptir.
// Ayrıca raporlama ve analiz için değerli veriler sağlar.
// =============================================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kupon kullanım geçmişi entity'si.
    /// Her kupon kullanımı için bir kayıt oluşturulur.
    /// </summary>
    public class CouponUsage : BaseEntity
    {
        // =============================================================================
        // İlişki Bilgileri
        // =============================================================================

        /// <summary>
        /// Kullanılan kuponun ID'si
        /// </summary>
        [Required]
        public int CouponId { get; set; }

        /// <summary>
        /// Kuponu kullanan kullanıcının ID'si
        /// Guest siparişlerinde null olabilir
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Kuponun kullanıldığı siparişin ID'si
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        // =============================================================================
        // Kullanım Detayları
        // =============================================================================

        /// <summary>
        /// Kuponun kullanıldığı tarih/saat
        /// </summary>
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Uygulanan indirim tutarı (TL)
        /// Raporlama için önemli - kuponun o siparişte sağladığı indirim
        /// </summary>
        [Range(0, 1000000)]
        public decimal DiscountApplied { get; set; }

        /// <summary>
        /// Sipariş toplam tutarı (indirim öncesi)
        /// Analiz için - kuponun uygulandığı sepet değeri
        /// </summary>
        public decimal OrderTotalBeforeDiscount { get; set; }

        /// <summary>
        /// Sipariş son tutarı (indirim sonrası)
        /// </summary>
        public decimal OrderTotalAfterDiscount { get; set; }

        /// <summary>
        /// Kullanılan kupon kodu (snapshot)
        /// Kupon kodu değişse bile geçmiş kayıtlarda orijinal kod kalır
        /// </summary>
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;

        /// <summary>
        /// Kupon türü (snapshot)
        /// Kupon türü değişse bile geçmiş kayıtlarda orijinal tür kalır
        /// </summary>
        [StringLength(50)]
        public string CouponType { get; set; } = string.Empty;

        // =============================================================================
        // Ek Bilgiler
        // =============================================================================

        /// <summary>
        /// Kullanıcı IP adresi - Güvenlik ve dolandırıcılık tespiti için
        /// </summary>
        [StringLength(45)] // IPv6 için yeterli
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent - Hangi cihaz/tarayıcıdan kullanıldı
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Session ID - Aynı oturumdaki işlemleri gruplamak için
        /// </summary>
        [StringLength(100)]
        public string? SessionId { get; set; }

        // =============================================================================
        // Navigation Properties
        // =============================================================================

        /// <summary>
        /// İlişkili kupon
        /// </summary>
        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; set; } = null!;

        /// <summary>
        /// Kuponu kullanan kullanıcı
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// İlişkili sipariş
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}
