using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kurye "Yola Çıktım" isteği DTO.
    /// ASSIGNED → OUT_FOR_DELIVERY geçişi için kullanılır.
    /// </summary>
    public class StartDeliveryDto
    {
        /// <summary>
        /// Kurye notu (opsiyonel)
        /// </summary>
        [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }

        /// <summary>
        /// Kurye konum bilgisi (opsiyonel, tracking için)
        /// </summary>
        public string? CurrentLocation { get; set; }
    }

    /// <summary>
    /// Kurye "Teslim Ettim" isteği DTO.
    /// OUT_FOR_DELIVERY → DELIVERED geçişi için kullanılır.
    /// </summary>
    public class MarkDeliveredDto
    {
        /// <summary>
        /// Teslim alan kişinin adı (opsiyonel)
        /// Müşteri evde değilse komşu/kapıcı adı
        /// </summary>
        [MaxLength(100, ErrorMessage = "İsim en fazla 100 karakter olabilir.")]
        public string? ReceiverName { get; set; }

        /// <summary>
        /// Teslim notu (opsiyonel)
        /// "Kapıda bırakıldı", "Komşuya teslim" vs.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        public string? Note { get; set; }

        /// <summary>
        /// Teslim fotoğrafı URL (opsiyonel, proof of delivery)
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Ağırlık farkı (gram cinsinden, opsiyonel)
        /// Mikro API yoksa kurye manuel girebilir
        /// Pozitif: Fazla geldi, Negatif: Eksik geldi
        /// </summary>
        public int? WeightAdjustmentGrams { get; set; }

        /// <summary>
        /// Kapıda ödeme tahsil edildi mi?
        /// Kapıda ödeme siparişlerinde zorunlu
        /// </summary>
        public bool? CashCollected { get; set; }

        /// <summary>
        /// Tahsil edilen tutar (kapıda ödeme için)
        /// </summary>
        public decimal? CollectedAmount { get; set; }
    }

    /// <summary>
    /// Kurye "Sorun Var" isteği DTO.
    /// Herhangi bir durum → DELIVERY_FAILED geçişi için kullanılır.
    /// </summary>
    public class ReportProblemDto
    {
        /// <summary>
        /// Problem sebebi (zorunlu)
        /// </summary>
        [Required(ErrorMessage = "Problem sebebi seçilmelidir.")]
        public DeliveryProblemReason Reason { get; set; }

        /// <summary>
        /// Detaylı açıklama (Reason=Other ise zorunlu)
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string? Description { get; set; }

        /// <summary>
        /// Problem fotoğrafı URL (opsiyonel)
        /// Hasarlı paket, kapalı kapı vs.
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Kurye konum bilgisi (problem anında)
        /// </summary>
        public string? CurrentLocation { get; set; }

        /// <summary>
        /// Müşteriye ulaşmaya çalışıldı mı?
        /// </summary>
        public bool AttemptedToContactCustomer { get; set; }

        /// <summary>
        /// Kaç kez arama yapıldı?
        /// </summary>
        public int? CallAttempts { get; set; }
    }

    /// <summary>
    /// Kurye sipariş işlemi yanıt DTO.
    /// </summary>
    public class CourierOrderActionResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Sonuç mesajı (Türkçe)
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Yeni sipariş durumu
        /// </summary>
        public string? NewStatus { get; set; }

        /// <summary>
        /// Yeni durum Türkçe açıklama
        /// </summary>
        public string? NewStatusText { get; set; }

        /// <summary>
        /// İşlem zamanı
        /// </summary>
        public DateTime ActionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ödeme capture sonucu (teslim işleminde)
        /// </summary>
        public PaymentCaptureInfo? PaymentInfo { get; set; }
    }

    /// <summary>
    /// Ödeme capture bilgisi
    /// </summary>
    public class PaymentCaptureInfo
    {
        /// <summary>
        /// Capture başarılı mı?
        /// </summary>
        public bool CaptureSuccess { get; set; }

        /// <summary>
        /// Çekilen tutar
        /// </summary>
        public decimal CapturedAmount { get; set; }

        /// <summary>
        /// Ek tutar (tartı farkı vs.)
        /// </summary>
        public decimal? AdditionalAmount { get; set; }

        /// <summary>
        /// Capture mesajı
        /// </summary>
        public string? CaptureMessage { get; set; }

        /// <summary>
        /// Final > Authorized durumu oluştu mu?
        /// Bu durumda admin müdahalesi gerekir
        /// </summary>
        public bool RequiresAdminAction { get; set; }
    }

    /// <summary>
    /// Kurye sipariş listesi filtre DTO.
    /// </summary>
    public class CourierOrderFilterDto
    {
        /// <summary>
        /// Durum filtresi (opsiyonel)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Öncelik filtresi (opsiyonel)
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Tarih filtresi - Başlangıç
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Tarih filtresi - Bitiş
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Sayfa numarası (1'den başlar)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Sayfa başına kayıt
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Kurye sipariş listesi yanıt DTO.
    /// </summary>
    public class CourierOrderListResponseDto
    {
        /// <summary>
        /// Sipariş listesi
        /// </summary>
        public System.Collections.Generic.List<CourierOrderListDto> Orders { get; set; } = new();

        /// <summary>
        /// Toplam kayıt sayısı
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Mevcut sayfa
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Toplam sayfa sayısı
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Özet istatistikler
        /// </summary>
        public CourierOrderSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// Kurye sipariş özet istatistikleri
    /// </summary>
    public class CourierOrderSummaryDto
    {
        /// <summary>
        /// Bugün teslim edilen sipariş sayısı
        /// </summary>
        public int TodayDelivered { get; set; }

        /// <summary>
        /// Aktif (yoldaki) sipariş sayısı
        /// </summary>
        public int ActiveOrders { get; set; }

        /// <summary>
        /// Bekleyen (atanmış ama başlanmamış) sipariş sayısı
        /// </summary>
        public int PendingOrders { get; set; }

        /// <summary>
        /// Bugün problem bildirilen sipariş sayısı
        /// </summary>
        public int TodayFailed { get; set; }

        /// <summary>
        /// Toplam kazanç (bugün)
        /// </summary>
        public decimal TodayEarnings { get; set; }
    }
}
