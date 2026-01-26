using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Admin bildirimleri için SignalR Hub
    /// 
    /// KULLANIM:
    /// 1. Admin paneli açıldığında JoinAdminRoom() çağrılır
    /// 2. Yeni sipariş, teslimat problemi, ödeme hatası gibi durumlarda event tetiklenir
    /// 3. Tüm adminler aynı odada bildirim alır
    /// 
    /// GÜVENLİK:
    /// - Sadece Admin ve SuperAdmin rolleri erişebilir
    /// - JWT token ile doğrulama yapılır
    /// 
    /// EVENTS:
    /// - NewOrderReceived(orderId, orderNumber, customerName, totalAmount, createdAt)
    /// - DeliveryProblem(orderId, orderNumber, courierName, problemReason)
    /// - PaymentCaptureFailed(orderId, orderNumber, amount, failureReason)
    /// - CourierOffline(courierId, courierName, activeOrderCount)
    /// - DashboardMetricsUpdate(metrics)
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin,StoreManager")]
    public class AdminNotificationHub : Hub
    {
        private readonly ILogger<AdminNotificationHub> _logger;
        
        // Admin odası için sabit grup adı
        private const string AdminRoomName = "admin-notifications";

        public AdminNotificationHub(ILogger<AdminNotificationHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Admin bildirim odasına katıl
        /// Tüm adminler aynı odada bildirim alır
        /// </summary>
        public async Task JoinAdminRoom()
        {
            try
            {
                var userId = GetUserId();
                var userName = GetUserName();
                var userRole = GetUserRole();

                await Groups.AddToGroupAsync(Context.ConnectionId, AdminRoomName);

                _logger.LogInformation(
                    "JoinAdminRoom: Admin bildirim odasına katıldı. UserId: {UserId}, UserName: {UserName}, Role: {Role}, ConnectionId: {ConnectionId}",
                    userId, userName, userRole, Context.ConnectionId);

                // Diğer adminlere katılım bildirimi (opsiyonel)
                await Clients.OthersInGroup(AdminRoomName).SendAsync("AdminJoined", new
                {
                    UserId = userId,
                    UserName = userName,
                    JoinedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinAdminRoom: Hata oluştu. ConnectionId: {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        /// <summary>
        /// Admin bildirim odasından ayrıl
        /// </summary>
        public async Task LeaveAdminRoom()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminRoomName);

                var userId = GetUserId();
                _logger.LogInformation(
                    "LeaveAdminRoom: Admin bildirim odasından ayrıldı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaveAdminRoom: Hata oluştu. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Bildirim okundu olarak işaretle
        /// Client tarafından çağrılır, server'da loglama için kullanılır
        /// </summary>
        /// <param name="notificationId">Okundu olarak işaretlenecek bildirim ID</param>
        public Task MarkNotificationRead(string notificationId)
        {
            var userId = GetUserId();
            _logger.LogDebug(
                "MarkNotificationRead: Bildirim okundu. NotificationId: {NotificationId}, UserId: {UserId}",
                notificationId, userId);
            
            // İsteğe bağlı: Veritabanında işaretleme yapılabilir
            return Task.CompletedTask;
        }

        /// <summary>
        /// Admin bağlantı kurduğunda çağrılır
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            _logger.LogInformation(
                "AdminNotificationHub: Admin bağlandı. UserId: {UserId}, Role: {Role}, ConnectionId: {ConnectionId}",
                userId, userRole, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Admin bağlantısı koptuğunda çağrılır
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "AdminNotificationHub: Admin bağlantısı hata ile sonlandı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation(
                    "AdminNotificationHub: Admin bağlantısı sonlandı. UserId: {UserId}, ConnectionId: {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Helper Methods

        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        private string GetUserName()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value 
                ?? Context.User?.FindFirst("name")?.Value 
                ?? "unknown";
        }

        private string GetUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value 
                ?? Context.User?.FindFirst("role")?.Value 
                ?? "unknown";
        }

        #endregion
    }
}
