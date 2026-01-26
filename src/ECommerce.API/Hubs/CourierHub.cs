using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Kurye bildirimleri için SignalR Hub
    /// 
    /// KULLANIM:
    /// 1. Kurye uygulamayı açtığında JoinCourierRoom() çağrılır
    /// 2. Kurye kendi ID'sine özel odaya katılır
    /// 3. Yeni sipariş atandığında, sipariş iptal edildiğinde event tetiklenir
    /// 
    /// GÜVENLİK:
    /// - Sadece Courier rolü erişebilir
    /// - Ownership kontrolü: Kurye sadece kendi odasına katılabilir
    /// - JWT token ile doğrulama
    /// 
    /// EVENTS:
    /// - OrderAssigned(orderId, orderNumber, address, totalAmount, estimatedDeliveryTime)
    /// - OrderCancelled(orderId, orderNumber, cancellationReason)
    /// - OrderUpdated(orderId, orderNumber, updateType, message)
    /// </summary>
    [Authorize(Roles = "Courier")]
    public class CourierHub : Hub
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<CourierHub> _logger;

        public CourierHub(ECommerceDbContext context, ILogger<CourierHub> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Kurye bildirim odasına katıl
        /// Her kurye kendi ID'sine özel odada bildirim alır
        /// Ownership kontrolü: Kurye sadece kendi odasına katılabilir
        /// </summary>
        public async Task<bool> JoinCourierRoom()
        {
            try
            {
                var userId = GetUserId();
                
                if (userId == null)
                {
                    _logger.LogWarning(
                        "JoinCourierRoom: Kullanıcı ID bulunamadı. ConnectionId: {ConnectionId}",
                        Context.ConnectionId);
                    return false;
                }

                // Kurye kaydını kontrol et
                var courier = await _context.Couriers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (courier == null)
                {
                    _logger.LogWarning(
                        "JoinCourierRoom: Kurye kaydı bulunamadı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                        userId, Context.ConnectionId);
                    return false;
                }

                // Kurye aktif mi kontrol et
                if (!courier.IsActive)
                {
                    _logger.LogWarning(
                        "JoinCourierRoom: Kurye pasif durumda. CourierId: {CourierId}, UserId: {UserId}",
                        courier.Id, userId);
                    return false;
                }

                // Kurye odasına katıl (courierId bazlı)
                var groupName = GetCourierGroupName(courier.Id);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                // Kurye durumunu online yap (opsiyonel)
                await UpdateCourierOnlineStatus(courier.Id, true);

                _logger.LogInformation(
                    "JoinCourierRoom: Kurye bildirim odasına katıldı. CourierId: {CourierId}, UserId: {UserId}, ConnectionId: {ConnectionId}",
                    courier.Id, userId, Context.ConnectionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinCourierRoom: Hata oluştu. ConnectionId: {ConnectionId}", Context.ConnectionId);
                return false;
            }
        }

        /// <summary>
        /// Kurye bildirim odasından ayrıl
        /// </summary>
        public async Task LeaveCourierRoom()
        {
            try
            {
                var userId = GetUserId();
                
                if (userId == null) return;

                // Kurye kaydını bul
                var courier = await _context.Couriers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (courier == null) return;

                var groupName = GetCourierGroupName(courier.Id);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation(
                    "LeaveCourierRoom: Kurye bildirim odasından ayrıldı. CourierId: {CourierId}, UserId: {UserId}",
                    courier.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaveCourierRoom: Hata oluştu. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Kurye konum güncellemesi gönderir (opsiyonel - GPS takibi için)
        /// Not: Bu projede GPS takibi yok, ama ileride eklenebilir
        /// </summary>
        /// <param name="latitude">Enlem</param>
        /// <param name="longitude">Boylam</param>
        public Task UpdateLocation(double latitude, double longitude)
        {
            // GPS takibi şu an için devre dışı
            // İleride gerekirse burada implement edilecek
            _logger.LogDebug(
                "UpdateLocation: Konum güncelleme isteği (GPS takibi devre dışı). Lat: {Lat}, Lng: {Lng}",
                latitude, longitude);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Kurye durumunu günceller (available, busy, offline)
        /// </summary>
        /// <param name="status">Yeni durum: "available", "busy", "offline"</param>
        public async Task<bool> UpdateStatus(string status)
        {
            try
            {
                var validStatuses = new[] { "available", "busy", "offline", "active" };
                if (string.IsNullOrWhiteSpace(status) || !validStatuses.Contains(status.ToLower()))
                {
                    _logger.LogWarning("UpdateStatus: Geçersiz durum. Status: {Status}", status);
                    return false;
                }

                var userId = GetUserId();
                if (userId == null) return false;

                var courier = await _context.Couriers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (courier == null) return false;

                courier.Status = status.ToLower();
                courier.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "UpdateStatus: Kurye durumu güncellendi. CourierId: {CourierId}, NewStatus: {Status}",
                    courier.Id, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateStatus: Hata oluştu. Status: {Status}", status);
                return false;
            }
        }

        /// <summary>
        /// Kurye bağlantı kurduğunda çağrılır
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation(
                "CourierHub: Kurye bağlandı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Kurye bağlantısı koptuğunda çağrılır
        /// Otomatik olarak offline durumuna geçirilir
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "CourierHub: Kurye bağlantısı hata ile sonlandı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation(
                    "CourierHub: Kurye bağlantısı sonlandı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            // Bağlantı koptuğunda kurye durumunu offline yap
            if (userId != null)
            {
                try
                {
                    var courier = await _context.Couriers
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    if (courier != null)
                    {
                        await UpdateCourierOnlineStatus(courier.Id, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OnDisconnectedAsync: Kurye durumu güncellenirken hata. UserId: {UserId}", userId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Helper Methods

        private int? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private static string GetCourierGroupName(int courierId) => $"courier-{courierId}";

        /// <summary>
        /// Kurye online/offline durumunu günceller
        /// </summary>
        private async Task UpdateCourierOnlineStatus(int courierId, bool isOnline)
        {
            try
            {
                var courier = await _context.Couriers.FindAsync(courierId);
                if (courier != null)
                {
                    // Status alanını güncelle: online olunca "active", offline olunca "offline"
                    courier.Status = isOnline ? "active" : "offline";
                    courier.UpdatedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug(
                        "UpdateCourierOnlineStatus: Kurye durumu güncellendi. CourierId: {CourierId}, IsOnline: {IsOnline}",
                        courierId, isOnline);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "UpdateCourierOnlineStatus: Hata oluştu. CourierId: {CourierId}, IsOnline: {IsOnline}",
                    courierId, isOnline);
            }
        }

        #endregion
    }
}
