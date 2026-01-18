// ==========================================================================
// IRealTimeNotificationService.cs - Real-Time Bildirim Servisi Interface
// ==========================================================================
// Manager'lardan SignalR hub'larına bildirim göndermek için kullanılır.
// IHubContext injection pattern ile hub'lara erişir.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Real-time bildirim servisi interface.
    /// Manager'lardan SignalR hub'larına mesaj göndermek için kullanılır.
    /// </summary>
    public interface IRealTimeNotificationService
    {
        // =====================================================================
        // KURYE BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Kuryeye yeni görev atandığını bildirir.
        /// </summary>
        Task NotifyCourierNewTaskAsync(int courierId, CourierTaskNotification notification);
        
        /// <summary>
        /// Kuryeye görev iptal edildiğini bildirir.
        /// </summary>
        Task NotifyCourierTaskCancelledAsync(int courierId, int deliveryTaskId, string reason);
        
        /// <summary>
        /// Kuryeye SLA uyarısı gönderir.
        /// </summary>
        Task NotifyCourierSlaWarningAsync(int courierId, int deliveryTaskId, int minutesRemaining);
        
        /// <summary>
        /// Kuryenin durumunu günceller ve ilgili taraflara bildirir.
        /// </summary>
        Task NotifyCourierStatusChangedAsync(int courierId, string status);

        // =====================================================================
        // ADMİN BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Adminlere dashboard metriklerini gönderir.
        /// </summary>
        Task NotifyAdminsDashboardUpdateAsync(DashboardMetricsNotification metrics);
        
        /// <summary>
        /// Adminlere SLA ihlali uyarısı gönderir.
        /// </summary>
        Task NotifyAdminsSlaViolationAsync(int deliveryTaskId, string orderNumber, DateTime deadline);
        
        /// <summary>
        /// Adminlere takılmış teslimat uyarısı gönderir.
        /// </summary>
        Task NotifyAdminsDeliveryStuckAsync(int deliveryTaskId, string orderNumber, int stuckMinutes);
        
        /// <summary>
        /// Adminlere kurye offline uyarısı gönderir.
        /// </summary>
        Task NotifyAdminsCourierOfflineAsync(int courierId, string courierName, int activeTaskCount);

        // =====================================================================
        // TESLİMAT TAKİP BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Teslimat durumu değişikliğini izleyenlere bildirir.
        /// </summary>
        Task NotifyDeliveryStatusChangedAsync(int deliveryTaskId, string status, string message);
        
        /// <summary>
        /// Kurye konumunu izleyenlere bildirir.
        /// </summary>
        Task NotifyCourierLocationUpdatedAsync(int deliveryTaskId, double latitude, double longitude, double? estimatedMinutes);
        
        /// <summary>
        /// Teslimat tamamlandığını bildirir.
        /// </summary>
        Task NotifyDeliveryCompletedAsync(int deliveryTaskId, string orderNumber, DateTime completedAt);

        // =====================================================================
        // ZONE BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Belirli bir bölgedeki tüm kuryeları bilgilendirir.
        /// </summary>
        Task NotifyZoneCouriersAsync(int zoneId, string message);
    }

    /// <summary>
    /// Kuryeye gönderilecek görev bildirimi.
    /// </summary>
    public class CourierTaskNotification
    {
        public int DeliveryTaskId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
        public double? DropoffLatitude { get; set; }
        public double? DropoffLongitude { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool IsCod { get; set; }
        public decimal? CodAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime? TimeWindowStart { get; set; }
        public DateTime? TimeWindowEnd { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Dashboard metrikleri bildirimi.
    /// </summary>
    public class DashboardMetricsNotification
    {
        public int ActiveDeliveries { get; set; }
        public int PendingDeliveries { get; set; }
        public int OnlinesCouriers { get; set; }
        public int OfflineCouriers { get; set; }
        public int SlaCritical { get; set; }
        public int SlaWarning { get; set; }
        public double AverageDeliveryTime { get; set; }
        public int CompletedToday { get; set; }
        public int FailedToday { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
