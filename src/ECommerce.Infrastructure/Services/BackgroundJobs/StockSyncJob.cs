using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.BackgroundJobs
{
    public class StockSyncJob
    {
        public async Task Run()
        {
            // Placeholder for stock synchronization with Mikro ERP
            await Task.Delay(1000);
        }
    }
}
