using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Config;
using ECommerce.Core.Interfaces;

//Amaç: Mikro ERP ile stok senkronizasyonunu otomatikleştirmek.
namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class StockSyncJob : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMicroService _microService;
        private CancellationTokenSource? _cts;
        private readonly int _intervalSeconds;
        private readonly IStockUpdatePublisher _stockPublisher;

        public StockSyncJob(
            IServiceScopeFactory scopeFactory,
            IMicroService microService,
            IOptions<InventorySettings> inventoryOptions,
            IStockUpdatePublisher stockPublisher)
        {
            _scopeFactory = scopeFactory;
            _microService = microService;
            _intervalSeconds = Math.Max(10, inventoryOptions.Value.StockSyncIntervalSeconds);
            _stockPublisher = stockPublisher;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = Task.Run(async () =>
            {
                while (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    await SyncStocks();
                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), _cts.Token);
                }
            });

            return Task.CompletedTask;
        }

        private async Task SyncStocks()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                
                var stocks = await _microService.GetStocksAsync();
                foreach (var stock in stocks)
                {
                    var product = await productRepository.GetBySkuAsync(stock.Sku);
                    if (product != null && product.StockQuantity != stock.Quantity)
                    {
                        product.StockQuantity = stock.Quantity;
                        await productRepository.UpdateAsync(product);
                        await _stockPublisher.PublishAsync(product.Id, product.StockQuantity);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }
        public async Task RunOnce()
        {
           await SyncStocks();
        }

    }
}
//	○ BackgroundJobs/StockSyncJob.cs mevcut; burada Hangfire veya Quartz ile planlanmış iş olarak çalıştır.
