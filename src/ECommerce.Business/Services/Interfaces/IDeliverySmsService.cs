// ==========================================================================
// IDeliverySmsService.cs - Teslimat SMS Servisi Interface
// ==========================================================================
// Kurye teslimat süreçleri için SMS bildirim servisi.
// Müşteriye kurye yola çıktı, yaklaşıyor, teslimat OTP gibi
// kritik bildirimleri gönderir.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat süreçleri için SMS bildirim servisi interface.
    /// Müşteri ve kurye arasındaki SMS iletişimini yönetir.
    /// </summary>
    public interface IDeliverySmsService
    {
        #region Müşteri SMS Bildirimleri

        /// <summary>
        /// Kurye atandığında müşteriye bildirim gönderir
        /// </summary>
        Task<SmsResult> SendCourierAssignedSmsAsync(CourierAssignedSmsData data);

        /// <summary>
        /// Kurye yola çıktığında müşteriye bildirim gönderir
        /// </summary>
        Task<SmsResult> SendCourierEnRouteSmsAsync(CourierEnRouteSmsData data);

        /// <summary>
        /// Kurye yaklaştığında müşteriye bildirim gönderir (10 dk kala)
        /// </summary>
        Task<SmsResult> SendCourierApproachingSmsAsync(CourierApproachingSmsData data);

        /// <summary>
        /// Teslimat OTP kodunu müşteriye gönderir
        /// </summary>
        Task<SmsResult> SendDeliveryOtpSmsAsync(DeliveryOtpSmsData data);

        /// <summary>
        /// Teslimat tamamlandığında müşteriye onay SMS'i gönderir
        /// </summary>
        Task<SmsResult> SendDeliveryCompletedSmsAsync(DeliveryCompletedSmsData data);

        /// <summary>
        /// Teslimat başarısız olduğunda müşteriye bilgi SMS'i gönderir
        /// </summary>
        Task<SmsResult> SendDeliveryFailedSmsAsync(DeliveryFailedSmsData data);

        /// <summary>
        /// Teslimat yeniden planlandığında müşteriye bilgi SMS'i gönderir
        /// </summary>
        Task<SmsResult> SendDeliveryRescheduledSmsAsync(DeliveryRescheduledSmsData data);

        #endregion

        #region Kurye SMS Bildirimleri

        /// <summary>
        /// Kuryeye yeni görev atandığında SMS gönderir
        /// </summary>
        Task<SmsResult> SendNewTaskToCourrierSmsAsync(NewTaskSmsData data);

        /// <summary>
        /// Kuryeye acil uyarı SMS'i gönderir (SLA aşımı vb.)
        /// </summary>
        Task<SmsResult> SendUrgentAlertToCourrierSmsAsync(UrgentAlertSmsData data);

        #endregion

        #region Toplu SMS

        /// <summary>
        /// Belirli bir bölgedeki tüm kuryeye yayın SMS'i gönderir
        /// </summary>
        Task<int> BroadcastToZoneCouriersSmsAsync(string zoneId, string message);

        #endregion
    }

    #region SMS Result

    /// <summary>
    /// SMS gönderim sonucu
    /// </summary>
    public class SmsResult
    {
        /// <summary>
        /// Gönderim başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// SMS sağlayıcısından dönen mesaj ID
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Hata durumunda hata mesajı
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// SMS ücreti (birim cinsinden)
        /// </summary>
        public decimal? Cost { get; set; }

        /// <summary>
        /// SMS gönderim zamanı
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        public static SmsResult Success(string? messageId = null)
        {
            return new SmsResult
            {
                IsSuccess = true,
                MessageId = messageId,
                SentAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Hatalı sonuç oluşturur
        /// </summary>
        public static SmsResult Failed(string errorMessage)
        {
            return new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                SentAt = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region Müşteri SMS Data Modelleri

    /// <summary>
    /// Kurye atandı SMS verileri
    /// </summary>
    public class CourierAssignedSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public DateTime EstimatedDeliveryTime { get; set; }
        public string? TrackingUrl { get; set; }
    }

    /// <summary>
    /// Kurye yola çıktı SMS verileri
    /// </summary>
    public class CourierEnRouteSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; }
        public string? TrackingUrl { get; set; }
    }

    /// <summary>
    /// Kurye yaklaşıyor SMS verileri
    /// </summary>
    public class CourierApproachingSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CourierName { get; set; } = string.Empty;
        public int RemainingMinutes { get; set; }
    }

    /// <summary>
    /// Teslimat OTP SMS verileri
    /// </summary>
    public class DeliveryOtpSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public int ValidityMinutes { get; set; } = 10;
    }

    /// <summary>
    /// Teslimat tamamlandı SMS verileri
    /// </summary>
    public class DeliveryCompletedSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public string? RatingUrl { get; set; }
    }

    /// <summary>
    /// Teslimat başarısız SMS verileri
    /// </summary>
    public class DeliveryFailedSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string SupportPhone { get; set; } = string.Empty;
    }

    /// <summary>
    /// Teslimat yeniden planlandı SMS verileri
    /// </summary>
    public class DeliveryRescheduledSmsData
    {
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime NewDeliveryDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
    }

    #endregion

    #region Kurye SMS Data Modelleri

    /// <summary>
    /// Yeni görev SMS verileri
    /// </summary>
    public class NewTaskSmsData
    {
        public string CourierPhone { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public DateTime DeliveryDeadline { get; set; }
    }

    /// <summary>
    /// Acil uyarı SMS verileri
    /// </summary>
    public class UrgentAlertSmsData
    {
        public string CourierPhone { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string AlertMessage { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
    }

    #endregion
}
