using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class StockSyncJob : IHostedService
    {
        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;
        private CancellationTokenSource _cts;

        public StockSyncJob(IMicroService microService, IProductRepository productRepository)
        {
            _microService = microService;
            _productRepository = productRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await SyncStocks();
                    await Task.Delay(60000, _cts.Token); // her 60 saniyede bir çalıştır
                }
            });

            return Task.CompletedTask;
        }

        private async Task SyncStocks()
        {
            var stocks = await _microService.GetStocksAsync();
            foreach (var stock in stocks)
            {
                var product = await _productRepository.GetBySkuAsync(stock.Sku);
                if (product != null)
                {
                    product.StockQuantity = stock.Quantity;
                    await _productRepository.UpdateAsync(product);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }
        public async Task RunOnce()
        {
           await SyncStocks();
        }

    }
}
