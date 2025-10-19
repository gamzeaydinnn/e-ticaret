namespace ECommerce.Infrastructure.Config
{
    public class InventorySettings
    {
        public int CriticalStockThreshold { get; set; } = 5;
        public int StockSyncIntervalSeconds { get; set; } = 60;
    }
}
