using ECommerce.Core.DTOs.Micro;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// Mikro ERP MSSQL Server'a DOĞRUDAN bağlanarak stok/fiyat/ürün verisi çeken servis sözleşmesi.
    /// 
    /// NEDEN: /Api/APIMethods/SqlVeriOkuV2 HTTP endpoint'i 90s+ timeout yapıyor.
    /// Bu interface direkt SqlConnection ile aynı sorguları 2s altında çalıştırır.
    /// 
    /// BAĞIMLILIK YÖNELİMİ: MicroService bu interface'e bağımlı olur;
    /// HTTP API çağrıları tamamen bu interface arkasına taşınır.
    /// </summary>
    public interface IMikroDbService
    {
        /// <summary>
        /// Mikro'da mevcut SQL bağlantı string'i yapılandırılmış ve erişilebilir mi?
        /// Bağlantı yoksa tüm metodlar boş koleksiyon döner (exception fırlatmaz).
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Mikro ERP'den BİRLEŞİK tek SQL sorgusuyla tüm web-aktif ürünleri çeker:
        /// stok kodu, stok adı, fiyat, stok miktarı, depo, barkod, grup, birim, KDV, web aktif bayrağı.
        /// 
        /// SQL KAYNAK: BuildUnifiedProductQuery — aynı sorgu, sadece http yerine direkt conn.
        /// </summary>
        Task<List<MikroUnifiedProductDto>> GetUnifiedProductsAsync(
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mikro fiyat listesi satırlarını direkt SQL ile çeker.
        /// SQL KAYNAK: BuildSqlPriceQuery
        /// </summary>
        Task<List<MikroFiyatSatirDto>> GetFiyatSatirlariAsync(
            int? fiyatListesiNo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Depo bazlı stok miktarlarını direkt SQL ile çeker.
        /// SQL KAYNAK: BuildSqlStockQuery
        /// Returns: stokKod → miktar map'i
        /// </summary>
        Task<Dictionary<string, decimal>> GetStokMiktarlariAsync(
            int? depoNo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Son belirtilen tarihten bu yana değişen ürünleri çeker (HotPoll delta sorgusu).
        /// 
        /// NEDEN: 6000+ ürünü her 10sn'de çekmek DB'yi boğar.
        /// Bu metod yalnızca sto_lastup_date + stok hareketleri tablosundaki 
        /// son değişiklikleri filtreler — tipik olarak &lt;50 satır döner.
        /// 
        /// SQL STRATEJİSİ: STOKLAR.sto_lastup_date >= @since OR 
        /// STOK_HAREKETLERI.sth_tarih >= @since 
        /// </summary>
        /// <param name="since">Bu tarihten sonra değişen kayıtlar (UTC)</param>
        /// <param name="fiyatListesiNo">Fiyat listesi numarası</param>
        /// <param name="depoNo">Depo numarası</param>
        Task<List<ECommerce.Core.DTOs.Micro.MikroUnifiedProductDto>> GetDeltaChangedProductsAsync(
            DateTime since,
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mikro ERP'deki web-aktif ürün sayısını hızlı COUNT(*) sorgusuyla döner.
        /// Dashboard istatistikleri için optimize edilmiş — tüm veriyi çekmez.
        /// </summary>
        Task<int> GetWebProductCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Web fiyat listesini hazırlar: hedef listeyi temizler, eksik stokları ekler, kaynak listeden fiyatları kopyalar.
        ///
        /// ADIMLAR:
        /// 1) DELETE — hedef liste (varsayılan 11) tamamen temizlenir (her çalıştırmada sıfırdan doldurulur).
        /// 2) INSERT — sto_webe_gonderilecek_fl = 1 olan stoklar hedef listeye eklenir.
        /// 3) UPDATE — Hedef listedeki fiyatlar, kaynak listeden (varsayılan 1) EN YÜKSEK fiyat ile güncellenir.
        ///
        /// NEDEN: Kaynak liste (1) orijinal Mikro fiyatlarını barındırır; hedef liste (11)
        /// web için hazırlanmış temiz bir kopyadır. SELECT sorguları liste 11'den okur.
        /// </summary>
        /// <returns>Silinen, eklenen ve güncellenen satır sayıları.</returns>
        Task<(int Deleted, int Inserted, int Updated)> PrepareWebPriceListAsync(
            int hedefListeNo = 11,
            int kaynakListeNo = 1,
            int hedefDepoNo = 0,
            CancellationToken cancellationToken = default);
    }
}
