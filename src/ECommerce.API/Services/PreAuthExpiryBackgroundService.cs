using System;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Services
{
    public class PreAuthExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

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
    }
}