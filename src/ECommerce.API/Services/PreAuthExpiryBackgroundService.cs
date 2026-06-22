using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Services
{
    public class PreAuthExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan PreAuthValidity = TimeSpan.FromHours(168);
        private static readonly TimeSpan WarningLeadTime = TimeSpan.FromHours(6);
        private const string WarningMarker = "[PREAUTH_EXPIRY_WARNING_SENT]";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PreAuthExpiryBackgroundService> _logger;

        public PreAuthExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PreAuthExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[PreAuthExpiryBackgroundService] Tartılı ürün provizyon temizleme servisi başlatıldı. Aralık: {Interval}",
                CheckInterval);

            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await CancelExpiredPreAuthorizationsAsync(stoppingToken);
                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CancelExpiredPreAuthorizationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await SendUpcomingExpiryWarningsAsync(scope.ServiceProvider, cancellationToken);

                var weightBasedPaymentService = scope.ServiceProvider.GetRequiredService<IWeightBasedPaymentService>();
                var cancelledCount = await weightBasedPaymentService.CancelExpiredPreAuthorizationsAsync(cancellationToken);

                if (cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "[PreAuthExpiryBackgroundService] Süresi dolan provizyonlar temizlendi. Count={Count}",
                        cancelledCount);
                }
                else
                {
                    _logger.LogDebug("[PreAuthExpiryBackgroundService] Süresi dolan provizyon bulunmadı.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PreAuthExpiryBackgroundService] Provizyon temizleme sırasında hata oluştu.");
            }
        }

        private async Task SendUpcomingExpiryWarningsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var db = serviceProvider.GetRequiredService<ECommerceDbContext>();
            var notificationService = serviceProvider.GetRequiredService<IRealTimeNotificationService>();

            var now = DateTime.UtcNow;
            var warningWindowEnd = now.Add(WarningLeadTime);
            var preAuthWindowStart = now.Subtract(PreAuthValidity);
            var preAuthWindowEnd = warningWindowEnd.Subtract(PreAuthValidity);

            var ordersToWarn = await db.Orders
                .Where(o => o.PreAuthDate.HasValue &&
                            !string.IsNullOrEmpty(o.PreAuthHostLogKey) &&
                            (o.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingWeighing ||
                             o.WeightAdjustmentStatus == WeightAdjustmentStatus.Weighed ||
                             o.WeightAdjustmentStatus == WeightAdjustmentStatus.PendingAdminApproval) &&
                            o.PreAuthDate.Value > preAuthWindowStart &&
                            o.PreAuthDate.Value <= preAuthWindowEnd &&
                            (o.DeliveryNotes == null || !o.DeliveryNotes.Contains(WarningMarker)))
                .ToListAsync(cancellationToken);

            foreach (var order in ordersToWarn)
            {
                var expiresAt = order.PreAuthDate!.Value.Add(PreAuthValidity);
                var hoursRemaining = Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalHours));

                await notificationService.NotifyAdminAlertAsync(
                    "warning",
                    $"Provizyon Süresi Doluyor: {order.OrderNumber}",
                    $"Sipariş #{order.Id} için POSNET provizyonu yaklaşık {hoursRemaining} saat içinde dolacak. Teslimat veya manuel müdahale planlanmalı.",
                    $"/admin/orders/{order.Id}");

                // NEDEN: Ayrı bir teknik bayrak alanı olmadığı için aynı uyarının her saat yeniden gitmesini engelliyoruz.
                order.DeliveryNotes = string.IsNullOrWhiteSpace(order.DeliveryNotes)
                    ? $"{WarningMarker} {DateTime.UtcNow:O}"
                    : $"{order.DeliveryNotes}\n{WarningMarker} {DateTime.UtcNow:O}";
            }

            if (ordersToWarn.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "[PreAuthExpiryBackgroundService] Süresi yaklaşan provizyonlar için uyarı gönderildi. Count={Count}",
                    ordersToWarn.Count);
            }
        }
    }
}
