using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Weight
{
    /// <summary>
    /// Tartı cihazından gelen ağırlık raporu DTO
    /// </summary>
    public class MicroWeightReportDto
    {
        /// <summary>
        /// Tartı cihazından gelen benzersiz rapor ID (idempotency için)
        /// </summary>
        [Required]
        public string ReportId { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş numarası veya ID
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        /// <summary>
        /// Sipariş kalemi ID (opsiyonel - paket bazlı ise null)
        /// </summary>
        public int? OrderItemId { get; set; }

        /// <summary>
        /// Tartılan ağırlık (gram)
        /// </summary>
        [Required]
        [Range(1, 1000000)]
        public int ReportedWeightGrams { get; set; }

        /// <summary>
        /// Rapor zaman damgası
        /// </summary>
        [Required]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Tartı cihazı kaynak bilgisi
        /// </summary>
        [Required]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Ek meta veriler (JSON string)
        /// </summary>
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Ağırlık raporu yanıt DTO
    /// </summary>
    public class WeightReportResponseDto
    {
        public int Id { get; set; }
        public string ExternalReportId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int ExpectedWeightGrams { get; set; }
        public int ReportedWeightGrams { get; set; }
        public int OverageGrams { get; set; }
        public decimal OverageAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTimeOffset ReceivedAt { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }
        public string? AdminNote { get; set; }
        public string? CourierNote { get; set; }
    }

    /// <summary>
    /// Ağırlık raporu onay/red DTO
    /// </summary>
    public class WeightReportActionDto
    {
        [Required]
        public int ReportId { get; set; }

        public string? Note { get; set; }
    }
}
