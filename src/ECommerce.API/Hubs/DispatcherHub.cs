// ==========================================================================
// DispatcherHub.cs - Sevkiyat Görevlisi SignalR Hub
// ==========================================================================
// Sevkiyat/Kargo görevlisi paneli için gerçek zamanlı bildirimler sağlar.
// Sipariş hazır olduğunda, kurye ataması yapıldığında bildirim gönderir.
// ==========================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Sevkiyat Görevlisi (Dispatcher) bildirimleri için SignalR Hub.
    /// 
    /// KULLANIM:
    /// 1. Sevkiyat görevlisi paneli açıldığında JoinDispatchRoom() çağrılır
    /// 2. Sipariş hazır olduğunda, kurye atandığında/değiştiğinde bildirim tetiklenir
    /// 3. Kurye durumu değiştiğinde bildirim alır
    /// 4. Sesli bildirim desteği (PlaySound event)
    /// 
    /// GÜVENLİK:
    /// - Dispatcher, StoreManager, Admin ve SuperAdmin rolleri erişebilir
    /// - JWT token ile doğrulama yapılır
    /// 
    /// EVENTS:
    /// - OrderReadyForDispatch(orderId, orderNumber, address, totalAmount, paymentMethod, readyAt)
    /// - OrderAssigned(orderId, orderNumber, courierId, courierName, assignedAt)
    /// - OrderReassigned(orderId, orderNumber, oldCourierId, newCourierId, reason, reassignedAt)
    /// - CourierStatusChanged(courierId, courierName, newStatus, activeOrderCount)
    /// - CourierOnline(courierId, courierName, vehicleType)
    /// - CourierOffline(courierId, courierName, reason)
    /// - PlaySound(soundType, priority)
    /// - DispatcherJoined(userId, userName, joinedAt)
    /// - DispatcherLeft(userId, userName, leftAt)
    /// </summary>
    [Authorize(Roles = "Dispatcher,StoreManager,Admin,SuperAdmin")]
    public class DispatcherHub : Hub
    {
        private readonly ILogger<DispatcherHub> _logger;
        
        // Dispatcher odası için sabit grup adı
        // Tüm sevkiyat görevlileri aynı odada bildirim alır
        private const string DispatchRoomName = "dispatchers";
        
        // Aktif bağlantı takibi için
        private static readonly HashSet<string> _activeConnections = new();
        private static readonly object _connectionLock = new();

        public DispatcherHub(ILogger<DispatcherHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Room Management - Oda Yönetimi

        /// <summary>
        /// Sevkiyat görevlisi bildirim odasına katıl.
        /// Panel açıldığında çağrılır.
        /// </summary>
        /// <returns>Katılım başarılı ise true</returns>
        public async Task<bool> JoinDispatchRoom()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                var userRole = GetUserRole();

                // Odaya katıl
                await Groups.AddToGroupAsync(Context.ConnectionId, DispatchRoomName);

                // Aktif bağlantıları takip et
                lock (_connectionLock)
                {
                    _activeConnections.Add(Context.ConnectionId);
                }

                _logger.LogInformation(
                    "🚚 JoinDispatchRoom: Sevkiyat görevlisi odasına katıldı. " +
                    "UserId: {UserId}, UserName: {UserName}, Role: {Role}, ConnectionId: {ConnectionId}",
                    userId, userName, userRole, Context.ConnectionId);

                // Diğer sevkiyat görevlilerine katılım bildirimi
                await Clients.OthersInGroup(DispatchRoomName).SendAsync("DispatcherJoined", new
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
                    "JoinDispatchRoom: Hata oluştu. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
                return false;
            }
        }

        /// <summary>
        /// Sevkiyat görevlisi bildirim odasından ayrıl.
        /// Panel kapatıldığında çağrılır.
        /// </summary>
        public async Task LeaveDispatchRoom()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                // Odadan ayrıl
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchRoomName);

                // Aktif bağlantıları güncelle
                lock (_connectionLock)
                {
                    _activeConnections.Remove(Context.ConnectionId);
                }

                _logger.LogInformation(
                    "🚚 LeaveDispatchRoom: Sevkiyat görevlisi odasından ayrıldı. " +
                    "UserId: {UserId}, UserName: {UserName}, ConnectionId: {ConnectionId}",
                    userId, userName, Context.ConnectionId);

                // Diğer sevkiyat görevlilerine ayrılma bildirimi
                await Clients.OthersInGroup(DispatchRoomName).SendAsync("DispatcherLeft", new
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
                    "LeaveDispatchRoom: Hata oluştu. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        #endregion

        #region Connection Lifecycle - Bağlantı Yaşam Döngüsü

        /// <summary>
        /// Bağlantı kurulduğunda çağrılır.
        /// Otomatik olarak dispatch odasına ekler.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                var userRole = GetUserRole();

                _logger.LogInformation(
                    "DispatcherHub Connected: UserId={UserId}, UserName={UserName}, Role={Role}, ConnectionId={ConnectionId}",
                    userId, userName, userRole, Context.ConnectionId);

                // NOT: Gruba ekleme JoinDispatchRoom() ile yapılır.
                // OnConnectedAsync'te tekrar eklenmez (double-join önlemi).

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

                _logger.LogInformation(
                    "🔌 DispatcherHub Disconnected: UserId={UserId}, ConnectionId={ConnectionId}, Reason={Reason}",
                    userId, Context.ConnectionId, exception?.Message ?? "Normal disconnect");

                // Diğer sevkiyat görevlilerine bildir
                await Clients.Group(DispatchRoomName).SendAsync("DispatcherLeft", new
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
        /// Sevkiyat görevlisi bir siparişe kurye atama işlemine başladığını bildirir.
        /// Bu metod client tarafından çağrılabilir (opsiyonel koordinasyon için).
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        public async Task NotifyAssigningCourier(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                _logger.LogInformation(
                    "🚴 Sevkiyat görevlisi kurye atıyor. OrderId={OrderId}, UserId={UserId}, UserName={UserName}",
                    orderId, userId, userName);

                // Diğer sevkiyat görevlilerine bildir (aynı siparişe atama yapmasınlar)
                await Clients.OthersInGroup(DispatchRoomName).SendAsync("OrderBeingAssigned", new
                {
                    OrderId = orderId,
                    AssignedBy = userName,
                    StartedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyAssigningCourier hatası. OrderId={OrderId}", orderId);
            }
        }

        /// <summary>
        /// Kurye atama işlemi iptal edildiğinde çağrılır.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        public async Task NotifyAssignmentCancelled(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();

                _logger.LogInformation(
                    "❌ Kurye atama iptal edildi. OrderId={OrderId}, UserId={UserId}",
                    orderId, userId);

                // Diğer sevkiyat görevlilerine bildir
                await Clients.OthersInGroup(DispatchRoomName).SendAsync("AssignmentCancelled", new
                {
                    OrderId = orderId,
                    CancelledBy = userName,
                    CancelledAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotifyAssignmentCancelled hatası. OrderId={OrderId}", orderId);
            }
        }

        /// <summary>
        /// Belirli bir kuryenin detaylarını almak için istek gönderir.
        /// Sunucu tarafından kurye bilgisi client'a gönderilir.
        /// </summary>
        /// <param name="courierId">Kurye ID</param>
        public async Task RequestCourierDetails(int courierId)
        {
            try
            {
                var userId = GetUserId();

                _logger.LogDebug(
                    "📋 Kurye detayları istendi. CourierId={CourierId}, RequestedBy={UserId}",
                    courierId, userId);

                // Bu metod sadece log için. Gerçek veri CourierService'ten gelir.
                // Gelecekte bu endpoint aracılığıyla real-time kurye durumu alınabilir.
                
                await Clients.Caller.SendAsync("CourierDetailsRequested", new
                {
                    CourierId = courierId,
                    Message = "Kurye detayları API üzerinden alınacak."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestCourierDetails hatası. CourierId={CourierId}", courierId);
            }
        }

        /// <summary>
        /// Bildirim okundu olarak işaretle.
        /// Client tarafından çağrılır.
        /// </summary>
        /// <param name="notificationId">Okundu olarak işaretlenecek bildirim ID</param>
        public Task MarkNotificationRead(string notificationId)
        {
            var userId = GetUserId();
            
            _logger.LogDebug(
                "📖 Dispatch bildirim okundu: NotificationId={NotificationId}, UserId={UserId}",
                notificationId, userId);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Aktif bağlı sevkiyat görevlisi sayısını döner.
        /// </summary>
        public Task<int> GetActiveDispatcherCount()
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
        /// Dispatch room grup adını döner (statik erişim için).
        /// RealTimeNotificationService'ten erişim için kullanılır.
        /// </summary>
        public static string GetDispatchRoomGroupName() => DispatchRoomName;

        #endregion
    }
}
