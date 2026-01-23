// ═══════════════════════════════════════════════════════════════════════════════
// POSNET İŞLEM LOG ENTITY
// Yapı Kredi POSNET işlemlerinin detaylı logları
// Audit, debug ve mutabakat için kritik veriler saklanır
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// POSNET işlem logları entity'si
    /// Her XML isteği ve yanıtı detaylı şekilde loglanır
    /// Audit trail ve debugging için kritik
    /// </summary>
    public class PosnetTransactionLog
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // PRIMARY KEY & İLİŞKİLER
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Primary Key
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// İlişkili ödeme ID'si (nullable - log önce oluşabilir)
        /// </summary>
        public int? PaymentId { get; set; }

        /// <summary>
        /// İlişkili sipariş ID'si
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Benzersiz işlem korelasyon ID'si
        /// Aynı işleme ait tüm logları ilişkilendirmek için
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

        // ═══════════════════════════════════════════════════════════════════════════════
        // İŞLEM BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem tipi: Sale, Auth, Capt, Reverse, Return, PointInquiry, OosRequest, OosTds
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = null!;

        /// <summary>
        /// İşlem alt tipi (3D Secure aşamaları için)
        /// Initiate, Callback, Complete
        /// </summary>
        [StringLength(50)]
        public string? TransactionSubType { get; set; }

        /// <summary>
        /// İşlem tutarı (YKr - Yeni Kuruş cinsinden gönderilen)
        /// </summary>
        public long? AmountInKurus { get; set; }

        /// <summary>
        /// İşlem tutarı (TL cinsinden)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// Para birimi (YT, US, EU)
        /// </summary>
        [StringLength(5)]
        public string? Currency { get; set; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public int? InstallmentCount { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // REQUEST/RESPONSE VERİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET API'ye gönderilen XML isteği
        /// Hassas veriler (kart no, CVV) maskelenerek saklanır
        /// </summary>
        public string? RequestXml { get; set; }

        /// <summary>
        /// POSNET API'den alınan XML yanıtı
        /// </summary>
        public string? ResponseXml { get; set; }

        /// <summary>
        /// İstek HTTP header'ları (JSON formatında)
        /// </summary>
        public string? RequestHeaders { get; set; }

        /// <summary>
        /// İstek gönderilen URL
        /// </summary>
        [StringLength(500)]
        public string? RequestUrl { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // POSNET SONUÇ BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// POSNET hata kodu (approved = 1, diğerleri hata)
        /// </summary>
        [StringLength(10)]
        public string? ApprovedCode { get; set; }

        /// <summary>
        /// POSNET hata kodu
        /// </summary>
        [StringLength(10)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Hata mesajı
        /// </summary>
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// HostLogKey - Banka referans numarası
        /// </summary>
        [StringLength(20)]
        public string? HostLogKey { get; set; }

        /// <summary>
        /// AuthCode - Onay kodu
        /// </summary>
        [StringLength(10)]
        public string? AuthCode { get; set; }

        /// <summary>
        /// İşlem referans ID'si (XID)
        /// </summary>
        [StringLength(50)]
        public string? TransactionId { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 3D SECURE BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 3D Secure MdStatus değeri
        /// </summary>
        [StringLength(5)]
        public string? MdStatus { get; set; }

        /// <summary>
        /// ECI değeri
        /// </summary>
        [StringLength(5)]
        public string? Eci { get; set; }

        /// <summary>
        /// CAVV değeri
        /// </summary>
        [StringLength(100)]
        public string? Cavv { get; set; }

        /// <summary>
        /// 3D Secure işlemi mi?
        /// </summary>
        public bool Is3DSecure { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // KART BİLGİLERİ (Maskelenmiş)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kart BIN numarası (ilk 6 hane)
        /// </summary>
        [StringLength(10)]
        public string? CardBin { get; set; }

        /// <summary>
        /// Kart son 4 hanesi
        /// </summary>
        [StringLength(10)]
        public string? CardLastFour { get; set; }

        /// <summary>
        /// Kart tipi (Visa, MasterCard, Troy)
        /// </summary>
        [StringLength(20)]
        public string? CardType { get; set; }

        /// <summary>
        /// Kart numarası hash'i (karşılaştırma için)
        /// </summary>
        [StringLength(64)]
        public string? CardHash { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ZAMAN DAMGALARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log oluşturulma zamanı (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// İstek gönderilme zamanı
        /// </summary>
        public DateTime? RequestSentAt { get; set; }

        /// <summary>
        /// Yanıt alınma zamanı
        /// </summary>
        public DateTime? ResponseReceivedAt { get; set; }

        /// <summary>
        /// İşlem süresi (milisaniye)
        /// </summary>
        public long? ElapsedMilliseconds { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // METADATA
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İstek yapılan IP adresi
        /// </summary>
        [StringLength(50)]
        public string? ClientIpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Kullanıcı ID (varsa)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Merchant ID
        /// </summary>
        [StringLength(20)]
        public string? MerchantId { get; set; }

        /// <summary>
        /// Terminal ID
        /// </summary>
        [StringLength(20)]
        public string? TerminalId { get; set; }

        /// <summary>
        /// Ortam: Test veya Production
        /// </summary>
        [StringLength(20)]
        public string? Environment { get; set; }

        /// <summary>
        /// Ek notlar veya açıklamalar
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Retry sayısı (eğer yeniden deneme yapıldıysa)
        /// </summary>
        public int RetryCount { get; set; } = 0;

        // ═══════════════════════════════════════════════════════════════════════════════
        // NAVIGATION PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İlişkili ödeme kaydı
        /// </summary>
        public virtual Payments? Payment { get; set; }

        /// <summary>
        /// İlişkili sipariş
        /// </summary>
        public virtual Order? Order { get; set; }
    }
}
