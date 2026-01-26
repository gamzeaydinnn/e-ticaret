// ==========================================================================
// StoreAttendantHub.cs - Market Görevlisi SignalR Hub
// ==========================================================================
// Market görevlisi paneli için gerçek zamanlı bildirimler sağlar.
// Yeni sipariş onaylandığında, sipariş durumu değiştiğinde bildirim gönderir.
// ==========================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Market Görevlisi (Store Attendant) bildirimleri için SignalR Hub.
    /// 
    /// KULLANIM:
    /// 1. Market görevlisi paneli açıldığında JoinStoreRoom() çağrılır
    /// 2. Yeni sipariş onaylandığında, sipariş hazır olduğunda bildirim tetiklenir
    /// 3. Sesli bildirim desteği (PlaySound event)
    /// 
    /// GÜVENLİK:
    /// - StoreAttendant, StoreManager, Admin ve SuperAdmin rolleri erişebilir
    /// - JWT token ile doğrulama yapılır
    /// 
    /// EVENTS:
    /// - NewOrderForStore(orderId, orderNumber, customerName, itemCount, totalAmount, createdAt)
    /// - OrderConfirmed(orderId, orderNumber, confirmedBy, confirmedAt)
    /// - OrderStatusChanged(orderId, orderNumber, oldStatus, newStatus, changedBy)
    /// - PlaySound(soundType, priority)
    /// - StoreAttendantJoined(userId, userName, joinedAt)
    /// - StoreAttendantLeft(userId, userName, leftAt)
    /// </summary>
    [Authorize(Roles = "StoreAttendant,StoreManager,Admin,SuperAdmin")]
    public class StoreAttendantHub : Hub
    {
        private readonly ILogger<StoreAttendantHub> _logger;
        
        // Store Attendant odası için sabit grup adı
        // Tüm market görevlileri aynı odada bildirim alır
        private const string StoreRoomName = "store-attendants";
        
        // Aktif bağlantı takibi için
        private static readonly HashSet<string> _activeConnections = new();
        private static readonly object _connectionLock = new();

        public StoreAttendantHub(ILogger<StoreAttendantHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Room Management - Oda Yönetimi

        /// <summary>
        /// Market görevlisi bildirim odasına katıl.
        /// Panel açıldığında çağrılır.
        /// </summary>
        /// <returns>Katılım başarılı ise true</returns>
        public async Task<bool> JoinStoreRoom()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                var userRole = GetUserRole();

                // Odaya katıl
                await Groups.AddToGroupAsync(Context.ConnectionId, StoreRoomName);

                // Aktif bağlantıları takip et
                lock (_connectionLock)
                {
                    _activeConnections.Add(Context.ConnectionId);
                }

                _logger.LogInformation(
                    "🏪 JoinStoreRoom: Market görevlisi odasına katıldı. " +
                    "UserId: {UserId}, UserName: {UserName}, Role: {Role}, ConnectionId: {ConnectionId}",
                    userId, userName, userRole, Context.ConnectionId);

                // Diğer market görevlilerine katılım bildirimi (opsiyonel)
                await Clients.OthersInGroup(StoreRoomName).SendAsync("StoreAttendantJoined", new
                {
                    UserId = userId,
                    UserName = userName,
                    Role = userRole,
                    JoinedAt = DateTime.UtcNow,
                    ActiveCount = GetActiveConnectionCount()
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "JoinStoreRoom: Hata oluştu. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
                return false;
            }
        }

        /// <summary>
        /// Market görevlisi bildirim odasından ayrıl.
        /// Panel kapatıldığında çağrılır.
        /// </summary>
        public async Task LeaveStoreRoom()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                // Odadan ayrıl
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, StoreRoomName);

                // Aktif bağlantıları güncelle
                lock (_connectionLock)
                {
                    _activeConnections.Remove(Context.ConnectionId);
                }

                _logger.LogInformation(
                    "🏪 LeaveStoreRoom: Market görevlisi odasından ayrıldı. " +
                    "UserId: {UserId}, UserName: {UserName}, ConnectionId: {ConnectionId}",
                    userId, userName, Context.ConnectionId);

                // Diğer market görevlilerine ayrılma bildirimi
                await Clients.OthersInGroup(StoreRoomName).SendAsync("StoreAttendantLeft", new
                {
                    UserId = userId,
                    UserName = userName,
                    LeftAt = DateTime.UtcNow,
                    ActiveCount = GetActiveConnectionCount()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "LeaveStoreRoom: Hata oluştu. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        #endregion

        #region Connection Lifecycle - Bağlantı Yaşam Döngüsü

        /// <summary>
        /// Bağlantı kurulduğunda çağrılır.
        /// Otomatik olarak store odasına ekler.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                var userRole = GetUserRole();

                _logger.LogInformation(
                    "🔌 StoreAttendantHub Connected: UserId={UserId}, UserName={UserName}, Role={Role}, ConnectionId={ConnectionId}",
                    userId, userName, userRole, Context.ConnectionId);

                // Otomatik olarak store odasına katıl
                await Groups.AddToGroupAsync(Context.ConnectionId, StoreRoomName);

                lock (_connectionLock)
                {
                    _activeConnections.Add(Context.ConnectionId);
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnConnectedAsync hatası. ConnectionId: {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        /// <summary>
        /// Bağlantı koptuğunda çağrılır.
        /// Temizlik işlemleri yapar.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetUserId();

                // Aktif bağlantılardan kaldır
                lock (_connectionLock)
                {
                    _activeConnections.Remove(Context.ConnectionId);
                }

                // Odadan otomatik olarak ayrılır (SignalR bunu otomatik yapar ama log için)
                _logger.LogInformation(
                    "🔌 StoreAttendantHub Disconnected: UserId={UserId}, ConnectionId={ConnectionId}, Reason={Reason}",
                    userId, Context.ConnectionId, exception?.Message ?? "Normal disconnect");

                // Diğer market görevlilerine bildir
                await Clients.Group(StoreRoomName).SendAsync("StoreAttendantLeft", new
                {
                    UserId = userId,
                    LeftAt = DateTime.UtcNow,
                    ActiveCount = GetActiveConnectionCount()
                });

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnDisconnectedAsync hatası. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
        }

        #endregion

        #region Client Methods - İstemci Metodları

        /// <summary>
        /// Market görevlisi bir siparişi hazırlamaya başladığını bildirir.
        /// Bu metod client tarafından çağrılabilir (opsiyonel koordinasyon için).
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        public async Task NotifyStartedPreparing(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                _logger.LogInformation(
                    "🍳 Market görevlisi hazırlamaya başladı. OrderId={OrderId}, UserId={UserId}, UserName={UserName}",
                    orderId, userId, userName);

                // Diğer market görevlilerine bildir (aynı siparişi açmasınlar)
                await Clients.OthersInGroup(StoreRoomName).SendAsync("OrderBeingPrepared", new
                {
                    OrderId = orderId,
                    PreparedBy = userName,
                    StartedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyStartedPreparing hatası. OrderId={OrderId}", orderId);
            }
        }

        /// <summary>
        /// Bildirim okundu olarak işaretle.
        /// Client tarafından çağrılır, sunucuda loglama için kullanılır.
        /// </summary>
        /// <param name="notificationId">Okundu olarak işaretlenecek bildirim ID</param>
        public Task MarkNotificationRead(string notificationId)
        {
            var userId = GetUserId();
            
            _logger.LogDebug(
                "📖 Store bildirim okundu: NotificationId={NotificationId}, UserId={UserId}",
                notificationId, userId);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Aktif bağlı market görevlisi sayısını döner.
        /// </summary>
        public Task<int> GetActiveStoreAttendantCount()
        {
            return Task.FromResult(GetActiveConnectionCount());
        }

        #endregion

        #region Helper Methods - Yardımcı Metodlar

        /// <summary>
        /// Mevcut kullanıcının ID'sini alır.
        /// </summary>
        private string? GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? Context.User?.FindFirst("sub")?.Value;
        }

        /// <summary>
        /// Mevcut kullanıcının adını alır.
        /// </summary>
        private string? GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value 
                ?? Context.User?.FindFirst("name")?.Value
                ?? Context.User?.Identity?.Name;
        }

        /// <summary>
        /// Mevcut kullanıcının rolünü alır.
        /// </summary>
        private string? GetUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value 
                ?? Context.User?.FindFirst("role")?.Value;
        }

        /// <summary>
        /// Aktif bağlantı sayısını döner (thread-safe).
        /// </summary>
        private int GetActiveConnectionCount()
        {
            lock (_connectionLock)
            {
                return _activeConnections.Count;
            }
        }

        /// <summary>
        /// Store room grup adını döner (statik erişim için).
        /// RealTimeNotificationService'ten erişim için kullanılır.
        /// </summary>
        public static string GetStoreRoomGroupName() => StoreRoomName;

        #endregion
    }
}
