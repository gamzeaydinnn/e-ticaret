using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ödeme sağlayıcılarından gelen webhook event'lerinin kayıt tablosu
    /// Idempotency (tekrar eden event'leri engellemek) için kritik öneme sahip
    /// 
    /// AMAÇ:
    /// 1. Aynı webhook event'inin birden fazla kez işlenmesini önlemek (retry durumları)
    /// 2. Webhook trafiğini audit amaçlı loglamak
    /// 3. Replay attack'ları tespit etmek (timestamp kontrolü ile)
    /// 4. Debug ve sorun giderme için ham veriyi saklamak
    /// 
    /// KULLANIM:
    /// Her webhook geldiğinde önce bu tabloya bakılır:
    /// - ProviderEventId zaten varsa → 200 OK dön, işleme YAPMA
    /// - Yoksa → Kaydet, işle, ProcessedAt güncelle
    /// </summary>
    public class PaymentWebhookEvent : BaseEntity
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // PROVIDER BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ödeme sağlayıcı adı
        /// Değerler: "Iyzico", "Stripe", "PayTR", "Posnet", "PayPal"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = null!;

        /// <summary>
        /// Sağlayıcının event'e atadığı benzersiz ID
        /// UNIQUE constraint ile idempotency sağlanır
        /// 
        /// Örnekler:
        /// - Stripe: evt_1234567890
        /// - Iyzico: iyziEventId header'ından
        /// - PayTR: hash değeri
        /// - Posnet: HostLogKey + timestamp kombinasyonu
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ProviderEventId { get; set; } = null!;

        /// <summary>
        /// Ödeme intent/işlem ID'si
        /// Hangi ödeme işlemiyle ilgili olduğunu belirtir
        /// 
        /// Örnekler:
        /// - Stripe: pi_xxxxx (PaymentIntent)
        /// - Iyzico: paymentId
        /// - Posnet: HostLogKey
        /// </summary>
        [MaxLength(255)]
        public string? PaymentIntentId { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // EVENT BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Event tipi
        /// 
        /// Örnekler:
        /// - Stripe: payment_intent.succeeded, charge.refunded
        /// - Iyzico: CREDIT_PAYMENT_AUTH, CREDIT_PAYMENT_CAPTURE
        /// - PayTR: Callback (tek tip)
        /// - Posnet: 3DSecureCallback, PaymentResult
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = null!;

        /// <summary>
        /// Event'in oluşturulma tarihi (sağlayıcı tarafından)
        /// Replay attack kontrolü için kullanılır
        /// Çok eski tarihli event'ler (örn: 5 dk+) reddedilebilir
        /// </summary>
        public DateTime? EventTimestamp { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // İŞLEME DURUMU
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Webhook'un alındığı tarih (UTC)
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// İşleme alındığı tarih (UTC)
        /// NULL ise henüz işlenmemiş demektir
        /// 
        /// İşleme sırası:
        /// 1. ReceivedAt set edilir
        /// 2. Business logic çalışır
        /// 3. Başarılı olursa ProcessedAt set edilir
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// İşleme durumu
        /// Pending: Alındı, henüz işlenmedi
        /// Success: Başarıyla işlendi
        /// Failed: İşleme sırasında hata oluştu
        /// Skipped: Kasıtlı olarak atlandı (duplicate vs.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ProcessingStatus { get; set; } = "Pending";

        /// <summary>
        /// Hata durumunda hata mesajı
        /// Debug için saklanır
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Kaç kez işlenmeye çalışıldı
        /// Retry mekanizması için kullanılır
        /// </summary>
        public int RetryCount { get; set; } = 0;

        // ═══════════════════════════════════════════════════════════════════════════════
        // GÜVENLİK VE AUDIT
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Webhook ile gelen imza
        /// HMAC doğrulaması için kullanılır
        /// Başarılı doğrulamadan sonra saklanır
        /// </summary>
        [MaxLength(512)]
        public string? Signature { get; set; }

        /// <summary>
        /// İmza doğrulaması başarılı mı?
        /// false ise webhook işlenmemeli
        /// </summary>
        public bool SignatureValid { get; set; } = false;

        /// <summary>
        /// Gönderen IP adresi
        /// Whitelist kontrolü için kullanılabilir
        /// </summary>
        [MaxLength(45)] // IPv6 max uzunluğu
        public string? SourceIpAddress { get; set; }

        /// <summary>
        /// Ham webhook body'si (JSON)
        /// Debug ve audit için saklanır
        /// 
        /// NOT: PII (kart no, CVV vb.) maskelenmiş olmalı!
        /// </summary>
        public string? RawPayload { get; set; }

        /// <summary>
        /// HTTP headers (JSON formatında)
        /// Debug için gerekebilir
        /// </summary>
        public string? HttpHeaders { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // İLİŞKİLER
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İlişkili sipariş ID'si (varsa)
        /// Webhook işlendikten sonra set edilir
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// İlişkili ödeme kaydı ID'si (varsa)
        /// Webhook işlendikten sonra set edilir
        /// </summary>
        public int? PaymentId { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("PaymentId")]
        public virtual Payments? Payment { get; set; }
    }
}
