using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Data.Context;
using System.Linq;

namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class ReconciliationJob : BackgroundService
    {
        private readonly IServiceProvider _sp;

        public ReconciliationJob(IServiceProvider sp)
        {
            _sp = sp;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once on startup then daily
            await RunOnceAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    await RunOnceAsync(stoppingToken);
                }
                catch (TaskCanceledException) { break; }
                catch { /* swallow to avoid service crash */ }
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

            try
            {
                // Mock provider report pulling: in real integration this would call provider APIs.
                // For now create a simple in-memory check: detect Payments with Status Pending older than 7 days
                var cutoff = DateTime.UtcNow.AddDays(-7);
                var suspect = db.Payments.Where(p => p.Status == "Pending" && p.CreatedAt < cutoff).ToList();
                foreach (var p in suspect)
                {
                    db.ReconciliationLogs.Add(new Entities.Concrete.ReconciliationLog
                    {
                        Provider = p.Provider,
                        ProviderPaymentId = p.ProviderPaymentId,
                        CheckedAt = DateTime.UtcNow,
                        Issue = "Pending longer than 7 days",
                        Details = p.RawResponse
                    });
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch
            {
                // don't propagate
            }
        }
    }
}
