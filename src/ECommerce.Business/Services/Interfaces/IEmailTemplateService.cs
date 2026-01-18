// ==========================================================================
// IEmailTemplateService.cs - Email Template Servisi Interface
// ==========================================================================
// Teslimat süreçleri için profesyonel email template'leri oluşturur.
// Kurye atama, teslimat durumu, başarısız teslimat bildirimleri için
// HTML ve text formatında email içerikleri sağlar.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Email template servisi interface.
    /// Teslimat bildirimleri için profesyonel email şablonları sağlar.
    /// </summary>
    public interface IEmailTemplateService
    {
        #region Müşteri Email Template'leri

        /// <summary>
        /// Kurye atandı bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetCourierAssignedTemplateAsync(CourierAssignedEmailData data);

        /// <summary>
        /// Kurye yola çıktı bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetCourierEnRouteTemplateAsync(CourierEnRouteEmailData data);

        /// <summary>
        /// Teslimat tamamlandı bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetDeliveryCompletedTemplateAsync(DeliveryCompletedEmailData data);

        /// <summary>
        /// Teslimat başarısız bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetDeliveryFailedTemplateAsync(DeliveryFailedEmailData data);

        /// <summary>
        /// Teslimat yeniden programlandı bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetDeliveryRescheduledTemplateAsync(DeliveryRescheduledEmailData data);

        #endregion

        #region Kurye Email Template'leri

        /// <summary>
        /// Kuryeye yeni görev atandı bildirimi email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetNewTaskAssignedToCourrierTemplateAsync(NewTaskEmailData data);

        /// <summary>
        /// Kurye günlük özet raporu email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetCourierDailySummaryTemplateAsync(CourierDailySummaryData data);

        #endregion

        #region Admin Email Template'leri

        /// <summary>
        /// Admin için başarısız teslimat uyarı email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetAdminDeliveryAlertTemplateAsync(AdminDeliveryAlertData data);

        /// <summary>
        /// Admin günlük teslimat raporu email'i oluşturur
        /// </summary>
        Task<EmailTemplate> GetAdminDailyReportTemplateAsync(AdminDailyReportData data);

        #endregion
    }

    #region Email Template Modelleri

    /// <summary>
    /// Oluşturulan email template sonucu
    /// </summary>
    public class EmailTemplate
    {
        /// <summary>
        /// Email konusu
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML formatında email gövdesi
        /// </summary>
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>
        /// Plain text formatında email gövdesi (HTML desteklemeyen istemciler için)
        /// </summary>
        public string TextBody { get; set; } = string.Empty;

        /// <summary>
        /// Template tipi
        /// </summary>
        public EmailTemplateType TemplateType { get; set; }
    }

    /// <summary>
    /// Email template tipleri
    /// </summary>
    public enum EmailTemplateType
    {
        CourierAssigned,
        CourierEnRoute,
        DeliveryCompleted,
        DeliveryFailed,
        DeliveryRescheduled,
        NewTaskAssigned,
        CourierDailySummary,
        AdminDeliveryAlert,
        AdminDailyReport
    }

    #endregion

    #region Müşteri Email Data Modelleri

    /// <summary>
    /// Kurye atandı email verileri
    /// </summary>
    public class CourierAssignedEmailData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public string CourierPhone { get; set; } = string.Empty;
        public DateTime EstimatedDeliveryTime { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string TrackingUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kurye yola çıktı email verileri
    /// </summary>
    public class CourierEnRouteEmailData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; }
        public string TrackingUrl { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Teslimat tamamlandı email verileri
    /// </summary>
    public class DeliveryCompletedEmailData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ProofOfDeliveryUrl { get; set; }
        public string? SignatureUrl { get; set; }
        public string RatingUrl { get; set; } = string.Empty;
        public List<OrderItemSummary> OrderItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Teslimat başarısız email verileri
    /// </summary>
    public class DeliveryFailedEmailData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
        public string SupportPhone { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public string RescheduleUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Teslimat yeniden programlandı email verileri
    /// </summary>
    public class DeliveryRescheduledEmailData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime NewDeliveryDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string TrackingUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sipariş ürün özeti
    /// </summary>
    public class OrderItemSummary
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    #endregion

    #region Kurye Email Data Modelleri

    /// <summary>
    /// Kuryeye yeni görev email verileri
    /// </summary>
    public class NewTaskEmailData
    {
        public string CourierName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime PickupDeadline { get; set; }
        public DateTime DeliveryDeadline { get; set; }
        public string? SpecialInstructions { get; set; }
        public int PackageCount { get; set; }
        public decimal? TotalWeight { get; set; }
    }

    /// <summary>
    /// Kurye günlük özet email verileri
    /// </summary>
    public class CourierDailySummaryData
    {
        public string CourierName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public decimal TotalDistance { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
        public decimal AverageRating { get; set; }
        public decimal EarningsToday { get; set; }
    }

    #endregion

    #region Admin Email Data Modelleri

    /// <summary>
    /// Admin teslimat uyarı email verileri
    /// </summary>
    public class AdminDeliveryAlertData
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin günlük rapor email verileri
    /// </summary>
    public class AdminDailyReportData
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int FailedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageDeliveryTime { get; set; }
        public int ActiveCouriers { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<TopCourierSummary> TopCouriers { get; set; } = new();
        public List<FailureReasonSummary> FailureReasons { get; set; } = new();
    }

    /// <summary>
    /// En iyi kurye özeti
    /// </summary>
    public class TopCourierSummary
    {
        public string CourierName { get; set; } = string.Empty;
        public int DeliveryCount { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageRating { get; set; }
    }

    /// <summary>
    /// Başarısızlık nedeni özeti
    /// </summary>
    public class FailureReasonSummary
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    #endregion
}
