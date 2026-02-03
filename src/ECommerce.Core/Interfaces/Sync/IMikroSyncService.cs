using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// MikroAPI senkronizasyon orkestratör interface'i.
    /// 
    /// NEDEN: Tüm sync servislerini koordine eden üst seviye interface.
    /// Hangfire job'ları ve API controller'ları bu interface'i kullanır.
    /// 
    /// SOLID: Single Responsibility - sadece koordinasyon
    /// </summary>
    public interface IMikroSyncService
    {
        /// <summary>
        /// Tam senkronizasyon çalıştırır (stok, fiyat, cari, sipariş).
        /// KULLANIM: Günlük gece job'ı
        /// </summary>
        Task<SyncResult> RunFullSyncAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Delta senkronizasyon çalıştırır (sadece değişenler).
        /// KULLANIM: 15 dakikalık periyodik job
        /// </summary>
        Task<SyncResult> RunDeltaSyncAsync(DateTime? since = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Senkronizasyon durumunu sorgular.
        /// KULLANIM: Admin dashboard monitoring
        /// </summary>
        Task<SyncStatusReport> GetSyncStatusAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Stok/Ürün senkronizasyon servisi interface'i.
    /// 
    /// AKIŞ:
    /// Mikro → E-Ticaret: Stok miktarları çekilir (mağaza satışları)
    /// E-Ticaret → Mikro: Stok güncellemeleri gönderilir (online satışlar)
    /// </summary>
    public interface IStokSyncService
    {
        /// <summary>
        /// Mikro'dan tüm stokları çeker ve e-ticaret veritabanını günceller.
        /// KULLANIM: İlk kurulum veya günlük tam senkronizasyon.
        /// </summary>
        Task<SyncResult> SyncAllFromMikroAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sadece son senkronizasyondan sonra değişen stokları çeker.
        /// KULLANIM: 15 dakikalık periyodik job için ideal.
        /// </summary>
        Task<SyncResult> SyncDeltaFromMikroAsync(DateTime? since = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tek bir ürünün stoğunu Mikro'ya gönderir.
        /// KULLANIM: Online sipariş sonrası stok düşürme.
        /// </summary>
        Task<SyncResult> PushStockToMikroAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tüm ürün stoklarını Mikro'ya gönderir.
        /// KULLANIM: Stok sayımı sonrası toplu güncelleme.
        /// </summary>
        Task<SyncResult> PushAllStocksToMikroAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sipariş senkronizasyon servisi interface'i.
    /// 
    /// AKIŞ: 
    /// E-Ticaret → Mikro: Online siparişler Mikro'ya aktarılır (TEK YÖNLÜ)
    /// Sipariş onaylandığında otomatik tetiklenir.
    /// </summary>
    public interface ISiparisSyncService
    {
        /// <summary>
        /// Online siparişi Mikro ERP'ye gönderir.
        /// AKIŞ: Cari kontrol → Sipariş kaydı → Stok rezervasyonu
        /// </summary>
        Task<SyncResult> PushOrderToMikroAsync(int orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bekleyen (gönderilmemiş) siparişleri toplu olarak Mikro'ya gönderir.
        /// KULLANIM: Retry job'ı veya manuel tetikleme.
        /// </summary>
        Task<SyncResult> PushPendingOrdersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli tarihten sonra onaylanan siparişleri gönderir.
        /// </summary>
        Task<SyncResult> PushConfirmedOrdersAsync(DateTime since, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cari (Müşteri) senkronizasyon servisi interface'i.
    /// 
    /// AKIŞ:
    /// E-Ticaret → Mikro: Yeni müşteriler sipariş anında Mikro'ya kaydedilir
    /// </summary>
    public interface ICariSyncService
    {
        /// <summary>
        /// Müşteriyi Mikro'da cari hesap olarak oluşturur/günceller.
        /// KOD FORMAT: "ETCMUST{UserId:D6}"
        /// </summary>
        Task<SyncResult> CreateOrUpdateCariAsync(
            int? userId,
            string customerName,
            string email,
            string phone,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// E-ticaret kullanıcısını Mikro carisi olarak senkronize eder.
        /// </summary>
        Task<SyncResult> SyncUserToCariAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Müşterinin Mikro cari kodunu döndürür.
        /// </summary>
        Task<string?> GetMikroCariKodAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tüm kullanıcıları Mikro'ya cari olarak gönderir.
        /// </summary>
        Task<SyncResult> SyncAllUsersToCariAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Fiyat senkronizasyon servisi interface'i.
    /// 
    /// AKIŞ:
    /// Mikro → E-Ticaret: Fiyat değişiklikleri çekilir (TEK YÖNLÜ)
    /// Mikro fiyat listesi master kabul edilir.
    /// </summary>
    public interface IFiyatSyncService
    {
        /// <summary>
        /// Mikro'dan tüm fiyatları çeker ve günceller.
        /// </summary>
        Task<SyncResult> SyncAllFromMikroAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sadece değişen fiyatları çeker (delta sync).
        /// </summary>
        Task<SyncResult> SyncDeltaFromMikroAsync(DateTime? since = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tek bir ürünün fiyatını Mikro'ya gönderir (kampanya için).
        /// </summary>
        Task<SyncResult> PushPriceToMikroAsync(int productId, decimal newPrice, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kampanya fiyatlarını toplu olarak Mikro'ya gönderir.
        /// </summary>
        Task<SyncResult> PushCampaignPricesToMikroAsync(
            IEnumerable<(int ProductId, decimal CampaignPrice, DateTime? StartDate, DateTime? EndDate)> campaignPrices,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Fatura senkronizasyon servisi interface'i.
    /// 
    /// AKIŞ:
    /// E-Ticaret → Mikro: Siparişler faturalaştırılır (TEK YÖNLÜ)
    /// Fatura kesildiğinde Mikro'da stok otomatik düşer.
    /// 
    /// ÖNEMLİ: FaturaKaydetV2 çağrıldığında stok OTOMATIK düşer!
    /// Bu nedenle fatura kesimi sipariş tamamlandığında yapılmalı.
    /// </summary>
    public interface IFaturaSyncService
    {
        /// <summary>
        /// E-ticaret siparişi için Mikro'da fatura keser.
        /// AKIŞ: Order → MikroFaturaKaydetRequestDto → FaturaKaydetV2
        /// </summary>
        Task<SyncResult> CreateInvoiceForOrderAsync(int orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bekleyen (faturası kesilmemiş) siparişleri toplu olarak faturalar.
        /// KULLANIM: Günlük fatura kesimi job'ı.
        /// </summary>
        Task<SyncResult> CreateInvoicesForPendingOrdersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir sipariş için iade faturası keser.
        /// NEDEN: Sipariş iptali veya kısmi iade durumunda.
        /// </summary>
        Task<SyncResult> CreateRefundInvoiceAsync(int orderId, decimal refundAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Siparişin Mikro'da faturası kesilmiş mi kontrol eder.
        /// </summary>
        Task<bool> IsInvoicedAsync(int orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mikro'dan fatura bilgisini getirir.
        /// </summary>
        Task<(string? EvrakSeri, int? EvrakSira, string? EArsivNo)> GetInvoiceDetailsAsync(
            int orderId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Senkronizasyon sonuç raporu.
    /// Factory pattern ile başarılı/başarısız sonuç oluşturulur.
    /// </summary>
    public class SyncResult
    {
        public bool IsSuccess { get; private set; }
        public int ProcessedCount { get; private set; }
        public List<SyncError> Errors { get; private set; } = new();
        public DateTime CompletedAt { get; private set; } = DateTime.UtcNow;

        private SyncResult() { }

        /// <summary>
        /// Başarılı sonuç oluşturur.
        /// </summary>
        public static SyncResult Ok(int processedCount, IEnumerable<SyncError>? warnings = null)
        {
            var result = new SyncResult
            {
                IsSuccess = true,
                ProcessedCount = processedCount
            };
            if (warnings != null)
                result.Errors.AddRange(warnings);
            return result;
        }

        /// <summary>
        /// Hatalı sonuç oluşturur.
        /// </summary>
        public static SyncResult Fail(SyncError error)
        {
            return new SyncResult
            {
                IsSuccess = false,
                ProcessedCount = 0,
                Errors = new List<SyncError> { error }
            };
        }
    }

    /// <summary>
    /// Senkronizasyon hatası detayı.
    /// </summary>
    public class SyncError
    {
        public string Operation { get; set; }
        public string? Identifier { get; set; }
        public string Message { get; set; }
        public string? StackTrace { get; set; }

        public SyncError(string operation, string? identifier, string message, string? stackTrace = null)
        {
            Operation = operation;
            Identifier = identifier;
            Message = message;
            StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Senkronizasyon durum raporu.
    /// Dashboard ve monitoring için.
    /// </summary>
    public class SyncStatusReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<MikroSyncState> SyncStates { get; set; } = new();
        public int Last24HourFailedCount { get; set; }
        public bool IsHealthy { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
