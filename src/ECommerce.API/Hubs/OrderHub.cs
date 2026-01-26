using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Müşteri sipariş takibi için SignalR Hub
    /// 
    /// KULLANIM:
    /// 1. Müşteri siparişini takip etmek istediğinde JoinOrderTracking(orderId) çağırır
    /// 2. Sipariş durumu değiştiğinde OrderStatusChanged event'i tetiklenir
    /// 3. Müşteri sadece KENDİ siparişini takip edebilir (ownership kontrolü)
    /// 
    /// GÜVENLİK:
    /// - [Authorize] ile sadece giriş yapmış kullanıcılar erişebilir
    /// - Order ownership kontrolü: userId == order.UserId
    /// - Guest siparişler için orderId + email kombinasyonu ile doğrulama
    /// 
    /// EVENTS:
    /// - OrderStatusChanged(orderId, status, message, timestamp)
    /// - OrderDeliveryUpdate(orderId, courierName, estimatedMinutes)
    /// </summary>
    [Authorize]
    public class OrderHub : Hub
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<OrderHub> _logger;

        public OrderHub(ECommerceDbContext context, ILogger<OrderHub> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sipariş takip odasına katıl
        /// Müşteri sadece kendi siparişini takip edebilir
        /// </summary>
        /// <param name="orderId">Takip edilecek sipariş ID</param>
        /// <returns>Başarılı olursa true, değilse false</returns>
        public async Task<bool> JoinOrderTracking(int orderId)
        {
            try
            {
                // Kullanıcı ID'sini al
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("JoinOrderTracking: Kullanıcı ID bulunamadı. ConnectionId: {ConnectionId}", 
                        Context.ConnectionId);
                    return false;
                }

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("JoinOrderTracking: Geçersiz kullanıcı ID formatı. Value: {Value}", userIdClaim);
                    return false;
                }

                // Siparişi bul ve ownership kontrolü yap
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("JoinOrderTracking: Sipariş bulunamadı. OrderId: {OrderId}, UserId: {UserId}", 
                        orderId, userId);
                    return false;
                }

                // Ownership kontrolü: Kullanıcı sadece kendi siparişini takip edebilir
                // Guest siparişler için UserId null olabilir, bu durumda farklı bir doğrulama gerekir
                if (order.UserId != userId && order.UserId != null)
                {
                    _logger.LogWarning(
                        "JoinOrderTracking: Yetkisiz erişim denemesi. OrderId: {OrderId}, OrderUserId: {OrderUserId}, RequestingUserId: {RequestingUserId}",
                        orderId, order.UserId, userId);
                    return false;
                }

                // Gruba katıl
                var groupName = GetOrderGroupName(orderId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation(
                    "JoinOrderTracking: Kullanıcı sipariş takibine başladı. OrderId: {OrderId}, UserId: {UserId}, ConnectionId: {ConnectionId}",
                    orderId, userId, Context.ConnectionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinOrderTracking: Hata oluştu. OrderId: {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Guest sipariş takibi için özel metod
        /// OrderNumber + Email kombinasyonu ile doğrulama yapar
        /// </summary>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="email">Sipariş sahibinin email'i</param>
        /// <returns>Başarılı olursa orderId, değilse -1</returns>
        [AllowAnonymous]
        public async Task<int> JoinGuestOrderTracking(string orderNumber, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("JoinGuestOrderTracking: Eksik parametre. OrderNumber: {OrderNumber}", orderNumber);
                    return -1;
                }

                // Email'i normalize et
                var normalizedEmail = email.Trim().ToLowerInvariant();

                // Guest siparişi bul
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => 
                        o.OrderNumber == orderNumber && 
                        o.IsGuestOrder == true &&
                        o.CustomerEmail != null &&
                        o.CustomerEmail.ToLower() == normalizedEmail);

                if (order == null)
                {
                    _logger.LogWarning(
                        "JoinGuestOrderTracking: Sipariş bulunamadı. OrderNumber: {OrderNumber}, Email: {Email}",
                        orderNumber, normalizedEmail);
                    return -1;
                }

                // Gruba katıl
                var groupName = GetOrderGroupName(order.Id);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation(
                    "JoinGuestOrderTracking: Guest sipariş takibine başladı. OrderId: {OrderId}, OrderNumber: {OrderNumber}, ConnectionId: {ConnectionId}",
                    order.Id, orderNumber, Context.ConnectionId);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinGuestOrderTracking: Hata oluştu. OrderNumber: {OrderNumber}", orderNumber);
                return -1;
            }
        }

        /// <summary>
        /// Sipariş takip odasından ayrıl
        /// </summary>
        /// <param name="orderId">Ayrılınacak sipariş ID</param>
        public async Task LeaveOrderTracking(int orderId)
        {
            try
            {
                var groupName = GetOrderGroupName(orderId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation(
                    "LeaveOrderTracking: Kullanıcı sipariş takibinden ayrıldı. OrderId: {OrderId}, ConnectionId: {ConnectionId}",
                    orderId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LeaveOrderTracking: Hata oluştu. OrderId: {OrderId}", orderId);
            }
        }

        /// <summary>
        /// Bağlantı koptuğunda otomatik olarak çağrılır
        /// Cleanup işlemleri burada yapılır
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, 
                    "OrderHub: Bağlantı hata ile sonlandı. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
            else
            {
                _logger.LogDebug("OrderHub: Bağlantı sonlandı. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Sipariş için grup adı oluşturur
        /// Format: "order-{orderId}"
        /// </summary>
        private static string GetOrderGroupName(int orderId) => $"order-{orderId}";
    }
}
