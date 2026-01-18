// ==========================================================================
// IDeliveryNotificationService.cs - Teslimat Bildirim Servis Interface'i
// ==========================================================================
// Bu interface, teslimat sürecindeki bildirimleri yönetir.
// Kurye, müşteri ve admin bildirimleri için tek arayüz.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat bildirim servis interface'i.
    /// </summary>
    public interface IDeliveryNotificationService
    {
        // =====================================================================
        // KURYE BİLDİRİMLERİ
        // =====================================================================

        /// <summary>
        /// Kuryeye atama bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyCourierAssignmentAsync(int deliveryTaskId, int courierId);

        // =====================================================================
        // MÜŞTERİ BİLDİRİMLERİ
        // =====================================================================

        /// <summary>
        /// Müşteriye durum değişikliği bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyCustomerStatusUpdateAsync(int deliveryTaskId, DeliveryStatus newStatus);

        /// <summary>
        /// Müşteriye ETA güncellemesi bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyCustomerEtaUpdateAsync(int deliveryTaskId, DateTime newEta);

        /// <summary>
        /// Müşteriye kurye yaklaşıyor bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyCustomerCourierApproachingAsync(int deliveryTaskId);

        // =====================================================================
        // ADMİN BİLDİRİMLERİ
        // =====================================================================

        /// <summary>
        /// Admin'e teslimat başarısızlığı bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyAdminDeliveryFailureAsync(int deliveryTaskId);

        /// <summary>
        /// Admin'e yüksek öncelikli görev bildirimi gönderir.
        /// </summary>
        Task<bool> NotifyAdminHighPriorityTaskAsync(int deliveryTaskId);

        // =====================================================================
        // OTP VE DOĞRULAMA
        // =====================================================================

        /// <summary>
        /// Müşteriye OTP kodu gönderir.
        /// </summary>
        Task<bool> SendOtpToCustomerAsync(int deliveryTaskId);

        // =====================================================================
        // TOPLU BİLDİRİM
        // =====================================================================

        /// <summary>
        /// Bölgedeki tüm kuryelere bildirim gönderir.
        /// </summary>
        Task<bool> BroadcastToZoneCouriersAsync(int zoneId, string message);
    }
}
