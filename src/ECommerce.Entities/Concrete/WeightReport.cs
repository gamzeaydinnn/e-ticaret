using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Tartı cihazından gelen ağırlık raporu
    /// </summary>
    public class WeightReport : BaseEntity
    {
        /// <summary>
        /// Tartı cihazından gelen benzersiz rapor ID (idempotency için)
        /// </summary>
        public string ExternalReportId { get; set; } = string.Empty;

        /// <summary>
        /// İlişkili sipariş ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// İlişkili sipariş kalemi ID (opsiyonel - paket bazlı rapor için null olabilir)
        /// </summary>
        public int? OrderItemId { get; set; }

        /// <summary>
        /// Beklenen ağırlık (gram)
        /// </summary>
        public int ExpectedWeightGrams { get; set; }

        /// <summary>
        /// Tartılan gerçek ağırlık (gram)
        /// </summary>
        public int ReportedWeightGrams { get; set; }

        /// <summary>
        /// Fazla ağırlık (gram) = ReportedWeightGrams - ExpectedWeightGrams
        /// </summary>
        public int OverageGrams { get; set; }

        /// <summary>
        /// Fazla ağırlık tutarı (parasal)
        /// </summary>
        public decimal OverageAmount { get; set; }

        /// <summary>
        /// Para birimi (TRY, USD vb.)
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Rapor durumu
        /// </summary>
        public WeightReportStatus Status { get; set; } = WeightReportStatus.Pending;

        /// <summary>
        /// Tartı cihazı kaynak bilgisi
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Rapor alınma zamanı
        /// </summary>
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// İşlenme zamanı
        /// </summary>
        public DateTimeOffset? ProcessedAt { get; set; }

        /// <summary>
        /// Cihazdan gelen ham JSON metadata
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Yönetici notu
        /// </summary>
        public string? AdminNote { get; set; }

        /// <summary>
        /// Kurye notu
        /// </summary>
        public string? CourierNote { get; set; }

        /// <summary>
        /// Ödeme işlem referans ID
        /// </summary>
        public string? PaymentAttemptId { get; set; }

        /// <summary>
        /// İşlemi onaylayan yönetici ID
        /// </summary>
        public int? ApprovedByUserId { get; set; }

        /// <summary>
        /// Onay/Red zamanı
        /// </summary>
        public DateTimeOffset? ApprovedAt { get; set; }

        // Navigation Properties
        public virtual Order? Order { get; set; }
        public virtual OrderItem? OrderItem { get; set; }
        public virtual User? ApprovedBy { get; set; }
    }

    /// <summary>
    /// Ağırlık raporu durumları
    /// </summary>
    public enum WeightReportStatus
    {
        /// <summary>
        /// Beklemede - Yönetici onayı bekleniyor
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Onaylandı - Yönetici tarafından onaylandı
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Reddedildi - Yönetici tarafından reddedildi
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Tahsil edildi - Ödeme başarıyla alındı
        /// </summary>
        Charged = 3,

        /// <summary>
        /// Başarısız - Ödeme tahsilatı başarısız
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Otomatik onaylandı - Eşik değerin altında, otomatik işlendi
        /// </summary>
        AutoApproved = 5
    }
}
