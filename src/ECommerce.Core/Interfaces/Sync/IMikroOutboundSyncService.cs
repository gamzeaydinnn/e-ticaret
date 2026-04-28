namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// ECommerce → Mikro yönünde anlık veri gönderimi (outbound push) sözleşmesi.
    /// 
    /// NEDEN: Sipariş sonrası stok düşüşü, admin fiyat değişikliği, ürün güncelleme
    /// gibi olaylar Mikro'ya anında bildirilmeli. Aksi halde Mikro'daki veriler
    /// 15dk+ gecikmeyle güncellenir ve mağaza/online stok tutarsızlığı oluşur.
    /// 
    /// STRATEJİ: Hangfire BackgroundJob.Enqueue ile fire-and-retry.
    /// Başarısız push'lar exponential backoff ile 3 kez denenir.
    /// 3 kez başarısız olursa dead-letter log'a yazılır ve alert üretilir.
    /// </summary>
    public interface IMikroOutboundSyncService
    {
        /// <summary>
        /// Stok değişikliğini Mikro'ya push eder.
        /// KULLANIM: InventoryManager.DecreaseStockAsync / IncreaseStockAsync sonrası.
        /// </summary>
        /// <param name="productId">E-Ticaret ürün ID'si</param>
        /// <param name="newQuantity">Yeni stok miktarı</param>
        /// <param name="changeReason">Değişiklik sebebi (sipariş, iade, sayım vb.)</param>
        Task<OutboundPushResult> PushStockChangeAsync(
            int productId,
            int newQuantity,
            string changeReason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fiyat değişikliğini Mikro'ya push eder.
        /// KULLANIM: ProductManager fiyat güncelleme sonrası.
        /// </summary>
        Task<OutboundPushResult> PushPriceChangeAsync(
            int productId,
            decimal newPrice,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ürün bilgi değişikliğini Mikro'ya push eder (ad, birim, barkod vb.).
        /// KULLANIM: Admin panelden ürün düzenleme sonrası.
        /// </summary>
        Task<OutboundPushResult> PushProductInfoChangeAsync(
            int productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Toplu stok push (ör: stok sayımı sonrası).
        /// Batch'ler halinde gönderir — Mikro API'yi boğmaz.
        /// </summary>
        Task<OutboundPushResult> PushBulkStockChangesAsync(
            IDictionary<int, int> productStockMap,
            string changeReason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bekleyen (başarısız) push'ları tekrar dener.
        /// MikroRetryJob tarafından çağrılır.
        /// </summary>
        Task<OutboundPushResult> RetryFailedPushesAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Outbound push sonuç sınıfı.
    /// </summary>
    public class OutboundPushResult
    {
        public bool Success { get; set; }
        public int PushedCount { get; set; }
        public int FailedCount { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public List<OutboundPushError> Errors { get; set; } = new();

        public static OutboundPushResult Ok(int pushedCount, long durationMs = 0)
            => new() { Success = true, PushedCount = pushedCount, DurationMs = durationMs };

        public static OutboundPushResult Fail(string error, int failedCount = 0, long durationMs = 0)
            => new()
            {
                Success = false,
                ErrorMessage = error,
                FailedCount = failedCount,
                DurationMs = durationMs
            };

        public static OutboundPushResult Partial(int pushed, int failed, long durationMs, List<OutboundPushError>? errors = null)
            => new()
            {
                Success = failed == 0,
                PushedCount = pushed,
                FailedCount = failed,
                DurationMs = durationMs,
                Errors = errors ?? new()
            };
    }

    /// <summary>
    /// Tek bir push hatasının detayı.
    /// </summary>
    public class OutboundPushError
    {
        public int ProductId { get; set; }
        public string? StokKod { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }
}
