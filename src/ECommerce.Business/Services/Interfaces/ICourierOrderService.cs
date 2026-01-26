using System.Threading.Tasks;
using ECommerce.Core.DTOs.Courier;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kurye sipariş işlemlerini yöneten servis interface'i.
    /// 
    /// Güvenlik:
    /// - Her işlemde ownership kontrolü yapılır (order.CourierId == currentUserId)
    /// - Durum geçişleri OrderStateMachine ile validate edilir
    /// - Yetkisiz erişim denemeleri loglanır
    /// 
    /// İş Kuralları:
    /// - Kurye sadece kendisine atanan siparişleri görebilir
    /// - ASSIGNED → OUT_FOR_DELIVERY: "Yola Çıktım"
    /// - OUT_FOR_DELIVERY → DELIVERED: "Teslim Ettim" + Payment Capture
    /// - Herhangi bir durum → DELIVERY_FAILED: "Sorun Var"
    /// 
    /// Entegrasyonlar:
    /// - IOrderStateMachine: Durum geçiş validasyonu
    /// - IPaymentCaptureService: Teslim anında ödeme çekimi
    /// - IRealTimeNotificationService: Admin/Müşteri bildirimleri
    /// </summary>
    public interface ICourierOrderService
    {
        #region Sipariş Listeleme

        /// <summary>
        /// Kuryeye atanan siparişleri listeler.
        /// Sadece kurye'nin kendi siparişlerini döner.
        /// </summary>
        /// <param name="courierId">Kurye ID (Courier tablosundaki)</param>
        /// <param name="filter">Filtre parametreleri</param>
        /// <returns>Sipariş listesi ve özet istatistikler</returns>
        Task<CourierOrderListResponseDto> GetAssignedOrdersAsync(int courierId, CourierOrderFilterDto? filter = null);

        /// <summary>
        /// Belirli bir siparişin detayını getirir.
        /// Ownership kontrolü yapar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <returns>Sipariş detayı veya null (yetkisiz ise)</returns>
        Task<CourierOrderDetailDto?> GetOrderDetailAsync(int orderId, int courierId);

        #endregion

        #region Sipariş Aksiyonları

        /// <summary>
        /// Kurye teslimat için yola çıktığını bildirir.
        /// ASSIGNED → OUT_FOR_DELIVERY geçişi yapar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <param name="dto">Yola çıkış bilgileri</param>
        /// <returns>İşlem sonucu</returns>
        Task<CourierOrderActionResponseDto> StartDeliveryAsync(int orderId, int courierId, StartDeliveryDto dto);

        /// <summary>
        /// Kurye siparişi teslim ettiğini bildirir.
        /// OUT_FOR_DELIVERY → DELIVERED geçişi yapar.
        /// Payment capture işlemi tetiklenir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <param name="dto">Teslim bilgileri</param>
        /// <returns>İşlem sonucu (capture bilgisi dahil)</returns>
        Task<CourierOrderActionResponseDto> MarkDeliveredAsync(int orderId, int courierId, MarkDeliveredDto dto);

        /// <summary>
        /// Kurye teslimat problemi bildirir.
        /// Mevcut durum → DELIVERY_FAILED geçişi yapar.
        /// Admin'e bildirim gönderilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <param name="dto">Problem bilgileri</param>
        /// <returns>İşlem sonucu</returns>
        Task<CourierOrderActionResponseDto> ReportProblemAsync(int orderId, int courierId, ReportProblemDto dto);

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Kurye'nin belirli bir siparişe erişim yetkisi olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <returns>Erişim varsa true</returns>
        Task<bool> ValidateOrderOwnershipAsync(int orderId, int courierId);

        /// <summary>
        /// User ID'den Courier ID'yi bulur.
        /// CourierAuthController'dan gelen userId'yi Courier tablosundaki id'ye çevirir.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Courier ID veya null</returns>
        Task<int?> GetCourierIdByUserIdAsync(int userId);

        /// <summary>
        /// Kurye'nin günlük istatistiklerini getirir.
        /// </summary>
        /// <param name="courierId">Kurye ID</param>
        /// <returns>Özet istatistikler</returns>
        Task<CourierOrderSummaryDto> GetDailySummaryAsync(int courierId);

        #endregion
    }
}
