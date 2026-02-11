// ==========================================================================
// RealTimeNotificationService.cs - SignalR Hub Bildirim Servisi
// ==========================================================================
// Tüm SignalR hub'larına merkezi bildirim gönderimi için servis.
// IHubContext kullanarak type-safe mesaj gönderir.
// ==========================================================================

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ECommerce.API.Hubs;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.API.Services
{
    /// <summary>
    /// SignalR Hub'larına gerçek zamanlı bildirim gönderen servis.
    /// Tüm hub'lara merkezi erişim sağlar.
    /// </summary>
    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly IHubContext<AdminNotificationHub> _adminHub;
        private readonly IHubContext<CourierHub> _courierHub;
        private readonly IHubContext<StoreAttendantHub> _storeHub;
        private readonly IHubContext<DispatcherHub> _dispatcherHub;
        private readonly ILogger<RealTimeNotificationService> _logger;

        private const string AdminGroupName = "admin-notifications";
        
        // Bağlantı takibi için thread-safe dictionary'ler
        // Key: UserId/CourierId, Value: ConnectionId listesi
        private static readonly ConcurrentDictionary<int, HashSet<string>> UserConnections = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> CourierConnections = new();
        private static readonly ConcurrentDictionary<string, bool> AdminConnections = new();

        public RealTimeNotificationService(
            IHubContext<OrderHub> orderHub,
            IHubContext<AdminNotificationHub> adminHub,
            IHubContext<CourierHub> courierHub,
            IHubContext<StoreAttendantHub> storeHub,
            IHubContext<DispatcherHub> dispatcherHub,
            ILogger<RealTimeNotificationService> logger)
        {
            _orderHub = orderHub ?? throw new ArgumentNullException(nameof(orderHub));
            _adminHub = adminHub ?? throw new ArgumentNullException(nameof(adminHub));
            _courierHub = courierHub ?? throw new ArgumentNullException(nameof(courierHub));
            _storeHub = storeHub ?? throw new ArgumentNullException(nameof(storeHub));
            _dispatcherHub = dispatcherHub ?? throw new ArgumentNullException(nameof(dispatcherHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Müşteri Sipariş Takip Bildirimleri

        /// <inheritdoc />
        public async Task NotifyOrderStatusChangedAsync(int orderId, string orderNumber, string newStatus, 
            string statusText, DateTime? estimatedDelivery = null)
        {
            try
            {
                var groupName = $"order-{orderId}";
                
                var notification = new
                {
                    type = "OrderStatusChanged",
                    orderId,
                    orderNumber,
                    status = newStatus,
                    statusText,
                    estimatedDelivery = estimatedDelivery?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                await _orderHub.Clients.Group(groupName).SendAsync("OrderStatusChanged", notification);
                
                _logger.LogInformation(
                    "📢 Sipariş durum bildirimi gönderildi. OrderId={OrderId}, Status={Status}", 
                    orderId, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durum bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCustomerDeliveryCompletedAsync(int orderId, string orderNumber, 
            DateTime deliveredAt, string? signedBy = null)
        {
            try
            {
                var groupName = $"order-{orderId}";
                
                var notification = new
                {
                    type = "DeliveryCompleted",
                    orderId,
                    orderNumber,
                    deliveredAt = deliveredAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    signedBy,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                await _orderHub.Clients.Group(groupName).SendAsync("DeliveryCompleted", notification);
                
                _logger.LogInformation(
                    "📢 Teslimat tamamlandı bildirimi gönderildi. OrderId={OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teslimat tamamlandı bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCustomerDeliveryProblemAsync(int orderId, string problemType, string message)
        {
            try
            {
                var groupName = $"order-{orderId}";

                var notification = new
                {
                    type = "DeliveryProblem",
                    orderId,
                    problemType,
                    message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                await _orderHub.Clients.Group(groupName).SendAsync("DeliveryProblem", notification);

                _logger.LogInformation(
                    "📢 Teslimat sorunu bildirimi gönderildi. OrderId={OrderId}, ProblemType={ProblemType}",
                    orderId, problemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teslimat sorunu bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCustomerWeightChargeAsync(int orderId, string orderNumber,
            decimal originalAmount, decimal finalAmount, decimal weightDifferenceAmount)
        {
            try
            {
                var groupName = $"order-{orderId}";

                // Ek tahsilat mı yoksa iade mi?
                var isOverage = weightDifferenceAmount > 0;
                var messageText = isOverage
                    ? $"Tartı farkı nedeniyle {weightDifferenceAmount:N2} TL ek tahsilat yapıldı."
                    : $"Tartı farkı nedeniyle {Math.Abs(weightDifferenceAmount):N2} TL iade edildi.";

                var notification = new
                {
                    type = "WeightChargeApplied",
                    orderId,
                    orderNumber,
                    originalAmount,
                    finalAmount,
                    weightDifferenceAmount,
                    isOverage,
                    message = messageText,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                // Müşteriye sipariş takip kanalından bildirim gönder
                await _orderHub.Clients.Group(groupName).SendAsync("WeightChargeApplied", notification);

                // Admin'lere de bilgilendir
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("WeightChargeApplied", new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "WeightChargeApplied",
                    orderId,
                    orderNumber,
                    originalAmount,
                    finalAmount,
                    weightDifferenceAmount,
                    isOverage,
                    message = messageText,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                });

                _logger.LogInformation(
                    "📢 Ağırlık farkı bildirimi gönderildi. OrderId={OrderId}, Fark={Diff:N2} TL, Tip={Type}",
                    orderId, weightDifferenceAmount, isOverage ? "EkTahsilat" : "Iade");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ağırlık farkı bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        #endregion

        #region Kurye Bildirimleri

        /// <inheritdoc />
        public async Task NotifyCourierNewTaskAsync(int courierId, CourierTaskNotification notification)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("NewTask", new
                {
                    type = "NewTask",
                    notification,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Kurye yeni görev bildirimi gönderildi. CourierId={CourierId}, OrderId={OrderId}", 
                    courierId, notification.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye görev bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyOrderAssignedToCourierAsync(int courierId, int orderId, string orderNumber, 
            string deliveryAddress, string? customerPhone, decimal totalAmount, string paymentMethod)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                var notification = new
                {
                    type = "OrderAssigned",
                    orderId,
                    orderNumber,
                    deliveryAddress,
                    customerPhone,
                    totalAmount,
                    paymentMethod,
                    isCod = paymentMethod.Equals("cash_on_delivery", StringComparison.OrdinalIgnoreCase) ||
                            paymentMethod.Equals("kapida_odeme", StringComparison.OrdinalIgnoreCase),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                await _courierHub.Clients.Group(groupName).SendAsync("OrderAssigned", notification);
                
                // Ses bildirimi için ayrı event
                await _courierHub.Clients.Group(groupName).SendAsync("PlaySound", new
                {
                    soundType = "new_order",
                    priority = "high"
                });
                
                _logger.LogInformation(
                    "📢 Kuryeye sipariş atandı bildirimi gönderildi. CourierId={CourierId}, OrderId={OrderId}", 
                    courierId, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Kuryeye sipariş atama bildirimi gönderilemedi. CourierId={CourierId}, OrderId={OrderId}", 
                    courierId, orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyOrderUnassignedFromCourierAsync(int courierId, int orderId, 
            string orderNumber, string reason)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                var notification = new
                {
                    type = "OrderUnassigned",
                    orderId,
                    orderNumber,
                    reason,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                await _courierHub.Clients.Group(groupName).SendAsync("OrderUnassigned", notification);
                
                _logger.LogInformation(
                    "📢 Kuryeden sipariş kaldırıldı bildirimi gönderildi. CourierId={CourierId}, OrderId={OrderId}", 
                    courierId, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Kuryeden sipariş kaldırma bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCourierTaskCancelledAsync(int courierId, int deliveryTaskId, string reason)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("TaskCancelled", new
                {
                    type = "TaskCancelled",
                    deliveryTaskId,
                    reason,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Kurye görev iptali bildirimi gönderildi. CourierId={CourierId}, TaskId={TaskId}", 
                    courierId, deliveryTaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye görev iptali bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCourierSlaWarningAsync(int courierId, int deliveryTaskId, int minutesRemaining)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("SlaWarning", new
                {
                    type = "SlaWarning",
                    deliveryTaskId,
                    minutesRemaining,
                    urgency = minutesRemaining <= 5 ? "critical" : (minutesRemaining <= 15 ? "high" : "normal"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogWarning(
                    "⚠️ Kurye SLA uyarısı gönderildi. CourierId={CourierId}, MinutesRemaining={Minutes}", 
                    courierId, minutesRemaining);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye SLA uyarısı gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCourierStatusChangedAsync(int courierId, string status)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("StatusChanged", new
                {
                    type = "StatusChanged",
                    courierId,
                    status,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                // Admin'lere de bildir
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("CourierStatusChanged", new
                {
                    courierId,
                    status,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Kurye durum değişikliği bildirimi gönderildi. CourierId={CourierId}, Status={Status}", 
                    courierId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye durum bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyOrderUpdatedForCourierAsync(int courierId, int orderId, string updateType, string details)
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("OrderUpdated", new
                {
                    type = "OrderUpdated",
                    orderId,
                    updateType,
                    details,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Kuryeye sipariş güncelleme bildirimi gönderildi. CourierId={CourierId}, OrderId={OrderId}", 
                    courierId, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye sipariş güncelleme bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task SendMessageToCourierAsync(int courierId, string message, string priority = "normal")
        {
            try
            {
                var groupName = $"courier-{courierId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("AdminMessage", new
                {
                    type = "AdminMessage",
                    message,
                    priority,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                // Yüksek öncelikli mesajlar için ses bildirimi
                if (priority == "high" || priority == "urgent")
                {
                    await _courierHub.Clients.Group(groupName).SendAsync("PlaySound", new
                    {
                        soundType = "message",
                        priority
                    });
                }
                
                _logger.LogInformation(
                    "📢 Kuryeye mesaj gönderildi. CourierId={CourierId}, Priority={Priority}", 
                    courierId, priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuryeye mesaj gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        /// <inheritdoc />
        public async Task BroadcastToCouriersAsync(string message, string priority = "normal")
        {
            try
            {
                await _courierHub.Clients.Group("all_couriers").SendAsync("Broadcast", new
                {
                    type = "Broadcast",
                    message,
                    priority,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation("📢 Tüm kuryelere duyuru gönderildi. Priority={Priority}", priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye duyurusu gönderilemedi.");
            }
        }

        #endregion

        #region Admin Bildirimleri

        /// <inheritdoc />
        public async Task NotifyNewOrderAsync(int orderId, string orderNumber, string customerName, 
            decimal totalAmount, int itemCount)
        {
            try
            {
                // ============================================================================
                // YENİ SİPARİŞ BİLDİRİMİ
                // Admin grubundaki tüm bağlı istemcilere gönderilir
                // Debug: Bağlantı durumunu ve gönderilen veriyi logla
                // ============================================================================
                
                _logger.LogInformation(
                    "🔔 [NotifyNewOrderAsync] Başladı. OrderId={OrderId}, OrderNumber={OrderNumber}, CustomerName={CustomerName}, Amount={Amount}, Items={ItemCount}",
                    orderId, orderNumber, customerName, totalAmount, itemCount);

                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "NewOrder",
                    orderId,
                    orderNumber,
                    customerName,
                    totalAmount,
                    itemCount,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                _logger.LogDebug(
                    "📤 [NotifyNewOrderAsync] Admin grubuna 'NewOrder' eventi gönderiliyor. AdminGroupName={GroupName}",
                    AdminGroupName);
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("NewOrder", notification);
                
                _logger.LogDebug(
                    "📤 [NotifyNewOrderAsync] Admin grubuna 'PlaySound' eventi gönderiliyor.");
                
                // Ses bildirimi
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                {
                    soundType = "new_order",
                    priority = "normal"
                });
                
                _logger.LogInformation(
                    "✅ [NotifyNewOrderAsync] Yeni sipariş bildirimi başarıyla gönderildi. OrderId={OrderId}, Amount={Amount}", 
                    orderId, totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [NotifyNewOrderAsync] Yeni sipariş bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyPaymentSuccessAsync(int orderId, string orderNumber, decimal amount, string provider)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "PaymentSuccess",
                    orderId,
                    orderNumber,
                    amount,
                    provider,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PaymentSuccess", notification);
                
                _logger.LogInformation(
                    "📢 Ödeme başarılı bildirimi gönderildi. OrderId={OrderId}, Amount={Amount}", 
                    orderId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme başarılı bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyPaymentFailedAsync(int orderId, string orderNumber, string reason, string provider)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "PaymentFailed",
                    orderId,
                    orderNumber,
                    reason,
                    provider,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PaymentFailed", notification);
                
                // Ödeme hatası için uyarı sesi
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                {
                    soundType = "payment_failed",
                    priority = "high"
                });
                
                _logger.LogWarning(
                    "📢 Ödeme başarısız bildirimi gönderildi. OrderId={OrderId}, Reason={Reason}", 
                    orderId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme başarısız bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyDeliveryProblemToAdminAsync(int orderId, string orderNumber, string problemType, 
            string courierName, string? details = null)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "DeliveryProblem",
                    orderId,
                    orderNumber,
                    problemType,
                    courierName,
                    details,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("DeliveryProblem", notification);
                
                // Teslimat sorunu için acil uyarı sesi
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                {
                    soundType = "delivery_problem",
                    priority = "high"
                });
                
                _logger.LogWarning(
                    "📢 Teslimat sorunu bildirimi gönderildi. OrderId={OrderId}, Problem={Problem}", 
                    orderId, problemType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teslimat sorunu bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyOrderCancelledAsync(int orderId, string orderNumber, string reason, string cancelledBy)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "OrderCancelled",
                    orderId,
                    orderNumber,
                    reason,
                    cancelledBy,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };

                // Admin'e bildirim
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("OrderCancelled", notification);

                // StoreAttendant'a bildirim (market görevlisi de sipariş iptallerini görmeli)
                try
                {
                    await _storeHub.Clients.Group(StoreAttendantHub.GetStoreRoomGroupName())
                        .SendAsync("OrderCancelled", notification);
                }
                catch { /* StoreAttendant bildirimi opsiyonel */ }

                _logger.LogInformation(
                    "📢 Sipariş iptal bildirimi gönderildi. OrderId={OrderId}, CancelledBy={CancelledBy}",
                    orderId, cancelledBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş iptal bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyRefundRequestedAsync(int orderId, string orderNumber, decimal refundAmount, string reason)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "RefundRequested",
                    orderId,
                    orderNumber,
                    refundAmount,
                    reason,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };

                // Admin'e bildirim
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("RefundRequested", notification);

                // StoreAttendant'a bildirim (market görevlisi de iade taleplerini görmeli)
                try
                {
                    await _storeHub.Clients.Group(StoreAttendantHub.GetStoreRoomGroupName())
                        .SendAsync("RefundRequested", notification);
                }
                catch { /* StoreAttendant bildirimi opsiyonel */ }

                _logger.LogInformation(
                    "📢 İade talebi bildirimi gönderildi. OrderId={OrderId}, Amount={Amount}",
                    orderId, refundAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade talebi bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyLowStockAlertAsync(int productId, string productName, int currentStock, int minStock)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "LowStock",
                    productId,
                    productName,
                    currentStock,
                    minStock,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("LowStock", notification);
                
                _logger.LogWarning(
                    "📢 Düşük stok uyarısı gönderildi. ProductId={ProductId}, Stock={Stock}, Min={Min}", 
                    productId, currentStock, minStock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok uyarısı gönderilemedi. ProductId={ProductId}", productId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyAdminAlertAsync(string alertType, string title, string message, string? actionUrl = null)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "AdminAlert",
                    alertType, // info, warning, error, critical
                    title,
                    message,
                    actionUrl,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("AdminAlert", notification);
                
                // Kritik uyarılar için ses
                if (alertType == "error" || alertType == "critical")
                {
                    await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                    {
                        soundType = "alert",
                        priority = alertType == "critical" ? "urgent" : "high"
                    });
                }
                
                _logger.LogInformation(
                    "📢 Admin uyarısı gönderildi. Type={Type}, Title={Title}", 
                    alertType, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin uyarısı gönderilemedi. Title={Title}", title);
            }
        }

        /// <inheritdoc />
        public async Task NotifyAdminsDashboardUpdateAsync(DashboardMetricsNotification metrics)
        {
            try
            {
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("DashboardUpdate", new
                {
                    type = "DashboardUpdate",
                    metrics,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogDebug("📢 Dashboard güncelleme bildirimi gönderildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard güncelleme bildirimi gönderilemedi.");
            }
        }

        /// <inheritdoc />
        public async Task NotifyAdminsSlaViolationAsync(int deliveryTaskId, string orderNumber, DateTime deadline)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "SlaViolation",
                    deliveryTaskId,
                    orderNumber,
                    deadline = deadline.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("SlaViolation", notification);
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                {
                    soundType = "sla_violation",
                    priority = "urgent"
                });
                
                _logger.LogWarning(
                    "📢 SLA ihlali bildirimi gönderildi. TaskId={TaskId}, OrderNumber={OrderNumber}", 
                    deliveryTaskId, orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA ihlali bildirimi gönderilemedi. TaskId={TaskId}", deliveryTaskId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyAdminsDeliveryStuckAsync(int deliveryTaskId, string orderNumber, int stuckMinutes)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "DeliveryStuck",
                    deliveryTaskId,
                    orderNumber,
                    stuckMinutes,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("DeliveryStuck", notification);
                
                _logger.LogWarning(
                    "📢 Takılmış teslimat bildirimi gönderildi. TaskId={TaskId}, StuckMinutes={Minutes}", 
                    deliveryTaskId, stuckMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takılmış teslimat bildirimi gönderilemedi. TaskId={TaskId}", deliveryTaskId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyAdminsCourierOfflineAsync(int courierId, string courierName, int activeTaskCount)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "CourierOffline",
                    courierId,
                    courierName,
                    activeTaskCount,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    read = false
                };
                
                await _adminHub.Clients.Group(AdminGroupName).SendAsync("CourierOffline", notification);
                
                // Aktif görevleri varsa uyarı sesi
                if (activeTaskCount > 0)
                {
                    await _adminHub.Clients.Group(AdminGroupName).SendAsync("PlaySound", new
                    {
                        soundType = "courier_offline",
                        priority = "high"
                    });
                }
                
                _logger.LogWarning(
                    "📢 Kurye offline bildirimi gönderildi. CourierId={CourierId}, ActiveTasks={Tasks}", 
                    courierId, activeTaskCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye offline bildirimi gönderilemedi. CourierId={CourierId}", courierId);
            }
        }

        #endregion

        #region Teslimat Takip Bildirimleri

        /// <inheritdoc />
        public async Task NotifyDeliveryStatusChangedAsync(int deliveryTaskId, string status, string message)
        {
            try
            {
                var groupName = $"delivery_{deliveryTaskId}";
                
                await _orderHub.Clients.Group(groupName).SendAsync("DeliveryStatusChanged", new
                {
                    type = "DeliveryStatusChanged",
                    deliveryTaskId,
                    status,
                    message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Teslimat durumu bildirimi gönderildi. TaskId={TaskId}, Status={Status}", 
                    deliveryTaskId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teslimat durumu bildirimi gönderilemedi. TaskId={TaskId}", deliveryTaskId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyCourierLocationUpdatedAsync(int deliveryTaskId, double latitude, double longitude, double? estimatedMinutes)
        {
            // GPS takibi şimdilik devre dışı, sadece log atıyoruz
            _logger.LogDebug(
                "📍 Kurye konum güncellemesi (GPS takibi devre dışı). TaskId={TaskId}, Lat={Lat}, Lng={Lng}", 
                deliveryTaskId, latitude, longitude);
            
            // İleride aktif edilecek:
            // var groupName = $"delivery_{deliveryTaskId}";
            // await _orderHub.Clients.Group(groupName).SendAsync("CourierLocationUpdated", new { ... });
            
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task NotifyDeliveryCompletedAsync(int deliveryTaskId, string orderNumber, DateTime completedAt)
        {
            try
            {
                var groupName = $"delivery_{deliveryTaskId}";
                
                await _orderHub.Clients.Group(groupName).SendAsync("DeliveryTaskCompleted", new
                {
                    type = "DeliveryTaskCompleted",
                    deliveryTaskId,
                    orderNumber,
                    completedAt = completedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation(
                    "📢 Teslimat tamamlandı bildirimi gönderildi. TaskId={TaskId}, Order={Order}", 
                    deliveryTaskId, orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teslimat tamamlandı bildirimi gönderilemedi. TaskId={TaskId}", deliveryTaskId);
            }
        }

        #endregion

        #region Zone Bildirimleri

        /// <inheritdoc />
        public async Task NotifyZoneCouriersAsync(int zoneId, string message)
        {
            try
            {
                var groupName = $"zone_{zoneId}";
                
                await _courierHub.Clients.Group(groupName).SendAsync("ZoneMessage", new
                {
                    type = "ZoneMessage",
                    zoneId,
                    message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
                
                _logger.LogInformation("📢 Bölge bildirimi gönderildi. ZoneId={ZoneId}", zoneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bölge bildirimi gönderilemedi. ZoneId={ZoneId}", zoneId);
            }
        }

        #endregion

        #region Bağlantı Yönetimi

        /// <summary>
        /// Kullanıcı bağlantısını kaydeder. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void RegisterUserConnection(int userId, string connectionId)
        {
            UserConnections.AddOrUpdate(
                userId,
                new HashSet<string> { connectionId },
                (_, existing) => { existing.Add(connectionId); return existing; });
        }

        /// <summary>
        /// Kullanıcı bağlantısını kaldırır. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void UnregisterUserConnection(int userId, string connectionId)
        {
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                    UserConnections.TryRemove(userId, out _);
            }
        }

        /// <summary>
        /// Kurye bağlantısını kaydeder. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void RegisterCourierConnection(int courierId, string connectionId)
        {
            CourierConnections.AddOrUpdate(
                courierId,
                new HashSet<string> { connectionId },
                (_, existing) => { existing.Add(connectionId); return existing; });
        }

        /// <summary>
        /// Kurye bağlantısını kaldırır. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void UnregisterCourierConnection(int courierId, string connectionId)
        {
            if (CourierConnections.TryGetValue(courierId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                    CourierConnections.TryRemove(courierId, out _);
            }
        }

        /// <summary>
        /// Admin bağlantısını kaydeder. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void RegisterAdminConnection(string connectionId)
        {
            AdminConnections.TryAdd(connectionId, true);
        }

        /// <summary>
        /// Admin bağlantısını kaldırır. Hub'lar tarafından çağrılır.
        /// </summary>
        public static void UnregisterAdminConnection(string connectionId)
        {
            AdminConnections.TryRemove(connectionId, out _);
        }

        /// <inheritdoc />
        public Task<bool> IsUserConnectedAsync(int userId)
        {
            return Task.FromResult(UserConnections.ContainsKey(userId));
        }

        /// <inheritdoc />
        public Task<bool> IsCourierConnectedAsync(int courierId)
        {
            return Task.FromResult(CourierConnections.ContainsKey(courierId));
        }

        /// <inheritdoc />
        public Task<int> GetConnectedAdminCountAsync()
        {
            return Task.FromResult(AdminConnections.Count);
        }

        /// <inheritdoc />
        public Task<int> GetConnectedCourierCountAsync()
        {
            return Task.FromResult(CourierConnections.Count);
        }

        #endregion

        #region Store Attendant (Market Görevlisi) Bildirimleri

        /// <inheritdoc />
        public async Task NotifyStoreAttendantNewOrderAsync(int orderId, string orderNumber, string customerName,
            int itemCount, decimal totalAmount, DateTime confirmedAt)
        {
            try
            {
                // Store Attendant grup adını al (Hub'dan static erişim)
                var groupName = StoreAttendantHub.GetStoreRoomGroupName();
                
                var notification = new
                {
                    type = "NewOrderForStore",
                    orderId,
                    orderNumber,
                    customerName,
                    itemCount,
                    totalAmount,
                    confirmedAt = confirmedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                // Market görevlilerine bildirim gönder
                await _storeHub.Clients.Group(groupName).SendAsync("NewOrderForStore", notification);
                
                // Sesli bildirim tetikle
                await _storeHub.Clients.Group(groupName).SendAsync("PlaySound", new
                {
                    soundType = "new_order",
                    priority = "high"
                });
                
                _logger.LogInformation(
                    "🏪 Market görevlilerine yeni sipariş bildirimi gönderildi. " +
                    "OrderId={OrderId}, OrderNumber={OrderNumber}, ItemCount={ItemCount}",
                    orderId, orderNumber, itemCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Market görevlisi yeni sipariş bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyStoreAttendantOrderConfirmedAsync(int orderId, string orderNumber, 
            string confirmedBy, DateTime confirmedAt)
        {
            try
            {
                var groupName = StoreAttendantHub.GetStoreRoomGroupName();
                
                var notification = new
                {
                    type = "OrderConfirmed",
                    orderId,
                    orderNumber,
                    confirmedBy,
                    confirmedAt = confirmedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                await _storeHub.Clients.Group(groupName).SendAsync("OrderConfirmed", notification);
                
                // Sesli bildirim
                await _storeHub.Clients.Group(groupName).SendAsync("PlaySound", new
                {
                    soundType = "new_order",
                    priority = "high"
                });
                
                _logger.LogInformation(
                    "🏪 Market görevlilerine sipariş onay bildirimi gönderildi. " +
                    "OrderId={OrderId}, ConfirmedBy={ConfirmedBy}",
                    orderId, confirmedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Market görevlisi sipariş onay bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        #endregion

        #region Dispatcher (Sevkiyat Görevlisi) Bildirimleri

        /// <inheritdoc />
        public async Task NotifyDispatcherOrderReadyAsync(int orderId, string orderNumber, string deliveryAddress,
            decimal totalAmount, string paymentMethod, int? weightInGrams, DateTime readyAt)
        {
            try
            {
                var groupName = DispatcherHub.GetDispatchRoomGroupName();
                
                var notification = new
                {
                    type = "OrderReadyForDispatch",
                    orderId,
                    orderNumber,
                    deliveryAddress,
                    totalAmount,
                    paymentMethod,
                    isCod = paymentMethod.Equals("cash_on_delivery", StringComparison.OrdinalIgnoreCase) ||
                            paymentMethod.Equals("kapida_odeme", StringComparison.OrdinalIgnoreCase),
                    weightInGrams,
                    readyAt = readyAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                // Sevkiyat görevlilerine bildirim gönder
                await _dispatcherHub.Clients.Group(groupName).SendAsync("OrderReadyForDispatch", notification);
                
                // Sesli bildirim tetikle
                await _dispatcherHub.Clients.Group(groupName).SendAsync("PlaySound", new
                {
                    soundType = "order_ready",
                    priority = "high"
                });
                
                _logger.LogInformation(
                    "🚚 Sevkiyat görevlilerine sipariş hazır bildirimi gönderildi. " +
                    "OrderId={OrderId}, OrderNumber={OrderNumber}, Address={Address}",
                    orderId, orderNumber, deliveryAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Sevkiyat görevlisi sipariş hazır bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        #endregion

        #region Tüm Taraflara Bildirim (Ortak)

        /// <inheritdoc />
        public async Task NotifyAllPartiesOrderStatusChangedAsync(int orderId, string orderNumber, 
            string oldStatus, string newStatus, string changedBy, int? courierId = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                
                var notification = new
                {
                    type = "OrderStatusChanged",
                    orderId,
                    orderNumber,
                    oldStatus,
                    newStatus,
                    changedBy,
                    timestamp
                };
                
                // 1. Admin'lere bildirim
                await _adminHub.Clients.Group("admin-notifications").SendAsync("OrderStatusChanged", notification);
                
                // 2. Market görevlilerine bildirim
                await _storeHub.Clients.Group(StoreAttendantHub.GetStoreRoomGroupName())
                    .SendAsync("OrderStatusChanged", notification);
                
                // 3. Sevkiyat görevlilerine bildirim (sadece hazır ve sonrası durumlar için)
                var dispatcherStatuses = new[] { "Preparing", "Ready", "Assigned", "PickedUp", "OutForDelivery", "Delivered", "DeliveryFailed", "Cancelled" };
                if (Array.Exists(dispatcherStatuses, s => s.Equals(newStatus, StringComparison.OrdinalIgnoreCase)))
                {
                    await _dispatcherHub.Clients.Group(DispatcherHub.GetDispatchRoomGroupName())
                        .SendAsync("OrderStatusChanged", notification);
                }
                
                // 4. Kuryeye bildirim (atanmışsa)
                if (courierId.HasValue && courierId.Value > 0)
                {
                    var courierGroupName = $"courier-{courierId.Value}";
                    await _courierHub.Clients.Group(courierGroupName).SendAsync("OrderStatusChanged", notification);
                }
                
                // 5. Müşteriye bildirim (sipariş takip sayfası için)
                var orderGroupName = $"order-{orderId}";
                await _orderHub.Clients.Group(orderGroupName).SendAsync("OrderStatusChanged", notification);
                
                _logger.LogInformation(
                    "📢 Tüm taraflara sipariş durum değişikliği bildirildi. " +
                    "OrderId={OrderId}, {OldStatus} → {NewStatus}, ChangedBy={ChangedBy}",
                    orderId, oldStatus, newStatus, changedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Tüm taraflara sipariş durum bildirimi gönderilemedi. OrderId={OrderId}", orderId);
            }
        }

        #endregion

        #region Sesli Bildirim

        /// <inheritdoc />
        public async Task PlaySoundNotificationAsync(string targetGroup, string soundType, string priority = "normal")
        {
            try
            {
                var soundNotification = new
                {
                    soundType,
                    priority,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                // Hedef gruba göre uygun hub'a gönder
                switch (targetGroup.ToLowerInvariant())
                {
                    case "store":
                        await _storeHub.Clients.Group(StoreAttendantHub.GetStoreRoomGroupName())
                            .SendAsync("PlaySound", soundNotification);
                        break;
                        
                    case "dispatch":
                        await _dispatcherHub.Clients.Group(DispatcherHub.GetDispatchRoomGroupName())
                            .SendAsync("PlaySound", soundNotification);
                        break;
                        
                    case "courier":
                        // Tüm kuryelere (genel duyuru)
                        await _courierHub.Clients.Group("all_couriers")
                            .SendAsync("PlaySound", soundNotification);
                        break;
                        
                    case "admin":
                        await _adminHub.Clients.Group("admin-notifications")
                            .SendAsync("PlaySound", soundNotification);
                        break;
                        
                    default:
                        _logger.LogWarning("Bilinmeyen hedef grup: {TargetGroup}", targetGroup);
                        break;
                }
                
                _logger.LogDebug(
                    "🔊 Sesli bildirim gönderildi. TargetGroup={TargetGroup}, SoundType={SoundType}, Priority={Priority}",
                    targetGroup, soundType, priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Sesli bildirim gönderilemedi. TargetGroup={TargetGroup}, SoundType={SoundType}",
                    targetGroup, soundType);
            }
        }

        #endregion
    }
}
