// ==========================================================================
// PushNotificationManager.cs - Push Notification Servisi Implementasyonu
// ==========================================================================
// FCM (Firebase Cloud Messaging) ve OneSignal entegrasyonu için hazır
// push notification servisi. Şimdilik simülasyon modunda çalışır,
// gerçek FCM credentials eklendiğinde production-ready olur.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Concrete
{
    /// <summary>
    /// Push notification yönetim servisi.
    /// FCM ve OneSignal destekli, simülasyon modunda başlar.
    /// </summary>
    public class PushNotificationManager : IPushNotificationService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<PushNotificationManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // FCM Konfigürasyonu
        private readonly string? _fcmServerKey;
        private readonly string? _fcmSenderId;
        private readonly bool _isSimulationMode;

        // FCM API Endpoint
        private const string FCM_API_URL = "https://fcm.googleapis.com/fcm/send";

        public PushNotificationManager(
            ECommerceDbContext context,
            ILogger<PushNotificationManager> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("FCM");

            // FCM Ayarlarını yükle
            _fcmServerKey = _configuration["Push:FCM:ServerKey"];
            _fcmSenderId = _configuration["Push:FCM:SenderId"];
            
            // Eğer FCM key yoksa simülasyon modunda çalış
            _isSimulationMode = string.IsNullOrEmpty(_fcmServerKey);

            if (_isSimulationMode)
            {
                _logger.LogInformation(
                    "📱 Push Notification servisi SİMÜLASYON modunda başlatıldı. " +
                    "Production için Push:FCM:ServerKey ayarını yapılandırın.");
            }
            else
            {
                _logger.LogInformation(
                    "📱 Push Notification servisi FCM ile aktif. SenderId: {SenderId}",
                    _fcmSenderId);
            }
        }

        #region Kullanıcı Bildirimleri

        /// <summary>
        /// Belirli bir kullanıcıya push notification gönderir
        /// </summary>
        public async Task<bool> SendToUserAsync(int userId, PushNotificationPayload payload)
        {
            try
            {
                // Kullanıcının aktif token'larını al
                var tokens = await _context.Set<ECommerce.Entities.Concrete.DeviceToken>()
                    .Where(dt => dt.UserId == userId && dt.IsActive)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    _logger.LogWarning(
                        "📱 Kullanıcı {UserId} için kayıtlı aktif token bulunamadı",
                        userId);
                    return false;
                }

                var deviceTokens = tokens.Select(t => t.Token).ToList();
                return await SendToDevicesAsync(deviceTokens, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Kullanıcı {UserId} push notification hatası: {Message}",
                    userId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Birden fazla kullanıcıya toplu push notification gönderir
        /// </summary>
        public async Task<Dictionary<int, bool>> SendToUsersAsync(
            IEnumerable<int> userIds,
            PushNotificationPayload payload)
        {
            var results = new Dictionary<int, bool>();

            foreach (var userId in userIds)
            {
                var success = await SendToUserAsync(userId, payload);
                results[userId] = success;
            }

            var successCount = results.Count(r => r.Value);
            _logger.LogInformation(
                "📱 Toplu kullanıcı bildirimi: {Success}/{Total} başarılı",
                successCount, results.Count);

            return results;
        }

        #endregion

        #region Kurye Bildirimleri

        /// <summary>
        /// Kuryeye push notification gönderir
        /// </summary>
        public async Task<bool> SendToCourierAsync(int courierId, PushNotificationPayload payload)
        {
            try
            {
                // Kuryenin aktif token'larını al
                var tokens = await _context.Set<ECommerce.Entities.Concrete.DeviceToken>()
                    .Where(dt => dt.CourierId == courierId && dt.IsActive)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    _logger.LogWarning(
                        "📱 Kurye {CourierId} için kayıtlı aktif token bulunamadı",
                        courierId);
                    return false;
                }

                // Kurye bildirimleri için özel işaretleme
                payload.Data ??= new Dictionary<string, string>();
                payload.Data["recipient_type"] = "courier";
                payload.Data["courier_id"] = courierId.ToString();

                var deviceTokens = tokens.Select(t => t.Token).ToList();
                return await SendToDevicesAsync(deviceTokens, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Kurye {CourierId} push notification hatası: {Message}",
                    courierId, ex.Message);
                return false;
            }
        }

        #endregion

        #region Admin Bildirimleri

        /// <summary>
        /// Tüm admin kullanıcılarına push notification gönderir
        /// </summary>
        public async Task<bool> SendToAdminAsync(PushNotificationPayload payload)
        {
            try
            {
                // Admin rolündeki kullanıcıların ID'lerini al
                var adminUserIds = await _context.Set<ECommerce.Entities.Concrete.User>()
                    .Where(u => u.Role == "Admin" && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!adminUserIds.Any())
                {
                    _logger.LogWarning("📱 Aktif admin kullanıcısı bulunamadı");
                    return false;
                }

                // Admin bildirimleri için özel işaretleme
                payload.Data ??= new Dictionary<string, string>();
                payload.Data["recipient_type"] = "admin";
                payload.Data["priority"] = "high";

                var results = await SendToUsersAsync(adminUserIds, payload);
                return results.Any(r => r.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Admin push notification hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        #endregion

        #region Topic (Konu) Bildirimleri

        /// <summary>
        /// Belirli bir topic'e abone olan tüm cihazlara bildirim gönderir
        /// </summary>
        public async Task<bool> SendToTopicAsync(string topic, PushNotificationPayload payload)
        {
            try
            {
                if (_isSimulationMode)
                {
                    _logger.LogInformation(
                        "📱 [SİMÜLASYON] Topic '{Topic}' bildirimi: {Title}",
                        topic, payload.Title);
                    return true;
                }

                var fcmPayload = new
                {
                    to = $"/topics/{topic}",
                    notification = new
                    {
                        title = payload.Title,
                        body = payload.Body,
                        icon = payload.Icon ?? "/icons/notification-icon.png",
                        click_action = payload.ClickAction
                    },
                    data = payload.Data,
                    priority = "high"
                };

                return await SendFcmRequestAsync(fcmPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Topic '{Topic}' push notification hatası: {Message}",
                    topic, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Bir cihazı belirli bir topic'e abone eder
        /// </summary>
        public async Task<bool> SubscribeToTopicAsync(string deviceToken, string topic)
        {
            try
            {
                if (_isSimulationMode)
                {
                    _logger.LogInformation(
                        "📱 [SİMÜLASYON] Token topic'e abone edildi: {Topic}",
                        topic);
                    return true;
                }

                // FCM topic subscribe API çağrısı
                var subscribeUrl = $"https://iid.googleapis.com/iid/v1/{deviceToken}/rel/topics/{topic}";
                
                var request = new HttpRequestMessage(HttpMethod.Post, subscribeUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("key", $"={_fcmServerKey}");
                
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Topic abone olma hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Bir cihazın topic aboneliğini iptal eder
        /// </summary>
        public async Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic)
        {
            try
            {
                if (_isSimulationMode)
                {
                    _logger.LogInformation(
                        "📱 [SİMÜLASYON] Token topic aboneliği iptal edildi: {Topic}",
                        topic);
                    return true;
                }

                // FCM topic unsubscribe API çağrısı
                var unsubscribeUrl = $"https://iid.googleapis.com/iid/v1/{deviceToken}/rel/topics/{topic}";
                
                var request = new HttpRequestMessage(HttpMethod.Delete, unsubscribeUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("key", $"={_fcmServerKey}");
                
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Topic abonelik iptal hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        #endregion

        #region Token Yönetimi

        /// <summary>
        /// Yeni bir cihaz token'ı kaydeder
        /// </summary>
        public async Task<bool> RegisterDeviceTokenAsync(
            string token,
            int? userId,
            int? courierId,
            DevicePlatform platform)
        {
            try
            {
                // Aynı token var mı kontrol et
                var existingToken = await _context.Set<ECommerce.Entities.Concrete.DeviceToken>()
                    .FirstOrDefaultAsync(dt => dt.Token == token);

                if (existingToken != null)
                {
                    // Token varsa güncelle
                    existingToken.UserId = userId;
                    existingToken.CourierId = courierId;
                    existingToken.Platform = platform;
                    existingToken.IsActive = true;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    existingToken.FailedAttempts = 0;

                    _logger.LogInformation(
                        "📱 Mevcut token güncellendi. UserId: {UserId}, CourierId: {CourierId}",
                        userId, courierId);
                }
                else
                {
                    // Yeni token ekle
                    var deviceToken = new ECommerce.Entities.Concrete.DeviceToken
                    {
                        Token = token,
                        UserId = userId,
                        CourierId = courierId,
                        Platform = platform,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Set<ECommerce.Entities.Concrete.DeviceToken>().Add(deviceToken);

                    _logger.LogInformation(
                        "📱 Yeni token kaydedildi. Platform: {Platform}, UserId: {UserId}, CourierId: {CourierId}",
                        platform, userId, courierId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Token kayıt hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Bir cihaz token'ını siler/devre dışı bırakır
        /// </summary>
        public async Task<bool> UnregisterDeviceTokenAsync(string token)
        {
            try
            {
                var deviceToken = await _context.Set<ECommerce.Entities.Concrete.DeviceToken>()
                    .FirstOrDefaultAsync(dt => dt.Token == token);

                if (deviceToken == null)
                {
                    _logger.LogWarning("📱 Silinecek token bulunamadı: {Token}", token[..20] + "...");
                    return false;
                }

                // Soft delete - tamamen silmiyoruz
                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("📱 Token devre dışı bırakıldı");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Token silme hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Birden fazla cihaza bildirim gönderir
        /// </summary>
        private async Task<bool> SendToDevicesAsync(
            List<string> deviceTokens,
            PushNotificationPayload payload)
        {
            if (!deviceTokens.Any())
                return false;

            if (_isSimulationMode)
            {
                _logger.LogInformation(
                    "📱 [SİMÜLASYON] {Count} cihaza bildirim: {Title} - {Body}",
                    deviceTokens.Count, payload.Title, payload.Body);
                return true;
            }

            try
            {
                // Tek token varsa 'to', birden fazla varsa 'registration_ids' kullan
                object fcmPayload;
                
                if (deviceTokens.Count == 1)
                {
                    fcmPayload = new
                    {
                        to = deviceTokens[0],
                        notification = new
                        {
                            title = payload.Title,
                            body = payload.Body,
                            icon = payload.Icon ?? "/icons/notification-icon.png",
                            click_action = payload.ClickAction,
                            sound = payload.Sound ?? "default"
                        },
                        data = payload.Data,
                        priority = "high",
                        android = new
                        {
                            priority = "high",
                            notification = new
                            {
                                channel_id = "default"
                            }
                        },
                        apns = new
                        {
                            headers = new
                            {
                                apns_priority = "10"
                            },
                            payload = new
                            {
                                aps = new
                                {
                                    alert = new
                                    {
                                        title = payload.Title,
                                        body = payload.Body
                                    },
                                    badge = payload.Badge,
                                    sound = payload.Sound ?? "default"
                                }
                            }
                        }
                    };
                }
                else
                {
                    fcmPayload = new
                    {
                        registration_ids = deviceTokens,
                        notification = new
                        {
                            title = payload.Title,
                            body = payload.Body,
                            icon = payload.Icon ?? "/icons/notification-icon.png",
                            click_action = payload.ClickAction,
                            sound = payload.Sound ?? "default"
                        },
                        data = payload.Data,
                        priority = "high"
                    };
                }

                return await SendFcmRequestAsync(fcmPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "📱 Toplu push notification hatası: {Message}",
                    ex.Message);
                return false;
            }
        }

        /// <summary>
        /// FCM API'sine HTTP isteği gönderir
        /// </summary>
        private async Task<bool> SendFcmRequestAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("key", $"={_fcmServerKey}");

                var response = await _httpClient.PostAsync(FCM_API_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("📱 FCM yanıtı: {Response}", responseBody);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "📱 FCM hata yanıtı ({StatusCode}): {Error}",
                        response.StatusCode, errorBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "📱 FCM isteği hatası: {Message}", ex.Message);
                return false;
            }
        }

        #endregion
    }
}
