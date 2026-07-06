using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Sipariş iptal edildiğinde kurye atamasını kaldırır ve kuryeyi bilgilendirir.
    /// </summary>
    public class OrderCancellationHandler : IOrderCancellationHandler
    {
        private readonly ECommerceDbContext _db;
        private readonly IRealTimeNotificationService _notificationService;
        private readonly ILogger<OrderCancellationHandler> _logger;

        private static readonly OrderStatus[] NonCancellableDeliveryStatuses =
        {
            OrderStatus.PickedUp,
            OrderStatus.InTransit,
            OrderStatus.OutForDelivery,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Completed
        };

        public OrderCancellationHandler(
            ECommerceDbContext db,
            IRealTimeNotificationService notificationService,
            ILogger<OrderCancellationHandler> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(bool canCancel, string? reason)> CanCancelDeliveryAsync(int orderId)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return (false, "Sipariş bulunamadı.");
            }

            if (NonCancellableDeliveryStatuses.Contains(order.Status))
            {
                return (false, "Kurye paketi teslim aldığı için teslimat görevi otomatik iptal edilemez.");
            }

            return (true, null);
        }

        public async Task<OrderCancellationResult> HandleOrderCancellationAsync(
            int orderId,
            string reason,
            int cancelledByUserId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return new OrderCancellationResult
                {
                    Success = false,
                    Message = "Sipariş bulunamadı.",
                    FailureReason = "ORDER_NOT_FOUND"
                };
            }

            var previousStatus = order.Status.ToString();
            var courierId = order.CourierId;
            var orderNumber = order.OrderNumber ?? $"#{order.Id}";

            if (courierId.HasValue && courierId.Value > 0)
            {
                try
                {
                    await _notificationService.NotifyOrderUnassignedFromCourierAsync(
                        courierId.Value,
                        order.Id,
                        orderNumber,
                        reason);

                    await _notificationService.NotifyCourierTaskCancelledAsync(
                        courierId.Value,
                        order.Id,
                        reason);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Kurye iptal bildirimi gönderilemedi. OrderId={OrderId}, CourierId={CourierId}",
                        orderId, courierId);
                }

                order.CourierId = null;
                order.AssignedAt = null;
                await _db.SaveChangesAsync();
            }

            return new OrderCancellationResult
            {
                Success = true,
                Message = courierId.HasValue
                    ? "Sipariş iptali işlendi ve kurye bilgilendirildi."
                    : "Sipariş iptali işlendi.",
                CancelledDeliveryTaskId = order.Id,
                PreviousStatus = previousStatus,
                NotifiedCourierId = courierId,
                CourierNotified = courierId.HasValue
            };
        }

        public Task<bool> RevertCancellationAsync(int orderId)
        {
            _logger.LogInformation("RevertCancellationAsync çağrıldı ancak desteklenmiyor. OrderId={OrderId}", orderId);
            return Task.FromResult(false);
        }
    }
}
