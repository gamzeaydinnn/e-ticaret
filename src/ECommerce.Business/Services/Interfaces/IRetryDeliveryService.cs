// ==========================================================================
// IRetryDeliveryService.cs - Yeniden Teslimat Deneme Servis Interface'i
// ==========================================================================
// Bu interface, başarısız teslimatlar sonrası yeniden deneme işlemlerini yönetir.
// Ayrıca iade görevleri oluşturma işlevselliği de sağlar.
//
// İş Mantığı:
// - Başarısız teslimat sonrası yeniden deneme penceresi açılır
// - Maksimum deneme sayısına ulaşılırsa iade görevi oluşturulur
// - Müşteri onayı ile yeniden deneme planlanabilir
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Yeniden teslimat deneme servis interface'i.
    /// </summary>
    public interface IRetryDeliveryService
    {
        /// <summary>
        /// Başarısız teslimat için yeniden deneme planlar.
        /// </summary>
        /// <param name="deliveryTaskId">Başarısız teslimat görevi ID'si</param>
        /// <param name="request">Yeniden deneme isteği</param>
        /// <returns>Yeni oluşturulan teslimat görevi</returns>
        Task<RetryDeliveryResult> ScheduleRetryAsync(int deliveryTaskId, RetryDeliveryRequest request);

        /// <summary>
        /// Başarısız teslimat için iade görevi oluşturur.
        /// </summary>
        /// <param name="deliveryTaskId">Başarısız teslimat görevi ID'si</param>
        /// <param name="reason">İade sebebi</param>
        /// <returns>Oluşturulan iade görevi</returns>
        Task<DeliveryTask?> CreateReturnTaskAsync(int deliveryTaskId, string reason);

        /// <summary>
        /// Teslimat yeniden denenebilir mi kontrol eder.
        /// </summary>
        /// <param name="deliveryTaskId">Teslimat görevi ID'si</param>
        /// <returns>Denenebilir mi ve sebep</returns>
        Task<(bool canRetry, string? reason, int remainingAttempts)> CanRetryDeliveryAsync(int deliveryTaskId);

        /// <summary>
        /// Yeniden deneme bekleyen teslimatları getirir.
        /// </summary>
        /// <returns>Bekleyen teslimat listesi</returns>
        Task<IEnumerable<DeliveryTask>> GetPendingRetriesAsync();

        /// <summary>
        /// Belirli teslimatın deneme geçmişini getirir.
        /// </summary>
        /// <param name="orderId">Sipariş ID'si</param>
        /// <returns>Deneme geçmişi</returns>
        Task<IEnumerable<DeliveryAttemptInfo>> GetDeliveryAttemptsAsync(int orderId);

        /// <summary>
        /// Yeniden deneme işlemini iptal eder.
        /// </summary>
        /// <param name="deliveryTaskId">Teslimat görevi ID'si</param>
        /// <param name="reason">İptal sebebi</param>
        /// <returns>Başarılı mı?</returns>
        Task<bool> CancelRetryAsync(int deliveryTaskId, string reason);

        /// <summary>
        /// Otomatik yeniden deneme zamanı gelmiş teslimatları işler.
        /// (Background job tarafından çağrılır)
        /// </summary>
        /// <returns>İşlenen teslimat sayısı</returns>
        Task<int> ProcessScheduledRetriesAsync();
    }

    // =========================================================================
    // DTO'LAR
    // =========================================================================

    /// <summary>
    /// Yeniden deneme isteği.
    /// </summary>
    public class RetryDeliveryRequest
    {
        /// <summary>
        /// Yeniden deneme zamanı.
        /// Null ise hemen denenecek.
        /// </summary>
        public DateTime? ScheduledTime { get; set; }

        /// <summary>
        /// Alternatif adres (isteğe bağlı).
        /// </summary>
        public string? AlternativeAddress { get; set; }

        /// <summary>
        /// Alternatif telefon (isteğe bağlı).
        /// </summary>
        public string? AlternativePhone { get; set; }

        /// <summary>
        /// Kuryeye özel not.
        /// </summary>
        public string? CourierNotes { get; set; }

        /// <summary>
        /// Öncelik değişikliği.
        /// </summary>
        public string? NewPriority { get; set; }

        /// <summary>
        /// Aynı kuryeye mi atansın?
        /// </summary>
        public bool AssignSameCourier { get; set; } = false;

        /// <summary>
        /// Müşteri onayı alındı mı?
        /// </summary>
        public bool CustomerConfirmed { get; set; } = false;

        /// <summary>
        /// İşlemi yapan kullanıcı ID'si.
        /// </summary>
        public int? RequestedByUserId { get; set; }
    }

    /// <summary>
    /// Yeniden deneme sonucu.
    /// </summary>
    public class RetryDeliveryResult
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Sonuç mesajı.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Yeni oluşturulan teslimat görevi (varsa).
        /// </summary>
        public DeliveryTask? NewDeliveryTask { get; set; }

        /// <summary>
        /// Planlanan yeniden deneme zamanı.
        /// </summary>
        public DateTime? ScheduledRetryTime { get; set; }

        /// <summary>
        /// Deneme sayısı.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Kalan deneme hakkı.
        /// </summary>
        public int RemainingAttempts { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise).
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Teslimat deneme bilgisi.
    /// </summary>
    public class DeliveryAttemptInfo
    {
        /// <summary>
        /// Teslimat görevi ID'si.
        /// </summary>
        public int DeliveryTaskId { get; set; }

        /// <summary>
        /// Deneme numarası.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Deneme zamanı.
        /// </summary>
        public DateTime AttemptTime { get; set; }

        /// <summary>
        /// Sonuç durumu.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Başarısızlık sebebi (varsa).
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Atanan kurye ID'si.
        /// </summary>
        public int? CourierId { get; set; }

        /// <summary>
        /// Kurye adı.
        /// </summary>
        public string? CourierName { get; set; }
    }
}
