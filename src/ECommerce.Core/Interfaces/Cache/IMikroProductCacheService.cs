using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces.Cache
{
    /// <summary>
    /// Mikro ERP ürün cache yönetim servisi arayüzü.
    /// 
    /// NEDEN CORE'DA: Business katmanındaki Hangfire job'lar (MikroUnifiedSyncJob) bu arayüzü
    /// kullanarak cache'i günceller. API→Core→Business bağımlılık yönünü korumak için
    /// interface Core'da, implementation API'da kalır.
    /// </summary>
    public interface IMikroProductCacheService
    {
        /// <summary>Cache'den sayfalı ürün listesi getirir (local DB)</summary>
        Task<MikroCachePageResult> GetCachedProductsAsync(MikroCacheQuery query);

        /// <summary>Tüm ürünleri Mikro'dan çeker ve cache'e kaydeder</summary>
        Task<MikroCacheSyncResult> FetchAllAndCacheAsync(int fiyatListesiNo = 1, int depoNo = 0, IProgress<MikroFetchProgress>? progress = null);

        /// <summary>Sadece değişen ürünleri günceller (delta sync)</summary>
        Task<MikroCacheSyncResult> SyncChangedProductsAsync(int fiyatListesiNo = 1, int depoNo = 0);

        /// <summary>Yeni ürün varsa senkronizasyon yapar</summary>
        Task<MikroCacheSyncResult> SyncNewProductsOnlyAsync(int fiyatListesiNo = 1, int depoNo = 0);

        /// <summary>Cache istatistiklerini getirir</summary>
        Task<MikroCacheStats> GetCacheStatsAsync();

        /// <summary>Cache'i tamamen temizler</summary>
        Task ClearCacheAsync();

        /// <summary>Tek ürünün aktif/pasif durumunu değiştirir</summary>
        Task<bool> SetProductActiveStatusAsync(string stokKod, bool aktif);

        /// <summary>Tüm cache'deki ürünleri getirir (export için)</summary>
        Task<IEnumerable<MikroProductCache>> GetAllAsync();

        /// <summary>Stok kodlarına göre aktif/pasif durum haritası döndürür</summary>
        Task<Dictionary<string, bool>> GetActiveStatusMapByStokKodAsync(IEnumerable<string> stokKodlar);

        /// <summary>
        /// MikroProductCache tablosundaki güncel verileri Product tablosuna yansıtır.
        /// NEDEN: Frontend, Product tablosundan okur. Cache güncellendiğinde
        /// Product.Price ve Product.StockQuantity da güncellenmelidir.
        /// Aksi halde stok/fiyat 0 olarak görünür.
        /// </summary>
        /// <returns>Güncellenen Product sayısı</returns>
        Task<int> SyncCacheToProductTableAsync();
    }

    /// <summary>Cache sorgu parametreleri</summary>
    public class MikroCacheQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? StokKodFilter { get; set; }
        public string? GrupKodFilter { get; set; }
        public string? SearchTerm { get; set; }
        public bool? SadeceStokluOlanlar { get; set; }
        public bool? SadeceAktif { get; set; } = true; // varsayılan: sadece webe_gonderilecek_fl=1 ürünler
        public string? SortBy { get; set; } = "StokKod";
        public bool SortDescending { get; set; } = false;
    }

    /// <summary>Sayfalı cache sonucu</summary>
    public class MikroCachePageResult
    {
        public List<MikroProductCache> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public DateTime? LastSyncTime { get; set; }
    }

    /// <summary>Sync işlem sonucu</summary>
    public class MikroCacheSyncResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int TotalFetched { get; set; }
        public int NewProducts { get; set; }
        public int UpdatedProducts { get; set; }
        public int DeletedProducts { get; set; }
        public int UnchangedProducts { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>Cache istatistikleri</summary>
    public class MikroCacheStats
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int ProductsWithStock { get; set; }
        public int ProductsWithoutStock { get; set; }
        public int SyncedProducts { get; set; }
        public int NotSyncedProducts { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public DateTime? OldestRecord { get; set; }
        public DateTime? NewestRecord { get; set; }
        public Dictionary<string, int> ProductsByGrupKod { get; set; } = new();
    }

    /// <summary>Fetch ilerleme bilgisi (progress reporting)</summary>
    public class MikroFetchProgress
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int FetchedCount { get; set; }
        public int TotalCount { get; set; }
        public double ProgressPercentage => TotalPages > 0 ? (double)CurrentPage / TotalPages * 100 : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedRemainingTime { get; set; }
        public string Status { get; set; } = "Çekiliyor...";
    }
}
