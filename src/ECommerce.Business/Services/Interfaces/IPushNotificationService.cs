// ==========================================================================
// IPushNotificationService.cs - Push Notification Servis Interface'i
// ==========================================================================
// Firebase Cloud Messaging (FCM) ve benzeri push notification sistemleri
// için abstract layer. Mobil cihazlara bildirim göndermek için kullanılır.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Push notification servis interface'i.
    /// FCM, OneSignal gibi servisler için ortak arayüz.
    /// </summary>
    public interface IPushNotificationService
    {
        // =====================================================================
        // TEKİL BİLDİRİMLER
        // =====================================================================

        /// <summary>
        /// Kullanıcıya (tüm cihazlarına) push notification gönderir.
        /// </summary>
        Task<bool> SendToUserAsync(int userId, PushNotificationPayload payload);

        /// <summary>
        /// Birden fazla kullanıcıya toplu push notification gönderir.
        /// </summary>
        Task<Dictionary<int, bool>> SendToUsersAsync(IEnumerable<int> userIds, PushNotificationPayload payload);

        /// <summary>
        /// Kuryeye push notification gönderir.
        /// </summary>
        Task<bool> SendToCourierAsync(int courierId, PushNotificationPayload payload);

        /// <summary>
        /// Tüm admin kullanıcılarına push notification gönderir.
        /// </summary>
        Task<bool> SendToAdminAsync(PushNotificationPayload payload);

        // =====================================================================
        // TOPIC BİLDİRİMLERİ
        // =====================================================================

        /// <summary>
        /// Bir topic'e abone tüm cihazlara push notification gönderir.
        /// </summary>
        Task<bool> SendToTopicAsync(string topic, PushNotificationPayload payload);

        /// <summary>
        /// Kullanıcıyı bir topic'e abone yapar.
        /// </summary>
        Task<bool> SubscribeToTopicAsync(string deviceToken, string topic);

        /// <summary>
        /// Kullanıcının topic aboneliğini iptal eder.
        /// </summary>
        Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic);

        // =====================================================================
        // CİHAZ YÖNETİMİ
        // =====================================================================

        /// <summary>
        /// Kullanıcı veya kurye cihaz token'ını kaydeder.
        /// </summary>
        Task<bool> RegisterDeviceTokenAsync(string token, int? userId, int? courierId, DevicePlatform platform);

        /// <summary>
        /// Cihaz token'ını siler.
        /// </summary>
        Task<bool> UnregisterDeviceTokenAsync(string deviceToken);
    }

    // =========================================================================
    // PAYLOAD SINIFLARI
    // =========================================================================

    /// <summary>
    /// Push notification içeriği.
    /// </summary>
    public class PushNotificationPayload
    {
        /// <summary>Bildirim başlığı</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Bildirim mesajı</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Bildirim ikonu (opsiyonel)</summary>
        public string? Icon { get; set; }

        /// <summary>Bildirim resmi (opsiyonel)</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Tıklandığında açılacak URL/deep link</summary>
        public string? ClickAction { get; set; }

        /// <summary>Özel veri (JSON olarak gönderilir)</summary>
        public Dictionary<string, string> Data { get; set; } = new();

        /// <summary>Bildirim sesi</summary>
        public string? Sound { get; set; }

        /// <summary>Badge sayısı (iOS)</summary>
        public int? Badge { get; set; }
    }

    // NOT: DevicePlatform enum'u ECommerce.Entities.Enums altında tanımlandı
}
