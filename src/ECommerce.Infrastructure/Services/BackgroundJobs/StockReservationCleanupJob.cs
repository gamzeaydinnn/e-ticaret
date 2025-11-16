using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ECommerce.Data.Context;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class StockReservationCleanupJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InventorySettings _inventorySettings;

        public StockReservationCleanupJob(
            IServiceScopeFactory scopeFactory,
            IOptions<InventorySettings> inventoryOptions)
        {
            _scopeFactory = scopeFactory;
            _inventorySettings = inventoryOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = Math.Max(30, _inventorySettings.ReservationCleanupIntervalSeconds);
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredReservations(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task CleanupExpiredReservations(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var now = DateTime.UtcNow;

            var expiredReservations = await context.StockReservations
                .Where(r => !r.IsReleased && r.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            if (expiredReservations.Count == 0)
            {
                return;
            }

            foreach (var reservation in expiredReservations)
            {
                reservation.IsReleased = true;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
