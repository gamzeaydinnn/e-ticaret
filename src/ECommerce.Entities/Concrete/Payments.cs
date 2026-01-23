//	• Payments (Id, OrderId, Provider, ProviderPaymentId, Amount, Status, CreatedAt, RawResponse)
// Ödeme işlem kayıtları entity'si - Tüm ödeme sağlayıcıları için ortak
// POSNET entegrasyonu için genişletilmiş alanlar eklendi (v2.0)
using System;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ödeme işlem kayıtları entity'si
    /// Stripe, Iyzico, PayPal, PayTR, POSNET gibi tüm ödeme sağlayıcıları için ortak yapı
    /// </summary>
    public class Payments
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // TEMEL ALANLAR (Mevcut)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Primary Key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// İlişkili sipariş ID'si
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Ödeme sağlayıcı adı (Stripe, Iyzico, PayPal, PayTR, YapiKredi vs.)
        /// </summary>
        public string Provider { get; set; } = null!;

        /// <summary>
        /// Sağlayıcıdaki benzersiz ödeme ID'si
        /// POSNET için: HostLogKey
        /// </summary>
        public string ProviderPaymentId { get; set; } = null!;

        /// <summary>
        /// Ödeme tutarı (TL)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Ödeme durumu: Pending, Success, Failed, Refunded, Cancelled
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Kayıt oluşturulma tarihi (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ödeme tamamlanma tarihi (UTC)
        /// </summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// API'den gelen ham yanıt (JSON/XML)
        /// Debug ve audit amaçlı saklanır
        /// </summary>
        public string? RawResponse { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // POSNET ÖZEL ALANLARI (Yeni - v2.0)
        // Yapı Kredi POSNET entegrasyonu için gerekli alanlar
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET HostLogKey - Banka tarafından atanan işlem referans numarası
        /// 12 haneli string, iptal/iade işlemlerinde gerekli
        /// </summary>
        public string? HostLogKey { get; set; }

        /// <summary>
        /// POSNET AuthCode - Banka onay kodu
        /// 6 haneli, dekont ve mutabakat için gerekli
        /// </summary>
        public string? AuthCode { get; set; }

        /// <summary>
        /// Taksit sayısı (0 veya 1 = Peşin, 2-12 = Taksitli)
        /// </summary>
        public int InstallmentCount { get; set; } = 0;

        /// <summary>
        /// 3D Secure MdStatus değeri
        /// 1 = Tam doğrulama, 2-4 = Kısmi, 0/5-9 = Başarısız
        /// </summary>
        public string? MdStatus { get; set; }

        /// <summary>
        /// Electronic Commerce Indicator (ECI)
        /// 3D Secure işlem göstergesi
        /// </summary>
        public string? Eci { get; set; }

        /// <summary>
        /// Cardholder Authentication Verification Value (CAVV)
        /// 3D Secure doğrulama değeri
        /// </summary>
        public string? Cavv { get; set; }

        /// <summary>
        /// Kart BIN numarası (ilk 6 hane) - Maskelenmiş
        /// Banka/Kart tipi tespiti için
        /// </summary>
        public string? CardBin { get; set; }

        /// <summary>
        /// Kart son 4 hanesi - Maskelenmiş
        /// Müşteri gösterimi için güvenli
        /// </summary>
        public string? CardLastFour { get; set; }

        /// <summary>
        /// Kart tipi (Visa, MasterCard, Troy vs.)
        /// </summary>
        public string? CardType { get; set; }

        /// <summary>
        /// İşlem tipi: Sale, Auth, Capt, Reverse, Return, PointSale
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// İşlem referans ID'si (XID)
        /// 3D Secure işlemlerinde kullanılır
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// İade/İptal işlem referansı
        /// Bu işlem bir iade/iptal ise, orijinal işlemin ID'si
        /// </summary>
        public int? OriginalPaymentId { get; set; }

        /// <summary>
        /// İade edilen tutar (kısmi iade için)
        /// </summary>
        public decimal? RefundedAmount { get; set; }

        /// <summary>
        /// Kullanılan World Puan miktarı
        /// </summary>
        public int? UsedWorldPoints { get; set; }

        /// <summary>
        /// Para birimi kodu (TRY, USD, EUR)
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// IP adresi (fraud detection için)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // NAVIGATION PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İlişkili sipariş
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// Orijinal ödeme (iade/iptal için)
        /// </summary>
        public virtual Payments? OriginalPayment { get; set; }

        /// <summary>
        /// Bu ödemeye ait iade/iptal işlemleri
        /// </summary>
        public virtual ICollection<Payments>? RefundPayments { get; set; }
    }
}
