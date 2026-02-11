// ==========================================================================
// RefundRequestDto.cs - İade Talebi DTO'ları
// ==========================================================================
// İade talebi oluşturma, listeleme ve admin işlem DTO'ları.
// Controller ↔ Service katmanı arasında veri taşıma nesneleri.
// ==========================================================================

using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Order
{
    // ═══════════════════════════════════════════════════════════════════════════
    // MÜŞTERİ İADE TALEBİ OLUŞTURMA DTO
    // Frontend'den gelen iade talebi verisi
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Müşteri iade talebi oluşturma DTO'su.
    /// POST /api/orders/{orderId}/refund-request body'si.
    /// </summary>
    public class CreateRefundRequestDto
    {
        /// <summary>
        /// İade sebebi (zorunlu).
        /// Müşteriden UI'da açık metin olarak alınır.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// İade türü: "full" veya "partial".
        /// Varsayılan: "full" (tam iade).
        /// </summary>
        public string RefundType { get; set; } = "full";

        /// <summary>
        /// İade talep edilen tutar (kısmi iade için).
        /// Tam iade durumunda null gönderilir, sistem sipariş tutarını kullanır.
        /// </summary>
        public decimal? RefundAmount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ADMİN İADE İŞLEM DTO
    // Admin onay/ret kararı verisi
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Admin iade onay/ret DTO'su.
    /// POST /api/admin/orders/{orderId}/process-refund body'si.
    /// </summary>
    public class ProcessRefundDto
    {
        /// <summary>
        /// Admin kararı: true = onayla, false = reddet.
        /// </summary>
        public bool Approve { get; set; }

        /// <summary>
        /// Admin notu (opsiyonel).
        /// Onay sebebi veya ret açıklaması.
        /// </summary>
        public string? AdminNote { get; set; }

        /// <summary>
        /// İade tutarı (admin tarafından ayarlanabilir).
        /// Null ise talepdeki tutar kullanılır.
        /// </summary>
        public decimal? RefundAmount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // İADE TALEBİ LİSTELEME DTO
    // Admin paneli ve müşteri sipariş detayı için
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// İade talebi görüntüleme DTO'su.
    /// Admin paneli listesi ve müşteri sipariş detayları için kullanılır.
    /// </summary>
    public class RefundRequestListDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public int? UserId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        // İade detayları
        public RefundRequestStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string RefundType { get; set; } = "full";
        public string OrderStatusAtRequest { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }

        // Admin işlem bilgileri
        public int? ProcessedByUserId { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? AdminNote { get; set; }

        // POSNET bilgileri
        public string? PosnetHostLogKey { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundFailureReason { get; set; }

        // Sipariş özet bilgileri (admin listesi için)
        public decimal OrderTotalPrice { get; set; }
        public string? OrderStatus { get; set; }
    }
}
