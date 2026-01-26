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
        // MÜŞTERİ SİPARİŞ TAKİP BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Sipariş durumu değiştiğinde müşteriye bildirim gönderir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="newStatus">Yeni durum kodu</param>
        /// <param name="statusText">Durum açıklaması (Türkçe)</param>
        /// <param name="estimatedDelivery">Tahmini teslimat zamanı</param>
        Task NotifyOrderStatusChangedAsync(int orderId, string orderNumber, string newStatus, 
            string statusText, DateTime? estimatedDelivery = null);
        
        /// <summary>
        /// Teslimat tamamlandığında müşteriye bildirim gönderir.
        /// </summary>
        Task NotifyCustomerDeliveryCompletedAsync(int orderId, string orderNumber, DateTime deliveredAt, string? signedBy = null);
        
        /// <summary>
        /// Teslimat sorunu müşteriye bildirilir.
        /// </summary>
        Task NotifyCustomerDeliveryProblemAsync(int orderId, string problemType, string message);

        // =====================================================================
        // KURYE BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Kuryeye yeni görev atandığını bildirir.
        /// </summary>
        Task NotifyCourierNewTaskAsync(int courierId, CourierTaskNotification notification);
        
        /// <summary>
        /// Kuryeye yeni sipariş atandığını bildirir (basit versiyon).
        /// </summary>
        Task NotifyOrderAssignedToCourierAsync(int courierId, int orderId, string orderNumber, 
            string deliveryAddress, string? customerPhone, decimal totalAmount, string paymentMethod);
        
        /// <summary>
        /// Kuryeden sipariş ataması kaldırıldığını bildirir.
        /// </summary>
        Task NotifyOrderUnassignedFromCourierAsync(int courierId, int orderId, string orderNumber, string reason);
        
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
        
        /// <summary>
        /// Kuryeye sipariş güncelleme bildirimi gönderir.
        /// </summary>
        Task NotifyOrderUpdatedForCourierAsync(int courierId, int orderId, string updateType, string details);
        
        /// <summary>
        /// Kuryeye mesaj gönderir (admin'den).
        /// </summary>
        Task SendMessageToCourierAsync(int courierId, string message, string priority = "normal");
        
        /// <summary>
        /// Tüm aktif kuryelere duyuru gönderir.
        /// </summary>
        Task BroadcastToCouriersAsync(string message, string priority = "normal");

        // =====================================================================
        // ADMİN BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Adminlere yeni sipariş bildirimi gönderir.
        /// </summary>
        Task NotifyNewOrderAsync(int orderId, string orderNumber, string customerName, 
            decimal totalAmount, int itemCount);
        
        /// <summary>
        /// Adminlere ödeme başarılı bildirimi gönderir.
        /// </summary>
        Task NotifyPaymentSuccessAsync(int orderId, string orderNumber, decimal amount, string provider);
        
        /// <summary>
        /// Adminlere ödeme başarısız bildirimi gönderir.
        /// </summary>
        Task NotifyPaymentFailedAsync(int orderId, string orderNumber, string reason, string provider);
        
        /// <summary>
        /// Adminlere teslimat sorunu bildirimi gönderir.
        /// </summary>
        Task NotifyDeliveryProblemToAdminAsync(int orderId, string orderNumber, string problemType, 
            string courierName, string? details = null);
        
        /// <summary>
        /// Adminlere sipariş iptal bildirimi gönderir.
        /// </summary>
        Task NotifyOrderCancelledAsync(int orderId, string orderNumber, string reason, string cancelledBy);
        
        /// <summary>
        /// Adminlere iade talebi bildirimi gönderir.
        /// </summary>
        Task NotifyRefundRequestedAsync(int orderId, string orderNumber, decimal refundAmount, string reason);
        
        /// <summary>
        /// Adminlere düşük stok uyarısı gönderir.
        /// </summary>
        Task NotifyLowStockAlertAsync(int productId, string productName, int currentStock, int minStock);
        
        /// <summary>
        /// Adminlere genel uyarı gönderir.
        /// </summary>
        Task NotifyAdminAlertAsync(string alertType, string title, string message, string? actionUrl = null);
        
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
        // TESLİMAT TAKİP BİLDİRİMLERİ (Delivery Task için)
        // =====================================================================
        
        /// <summary>
        /// Teslimat durumu değişikliğini izleyenlere bildirir.
        /// </summary>
        Task NotifyDeliveryStatusChangedAsync(int deliveryTaskId, string status, string message);
        
        /// <summary>
        /// Kurye konumunu izleyenlere bildirir.
        /// NOT: GPS takibi şimdilik devre dışı.
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

        // =====================================================================
        // BAĞLANTI YÖNETİMİ
        // =====================================================================
        
        /// <summary>
        /// Kullanıcının bağlı olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> IsUserConnectedAsync(int userId);
        
        /// <summary>
        /// Kuryenin bağlı olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> IsCourierConnectedAsync(int courierId);
        
        /// <summary>
        /// Bağlı admin sayısını döner.
        /// </summary>
        Task<int> GetConnectedAdminCountAsync();
        
        /// <summary>
        /// Bağlı kurye sayısını döner.
        /// </summary>
        Task<int> GetConnectedCourierCountAsync();

        // =====================================================================
        // STORE ATTENDANT (MARKET GÖREVLİSİ) BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Market görevlilerine yeni sipariş bildirimi gönderir.
        /// Sipariş onaylandığında çağrılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="customerName">Müşteri adı</param>
        /// <param name="itemCount">Ürün sayısı</param>
        /// <param name="totalAmount">Toplam tutar</param>
        /// <param name="confirmedAt">Onay zamanı</param>
        Task NotifyStoreAttendantNewOrderAsync(int orderId, string orderNumber, string customerName,
            int itemCount, decimal totalAmount, DateTime confirmedAt);
        
        /// <summary>
        /// Market görevlilerine sipariş onay bildirimi gönderir.
        /// Admin siparişi onayladığında çağrılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="confirmedBy">Onaylayan kullanıcı adı</param>
        /// <param name="confirmedAt">Onay zamanı</param>
        Task NotifyStoreAttendantOrderConfirmedAsync(int orderId, string orderNumber, 
            string confirmedBy, DateTime confirmedAt);

        // =====================================================================
        // DISPATCHER (SEVKİYAT GÖREVLİSİ) BİLDİRİMLERİ
        // =====================================================================
        
        /// <summary>
        /// Sevkiyat görevlilerine sipariş hazır bildirimi gönderir.
        /// Market görevlisi siparişi "Hazır" olarak işaretlediğinde çağrılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="deliveryAddress">Teslimat adresi</param>
        /// <param name="totalAmount">Toplam tutar</param>
        /// <param name="paymentMethod">Ödeme yöntemi</param>
        /// <param name="weightInGrams">Ağırlık (gram, opsiyonel)</param>
        /// <param name="readyAt">Hazır olma zamanı</param>
        Task NotifyDispatcherOrderReadyAsync(int orderId, string orderNumber, string deliveryAddress,
            decimal totalAmount, string paymentMethod, int? weightInGrams, DateTime readyAt);

        // =====================================================================
        // TÜM TARAFLARA BİLDİRİM (ORTAK)
        // =====================================================================
        
        /// <summary>
        /// Sipariş durumu değiştiğinde tüm ilgili taraflara bildirim gönderir.
        /// Admin, Store Attendant, Dispatcher ve Courier (atanmışsa) bilgilendirilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="oldStatus">Eski durum</param>
        /// <param name="newStatus">Yeni durum</param>
        /// <param name="changedBy">Değişikliği yapan kullanıcı</param>
        /// <param name="courierId">Atanan kurye ID (varsa)</param>
        Task NotifyAllPartiesOrderStatusChangedAsync(int orderId, string orderNumber, 
            string oldStatus, string newStatus, string changedBy, int? courierId = null);

        // =====================================================================
        // SESLİ BİLDİRİM
        // =====================================================================
        
        /// <summary>
        /// Belirli bir gruba sesli bildirim gönderir.
        /// </summary>
        /// <param name="targetGroup">Hedef grup: "store", "dispatch", "courier", "admin"</param>
        /// <param name="soundType">Ses tipi: "new_order", "order_ready", "assigned", "alert", "success"</param>
        /// <param name="priority">Öncelik: "normal", "high", "urgent"</param>
        Task PlaySoundNotificationAsync(string targetGroup, string soundType, string priority = "normal");
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
