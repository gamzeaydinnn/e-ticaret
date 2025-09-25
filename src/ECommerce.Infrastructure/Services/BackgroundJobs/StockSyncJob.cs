
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class StockSyncJob : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Başlangıç işlemleri
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Stok senkronizasyon işlemi
                    await Task.Delay(1000, cancellationToken);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Servis dururken yapılacak işlemler
            return Task.CompletedTask;
        }
    }
}
