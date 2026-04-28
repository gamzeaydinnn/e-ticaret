namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Mikro ERP'den anlık delta değişiklikleri tespit eden HotPoll servisi sözleşmesi.
    /// 
    /// NEDEN: 15dk'lık polling aralığı stok/fiyat tutarsızlığına neden oluyor.
    /// Bu servis 10sn aralıklarla yalnızca son değişiklikleri sorguluyor;
    /// tam tablo çekmek yerine delta filtresi (sto_lastup_date) ile DB yükünü minimize ediyor.
    /// 
    /// MİMARİ ROL: IHostedService olarak arka planda sürekli çalışır.
    /// Tespit edilen değişiklikler IStockNotificationService üzerinden
    /// SignalR ile frontend'e iletilir.
    /// </summary>
    public interface IMikroHotPollService
    {
        /// <summary>
        /// Son polling'den bu yana değişen ürünleri çeker ve yerel DB'yi günceller.
        /// </summary>
        /// <returns>Güncellenen kayıt sayısı</returns>
        Task<HotPollResult> PollDeltaChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// HotPoll servisinin son çalışma durumu.
        /// Admin dashboard monitoring için.
        /// </summary>
        HotPollStatus GetStatus();

        /// <summary>
        /// Son başarılı polling zamanı (UTC).
        /// </summary>
        DateTime? LastSuccessfulPollTime { get; }

        /// <summary>
        /// Ardışık hata sayısı. 3+ olduğunda alert tetiklenir.
        /// </summary>
        int ConsecutiveFailureCount { get; }
    }

    /// <summary>
    /// HotPoll sonuç detayı.
    /// </summary>
    public class HotPollResult
    {
        public bool Success { get; set; }
        public int TotalChanged { get; set; }
        public int StockUpdated { get; set; }
        public int PriceUpdated { get; set; }
        public int InfoUpdated { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ProductChangeEvent> Changes { get; set; } = new();

        public static HotPollResult Ok(int stockUpdated, int priceUpdated, int infoUpdated, long durationMs)
            => new()
            {
                Success = true,
                StockUpdated = stockUpdated,
                PriceUpdated = priceUpdated,
                InfoUpdated = infoUpdated,
                TotalChanged = stockUpdated + priceUpdated + infoUpdated,
                DurationMs = durationMs
            };

        public static HotPollResult Fail(string error, long durationMs = 0)
            => new()
            {
                Success = false,
                ErrorMessage = error,
                DurationMs = durationMs
            };
    }

    /// <summary>
    /// Tek bir ürün değişiklik olayı — SignalR bildirimi için.
    /// </summary>
    public class ProductChangeEvent
    {
        public int? LocalProductId { get; set; }
        public string StokKod { get; set; } = string.Empty;
        public ProductChangeType ChangeType { get; set; }

        // Stok değişiklik detayları
        public decimal? OldStockQuantity { get; set; }
        public decimal? NewStockQuantity { get; set; }

        // Fiyat değişiklik detayları
        public decimal? OldPrice { get; set; }
        public decimal? NewPrice { get; set; }

        // Bilgi değişiklik detayları
        public string? OldName { get; set; }
        public string? NewName { get; set; }
    }

    /// <summary>
    /// Ürün değişiklik tipi flag'leri — birden fazla alan aynı anda değişebilir.
    /// </summary>
    [Flags]
    public enum ProductChangeType
    {
        None = 0,
        Stock = 1,
        Price = 2,
        Info = 4,       // İsim, birim, barkod vb.
        KdvRate = 8,
        All = Stock | Price | Info | KdvRate
    }

    /// <summary>
    /// HotPoll durum bilgisi — monitoring dashboard için.
    /// </summary>
    public class HotPollStatus
    {
        public bool IsRunning { get; set; }
        public DateTime? LastPollTime { get; set; }
        public DateTime? LastSuccessTime { get; set; }
        public int ConsecutiveFailures { get; set; }
        public long LastPollDurationMs { get; set; }
        public int LastPollChangedCount { get; set; }
        public string? LastError { get; set; }
    }
}
