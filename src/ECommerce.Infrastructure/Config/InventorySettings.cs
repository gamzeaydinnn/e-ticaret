namespace ECommerce.Infrastructure.Config
{
    public class InventorySettings
    {
        public int CriticalStockThreshold { get; set; } = 5;
        public int StockSyncIntervalSeconds { get; set; } = 60;
        public int ReservationExpiryMinutes { get; set; } = 15;
        public int ReservationCleanupIntervalSeconds { get; set; } = 60;
    }
}
