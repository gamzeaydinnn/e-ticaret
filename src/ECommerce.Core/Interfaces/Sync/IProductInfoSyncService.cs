namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Mikro ↔ E-Ticaret arasında ürün bilgi senkronizasyonu sözleşmesi.
    /// 
    /// NEDEN: HotPoll yalnızca stok/fiyat değişikliklerini Product tablosuna yansıtıyor.
    /// İsim, açıklama, kategori, barkod, birim, KDV oranı, ağırlık ve aktif/pasif durumu
    /// gibi bilgi alanları cache'te güncelleniyor ama Product tablosuna yazılmıyor.
    /// Bu servis bu eksik katmanı tamamlar.
    /// 
    /// KULLANIM:
    /// 1. HotPoll InfoChanged tespit ettiğinde → SyncProductInfoFromCacheAsync
    /// 2. Admin "Bilgi Senkronize Et" butonu → SyncAllProductInfoAsync
    /// 3. Kategori eşleme sonrası → SyncProductCategoriesAsync
    /// </summary>
    public interface IProductInfoSyncService
    {
        /// <summary>
        /// Tek bir ürünün bilgilerini MikroProductCache → Product tablosuna senkronize eder.
        /// HotPoll info değişikliği tespit ettiğinde çağrılır.
        /// </summary>
        /// <param name="stokKod">Mikro stok kodu (SKU)</param>
        /// <returns>Güncellenen alan sayısı</returns>
        Task<ProductInfoSyncResult> SyncProductInfoFromCacheAsync(
            string stokKod,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Toplu ürün bilgi senkronizasyonu — HotPoll batch sonrası.
        /// </summary>
        Task<ProductInfoSyncResult> SyncBatchProductInfoAsync(
            IEnumerable<string> stokKodlar,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tüm MikroProductCache → Product bilgi senkronizasyonu.
        /// Admin tetiklemesi veya günlük full sync kullanımı.
        /// </summary>
        Task<ProductInfoSyncResult> SyncAllProductInfoAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kategori eşleme tablosuna göre Product.CategoryId günceller.
        /// MikroCategoryMapping değişikliği sonrası çağrılır.
        /// </summary>
        Task<int> SyncProductCategoriesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cache'deki eşlenmemiş GrupKod değerlerini listeler.
        /// Admin panelinde eksik kategori eşleme uyarısı için.
        /// </summary>
        Task<List<UnmappedGroupInfo>> GetUnmappedGroupCodesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resmi olmayan (image'sız) ürünleri listeler.
        /// Admin panelinde eksik resim uyarısı için.
        /// </summary>
        Task<ImageSyncStatusReport> GetImageSyncStatusAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ADIM 7: Tüm mevcut ürünleri yeniden kategoriler (one-time migration).
        /// 1. Auto-Mapping Engine ile tüm cache grup kodlarını keşfeder + eşler
        /// 2. SyncProductCategoriesAsync ile tüm Product.CategoryId değerlerini günceller
        /// 3. Eşlenemeyenler "Diğer" kategorisine atanır
        /// </summary>
        Task<RecategorizeResult> RecategorizeAllProductsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ADIM 11: Belirli bir kategorideki ürünleri "Diğer" kategorisine taşır.
        /// Kategori silindiğinde/deaktif edildiğinde çağrılır.
        /// NEDEN: Kategori FK bütünlüğü — silinmiş/pasif kategorideki ürünler 
        /// orphan kalmamalı, "Diğer" güvenli limanına taşınmalı.
        /// </summary>
        Task<int> MoveProductsToDigerAsync(
            int categoryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ADIM 11: Belirli bir mapping değişikliğinden etkilenen ürünleri yeniden kategoriler.
        /// Mapping CRUD sonrası çağrılır — sadece etkilenen AnagrupKod'lu ürünler güncellenir.
        /// </summary>
        Task<int> ResyncProductsByAnagrupKodAsync(
            string anagrupKod,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Toplu yeniden kategorileme sonucu.
    /// </summary>
    public class RecategorizeResult
    {
        public bool Success { get; set; }
        public int TotalProducts { get; set; }
        public int CategoriesUpdated { get; set; }
        public int NewMappingsCreated { get; set; }
        public int NewCategoriesCreated { get; set; }
        public int FallbackToDiger { get; set; }
        public int Errors { get; set; }
        public long DurationMs { get; set; }
        public string? Message { get; set; }
        public List<string> ErrorDetails { get; set; } = new();
    }

    /// <summary>
    /// Ürün bilgi senkronizasyon sonucu.
    /// </summary>
    public class ProductInfoSyncResult
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int NamesUpdated { get; set; }
        public int CategoriesUpdated { get; set; }
        public int WeightInfoUpdated { get; set; }
        public int StatusUpdated { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ErrorDetails { get; set; } = new();

        public static ProductInfoSyncResult Ok(int processed, long durationMs) => new()
        {
            Success = true,
            TotalProcessed = processed,
            DurationMs = durationMs
        };

        public static ProductInfoSyncResult Fail(string error) => new()
        {
            Success = false,
            ErrorMessage = error
        };
    }

    /// <summary>
    /// Eşlenmemiş Mikro grup bilgisi — admin panelinde göstermek için.
    /// </summary>
    public class UnmappedGroupInfo
    {
        public string GrupKod { get; set; } = string.Empty;
        /// <summary>
        /// Alt grup kodu (sto_altgrup_kod) — AnagrupKod + AltgrupKod combo eşleme için.
        /// </summary>
        public string? AltgrupKod { get; set; }
        public int ProductCount { get; set; }
        public string? SampleStokKod { get; set; }
        public string? SampleStokAd { get; set; }
    }

    /// <summary>
    /// Resim senkronizasyon durumu raporu.
    /// </summary>
    public class ImageSyncStatusReport
    {
        public int TotalProducts { get; set; }
        public int ProductsWithImages { get; set; }
        public int ProductsWithoutImages { get; set; }
        public decimal CoveragePercent { get; set; }
        public List<MissingImageProduct> MissingImageProducts { get; set; } = new();
    }

    /// <summary>
    /// Resmi eksik olan ürün bilgisi.
    /// </summary>
    public class MissingImageProduct
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
