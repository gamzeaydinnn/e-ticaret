// ==========================================================================
// RefundRequest.cs - İade Talebi Entity'si
// ==========================================================================
// Müşteri iade taleplerini takip eden entity.
// Kargo durumuna göre iki farklı akış yönetir:
//   1. Kargo yola çıkmamış → Otomatik iptal + POSNET reverse
//   2. Kargo yola çıkmış  → Admin onaylı POSNET return (iade)
//
// NEDEN BaseEntity'den türetildi:
//   Id, IsActive, CreatedAt, UpdatedAt zaten BaseEntity'de mevcut.
//   Proje genelinde tüm entity'ler bu yapıyı kullanıyor.
// ==========================================================================

using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Müşteri iade talebi entity'si.
    /// Sipariş bazlı iade sürecini baştan sona takip eder.
    /// Her sipariş için birden fazla iade talebi oluşturulabilir (kısmi iade senaryosu).
    /// </summary>
    public class RefundRequest : BaseEntity
    {
        // ═══════════════════════════════════════════════════════════════════════
        // SİPARİŞ İLİŞKİSİ
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İade talebi yapılan sipariş ID'si.
        /// Foreign key: Orders tablosu.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// İade talebini oluşturan kullanıcı ID'si.
        /// Misafir siparişlerde null olabilir.
        /// </summary>
        public int? UserId { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // İADE DETAYLARI
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İade talebi durumu.
        /// Pending → Approved → Refunded (başarılı akış)
        /// Pending → Rejected (reddedildi)
        /// Pending → AutoCancelled (kargo çıkmadan otomatik iptal)
        /// </summary>
        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Pending;

        /// <summary>
        /// Müşterinin belirttiği iade sebebi.
        /// UI'da zorunlu alan olarak istenir.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// İade talep edilen tutar (TL).
        /// Tam iade: Sipariş toplam tutarı.
        /// Kısmi iade: Müşterinin talep ettiği tutar (admin onayına tabi).
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// İade türü.
        /// "full" = Tam iade, "partial" = Kısmi iade.
        /// </summary>
        public string RefundType { get; set; } = "full";

        // ═══════════════════════════════════════════════════════════════════════
        // TALEBİN OLUŞTURULMA BAĞLAMI
        // İade talebi oluşturulduğundaki sipariş durumu kaydedilir.
        // NEDEN: Admin kararını destekleyecek ek bilgi sağlar.
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İade talebi oluşturulduğundaki sipariş durumu.
        /// Admin'e bağlam sağlar: "Kargo yoldayken mi talep etti?"
        /// </summary>
        public string OrderStatusAtRequest { get; set; } = string.Empty;

        /// <summary>
        /// İade talebi oluşturulma tarihi.
        /// BaseEntity.CreatedAt ile aynı değeri taşır, sorgu kolaylığı için ayrı alan.
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // ═══════════════════════════════════════════════════════════════════════
        // ADMİN İŞLEM BİLGİLERİ
        // Talebi onaylayan/reddeden admin bilgileri.
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Talebi işleyen admin kullanıcı ID'si.
        /// Onay veya ret kararını veren kişi.
        /// Otomatik iptal durumunda null kalır (sistem işlemi).
        /// </summary>
        public int? ProcessedByUserId { get; set; }

        /// <summary>
        /// Talebin işlendiği tarih.
        /// Admin onayladığında veya sistem otomatik iptal ettiğinde set edilir.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Admin'in eklediği not.
        /// Onay sebebi, ret açıklaması veya müşteriye iletilen mesaj.
        /// </summary>
        public string? AdminNote { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // ÖDEME İADE BİLGİLERİ (POSNET)
        // Banka tarafında gerçekleşen işlem detayları.
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET işlem referans numarası (HostLogKey).
        /// İade/iptal işlemi sonrası bankadan dönen referans.
        /// Mutabakat ve takip için saklanır.
        /// </summary>
        public string? PosnetHostLogKey { get; set; }

        /// <summary>
        /// Yapılan işlem tipi: "reverse" veya "return".
        /// Reverse: Aynı gün iptal (ekstre'ye yansımaz).
        /// Return: Farklı gün iade (ekstre'de görünür).
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// Para iadesinin banka tarafında tamamlandığı tarih.
        /// POSNET'ten başarılı yanıt alındığında set edilir.
        /// </summary>
        public DateTime? RefundedAt { get; set; }

        /// <summary>
        /// Para iadesi başarısız olduysa hata açıklaması.
        /// POSNET hata kodu ve mesajı birlikte saklanır.
        /// Örnek: "0211 - GRUP KAPAMA YAPILMIŞ (Reverse yapılamaz, return denenecek)"
        /// </summary>
        public string? RefundFailureReason { get; set; }

        // ═══════════════════════════════════════════════════════════════════════
        // NAVİGASYON PROPERTİES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İlişkili sipariş navigasyon property.
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// İade talebini oluşturan kullanıcı.
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Talebi işleyen admin kullanıcı.
        /// </summary>
        public virtual User? ProcessedByUser { get; set; }
    }
}
