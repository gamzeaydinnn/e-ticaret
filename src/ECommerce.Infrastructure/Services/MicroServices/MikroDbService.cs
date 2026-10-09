using System.Data;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Helpers;
using ECommerce.Infrastructure.Config;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// Mikro ERP MSSQL Server'a DOĞRUDAN SqlConnection ile bağlanan servis.
    ///
    /// TEMEL FARK: Eski akış → HTTP POST → SqlVeriOkuV2 → Mikro API (timeout)
    ///             Yeni akış → SqlConnection → Mikro DB → &lt;2s
    ///
    /// TASARIM KARARLARI:
    /// - Her metod kendi SqlConnection açar/kapatır (stateless, thread-safe).
    /// - SqlParameter ile injection önlenir; raw interpolation yok.
    /// - SqlCommandTimeoutSeconds config'den okunur (varsayılan 30s).
    /// - Bağlantı problemi → log + boş koleksiyon döner (exception yukarı taşımaz).
    /// </summary>
    public class MikroDbService : IMikroDbService
    {
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroDbService> _logger;

        public MikroDbService(
            IOptions<MikroSettings> settings,
            ILogger<MikroDbService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.SqlConnectionString);

        // ==================== BİRLEŞİK ÜRÜN SORGUSU ====================

        /// <inheritdoc/>
        public async Task<List<MikroUnifiedProductDto>> GetUnifiedProductsAsync(
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "[MikroDbService] SqlConnectionString yapılandırılmamış. " +
                    "MikroSettings:SqlConnectionString ayarını doldurun.");
                return [];
            }

            var sql = BuildUnifiedProductQuery(fiyatListesiNo, depoNo);

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                // Bağlantı başarılı — hangi DB'ye bağlandığımızı logla (tanı için kritik)
                _logger.LogInformation(
                    "[MikroDbService] SQL bağlantısı açıldı. Server: {Server}, Database: {Database}",
                    conn.DataSource, conn.Database);

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = _settings.SqlCommandTimeoutSeconds,
                    CommandType = CommandType.Text
                };

                // depoNo parametresi dynamic SQL'e değil, WHERE koşuluna baked-in olarak giriyor
                // (BuildUnifiedProductQuery içinde safely formatlanıyor — sadece int kontrol).
                // Eğer ileride kullanıcı girdisi olursa SqlParameter zorunlu tutulacak.

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var results = new List<MikroUnifiedProductDto>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var stokKod = ReadString(reader, "stokkod", "msg_S_0001");
                    if (string.IsNullOrWhiteSpace(stokKod) || !seen.Add(stokKod))
                        continue;

                    results.Add(new MikroUnifiedProductDto
                    {
                        StokKod         = stokKod,
                        StokAd          = ReadString(reader, "stokad", "msg_S_0005"),
                        Fiyat           = ReadDecimal(reader, "fiyat", "msg_S_0002"),
                        StokMiktar      = ReadDecimal(reader, "stok_miktar", "msg_S_0343"),
                        DepoNo          = ReadNullableInt(reader, "depo_no", "msg_S_0873"),
                        Barkod          = ReadString(reader, "barkod", "bar_kodu"),
                        GrupKod         = ReadString(reader, "grup_kod", "sto_grup_kod"),
                        AnagrupKod      = ReadString(reader, "anagrup_kod", "sto_anagrup_kod"),
                        Birim           = ReadString(reader, "birim", "sto_birim1_ad"),
                        KdvOrani        = ReadDecimal(reader, "kdv_orani", "sto_perakende_vergi"),
                        WebeGonderilecekFl = ReadBool(reader, "webe_gonderilecek_fl", "sto_webe_gonderilecek_fl"),
                        SonHareketTarihi  = ReadNullableDateTime(reader, "son_hareket_tarihi")
                    });
                }

                _logger.LogInformation(
                    "[MikroDbService] Birleşik ürün sorgusu tamamlandı. " +
                    "Toplam: {Count}, Fiyat>0: {PriceOk}, Stok>0: {StockOk}",
                    results.Count,
                    results.Count(p => p.Fiyat > 0),
                    results.Count(p => p.StokMiktar > 0));

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[MikroDbService] Birleşik ürün sorgusu SQL hatası. " +
                    "Number: {Number}, Severity: {Class}",
                    ex.Number, ex.Class);
                return [];
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[MikroDbService] Birleşik ürün sorgusu iptal edildi / timeout ({Timeout}s).",
                    _settings.SqlCommandTimeoutSeconds);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Birleşik ürün sorgusu beklenmeyen hata.");
                return [];
            }
        }

        /// <inheritdoc/>
        public async Task<MikroUnifiedProductDto?> GetProductBySkuAsync(
            string sku,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            var unified = await GetUnifiedProductsAsync(null, null, cancellationToken);
            return MikroWebCatalogFilter.OnlyWebActive(unified)
                .FirstOrDefault(u =>
                    string.Equals(u.StokKod, sku.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // ==================== FİYAT SATIRLARI ====================

        /// <inheritdoc/>
        public async Task<List<MikroFiyatSatirDto>> GetFiyatSatirlariAsync(
            int? fiyatListesiNo = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[MikroDbService] SqlConnectionString yapılandırılmamış.");
                return [];
            }

            var sql = BuildSqlPriceQuery(fiyatListesiNo);

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = _settings.SqlCommandTimeoutSeconds,
                    CommandType = CommandType.Text
                };

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var results = new List<MikroFiyatSatirDto>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    var stokKod = ReadString(reader, "stokkod", "msg_S_0001");
                    if (string.IsNullOrWhiteSpace(stokKod) ||
                        string.Equals(stokKod, "TANIMSIZ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    results.Add(new MikroFiyatSatirDto
                    {
                        Guid            = ReadString(reader, "guid"),
                        StokKod         = stokKod.Trim(),
                        UrunAdi         = ReadString(reader, "stokad", "msg_S_0005"),
                        Fiyat           = ReadDecimal(reader, "fiyat", "msg_S_0002"),
                        Barkod          = ReadString(reader, "barkod"),
                        WebeGonderilecekFl = ReadNullableBool(reader, "webe_gonderilecek_fl")
                    });
                }

                _logger.LogInformation(
                    "[MikroDbService] Fiyat satırları sorgusu tamamlandı. Satır: {Count}",
                    results.Count);

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[MikroDbService] Fiyat satırları SQL hatası. Number: {Number}",
                    ex.Number);
                return [];
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[MikroDbService] Fiyat satırları sorgusu iptal / timeout ({Timeout}s).",
                    _settings.SqlCommandTimeoutSeconds);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Fiyat satırları beklenmeyen hata.");
                return [];
            }
        }

        // ==================== STOK MİKTARLARI ====================

        /// <inheritdoc/>
        public async Task<Dictionary<string, decimal>> GetStokMiktarlariAsync(
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[MikroDbService] SqlConnectionString yapılandırılmamış.");
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }

            var sql = BuildSqlStockQuery(depoNo);

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = _settings.SqlCommandTimeoutSeconds,
                    CommandType = CommandType.Text
                };

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var stokKod = ReadString(reader, "stokkod", "msg_S_0001");
                    if (string.IsNullOrWhiteSpace(stokKod))
                        continue;

                    var miktar = ReadDecimal(reader, "stok_miktar", "msg_S_0343");
                    var normalizedKey = stokKod.Trim();

                    // Aynı stok kod birden fazla satırda gelirse en yüksek miktarı al
                    if (!map.TryGetValue(normalizedKey, out var existing) || existing < miktar)
                        map[normalizedKey] = miktar;
                }

                _logger.LogInformation(
                    "[MikroDbService] Stok miktarları sorgusu tamamlandı. Ürün: {Count}",
                    map.Count);

                return map;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[MikroDbService] Stok miktarları SQL hatası. Number: {Number}",
                    ex.Number);
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "[MikroDbService] Stok miktarları sorgusu iptal / timeout ({Timeout}s).",
                    _settings.SqlCommandTimeoutSeconds);
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Stok miktarları beklenmeyen hata.");
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }
        }

        // ==================== WEB ÜRÜN SAYISI ====================

        /// <inheritdoc/>
        public async Task<int> GetWebProductCountAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[MikroDbService] SqlConnectionString yapılandırılmamış (count sorgusu).");
                return 0;
            }

            // Basit COUNT(*) — web bayrağı + satış fiyatı > 0 (admin/dashboard ile aynı tanım)
            const string sql = @"
                SELECT COUNT(*)
                FROM   STOKLAR S
                WHERE  S.sto_webe_gonderilecek_fl = 1
                  AND  ISNULL(S.sto_iptal, 0) = 0
                  AND  S.sto_kod IS NOT NULL
                  AND  LTRIM(RTRIM(S.sto_kod)) <> ''
                  AND  EXISTS (
                        SELECT 1
                        FROM STOK_SATIS_FIYAT_LISTELERI F
                        WHERE F.sfiyat_stokkod = S.sto_kod
                          AND F.sfiyat_listesirano IN (11, 1)
                          AND F.sfiyat_fiyati > 0
                      )";

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = _settings.SqlCommandTimeoutSeconds,
                    CommandType = CommandType.Text
                };

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var count = Convert.ToInt32(result);

                _logger.LogInformation("[MikroDbService] Web ürün sayısı: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Web ürün sayısı sorgusu hatası.");
                return 0;
            }
        }

        // ==================== WEB FİYAT LİSTESİ HAZIRLAMA ====================

        /// <inheritdoc/>
        public async Task<(int Deleted, int Inserted, int Updated)> PrepareWebPriceListAsync(
            int hedefListeNo = 11,
            int kaynakListeNo = 1,
            int hedefDepoNo = 0,
            CancellationToken cancellationToken = default)
        {
            // ⚠️ MÜŞTERİ TALEBİYLE İPTAL EDİLDİ (Enpos - Liste Ezme Hatası Çözümü)
            // Gamze Hanım'ın onayıyla bu metod devre dışı bırakıldı.
            // Nedeni: Web admin panelinden Liste 11'e girilen özel fiyatları,
            // bu metod SİLİP Liste 1'deki mağaza (Enpos) fiyatlarıyla eziyordu!
            _logger.LogInformation(
                "[MikroDbService] PrepareWebPriceListAsync ÇALIŞTIRILMADI! " +
                "Müşteri onayıyla Liste 11'in mağaza fiyatları tarafından ezilmesi engellendi.");
            
            return await Task.FromResult((0, 0, 0));
        }

        // ==================== SQL SORGU BUILDERları ====================

        /// <summary>
        /// Birleşik ürün sorgusunu oluşturur.
        /// FİYAT: Yalnızca web fiyat listesinden (Liste 11) okunur.
        /// KDV: sto_perakende_vergi Mikro'da GRUP NUMARASI saklar, YÜZDE DEĞİL!
        ///   Grup 0,1 → %0 | Grup 2 → %1 (gıda) | Grup 3,4 → %10 | Grup 5 → %10 | Grup 6 → %20
        /// STOK: STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW üzerinden set-based aggregate ile <2s sürede çekilir.
        /// </summary>
        private static string BuildUnifiedProductQuery(int? fiyatListesiNo, int? depoNo)
        {
            const int hedefListe = 11;
            var hedefDepo  = depoNo.HasValue ? depoNo.Value : 0;

            return $@"SELECT
    S.sto_kod                                     AS stokkod,
    ISNULL(S.sto_isim, '')                        AS stokad,
    -- 1. Öncelik: Liste 11 (Web Fiyatı). Liste 11'de henüz fiyatı olmayan eski ürünler için Fallback: Liste 1
    COALESCE(
        NULLIF(Hedef.sfiyat_fiyati, 0),
        NULLIF(Kaynak.MaxFiyat, 0),
        0
    )                                             AS fiyat,
    ISNULL(ST.stok_miktar, 0)                     AS stok_miktar,
    {hedefDepo}                                   AS depo_no,
    ISNULL(BK.bar_kodu, '')                       AS barkod,
    ISNULL(S.sto_altgrup_kod, '')                 AS grup_kod,
    ISNULL(S.sto_anagrup_kod, '')                 AS anagrup_kod,
    ISNULL(S.sto_birim1_ad, 'ADET')               AS birim,
    CASE ISNULL(S.sto_perakende_vergi, 0)
        WHEN 0 THEN 0
        WHEN 1 THEN 0
        WHEN 2 THEN 1
        WHEN 3 THEN 10
        WHEN 4 THEN 10
        WHEN 5 THEN 10
        WHEN 6 THEN 20
        ELSE 20
    END                                           AS kdv_orani,
    1                                             AS webe_gonderilecek_fl,
    NULL                                          AS son_hareket_tarihi
FROM STOKLAR S
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS sfiyat_fiyati
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = {hedefListe}
      AND  sfiyat_fiyati      > 0
    GROUP BY sfiyat_stokkod
) Hedef ON Hedef.sfiyat_stokkod = S.sto_kod
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
      AND  sfiyat_fiyati      > 0
    GROUP BY sfiyat_stokkod
) Kaynak ON Kaynak.sfiyat_stokkod = S.sto_kod
LEFT JOIN (
    SELECT sth_stok_kod,
           SUM(ISNULL(sth_eldeki_miktar, 0)) AS stok_miktar
    FROM   STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW
    GROUP BY sth_stok_kod
) ST ON ST.sth_stok_kod = S.sto_kod
OUTER APPLY (
    SELECT TOP 1 bar_kodu
    FROM   BARKOD_TANIMLARI
    WHERE  bar_stokkodu = S.sto_kod
) BK
WHERE S.sto_webe_gonderilecek_fl = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        /// <summary>
        /// Fiyat satırları sorgusunu oluşturur (Öncelik Liste 11, Fallback Liste 1).
        /// </summary>
        private static string BuildSqlPriceQuery(int? fiyatListesiNo)
        {
            const int hedefListe = 11;

            return $@"SELECT
    ISNULL(CONVERT(NVARCHAR(36), Hedef.sfiyat_Guid), '00000000-0000-0000-0000-000000000000') AS guid,
    S.sto_kod                                        AS stokkod,
    ISNULL(S.sto_isim, '')                           AS stokad,
    COALESCE(
        NULLIF(Hedef.sfiyat_fiyati, 0),
        NULLIF(Kaynak.MaxFiyat, 0),
        0
    )                                                AS fiyat,
    ISNULL(BK.bar_kodu, '-BARKODYOK-')               AS barkod,
    ISNULL(S.sto_webe_gonderilecek_fl, 0)            AS webe_gonderilecek_fl
FROM STOKLAR S
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON Hedef.sfiyat_stokkod     = S.sto_kod
      AND Hedef.sfiyat_listesirano = {hedefListe}
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
      AND  sfiyat_fiyati      > 0
    GROUP BY sfiyat_stokkod
) Kaynak ON Kaynak.sfiyat_stokkod = S.sto_kod
OUTER APPLY (
    SELECT TOP 1 bar_kodu
    FROM   BARKOD_TANIMLARI
    WHERE  bar_stokkodu = S.sto_kod
) BK
WHERE ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        /// <summary>
        /// Depo bazlı anlık stok miktarı sorgusunu oluşturur.
        /// dbo.fn_TeknikPc_Anlik_Stok_Miktari scalar fonksiyonu kullanılır:
        /// — STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW'dan farklı olarak Enpos (POS) satışları
        ///   anlık olarak stok miktarından düşülür → web sitesi doğru stok gösterir.
        /// </summary>
        private static string BuildSqlStockQuery(int? depoNo)
        {
            var hedefDepo = depoNo.HasValue ? depoNo.Value : 0;

            return $@"SELECT
    S.sto_kod                                                              AS stokkod,
    -- ANLIK STOK: fn_TeknikPc_Anlik_Stok_Miktari Enpos satışlarını düşer.
    ISNULL(dbo.fn_TeknikPc_Anlik_Stok_Miktari(S.sto_kod, {hedefDepo}), 0) AS stok_miktar
FROM STOKLAR S
WHERE ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        // ==================== YARDIMCI OKUYUCULAR ====================
        // SqlDataReader'dan type-safe, null-safe okuma — her alan için ayrı metod.
        // NEDEN: reader[col] direkt kullanımı runtime exception riski taşır.

        private static string ReadString(SqlDataReader reader, params string[] columns)
        {
            foreach(var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (!reader.IsDBNull(ordinal)) return reader.GetString(ordinal).Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    // Continue to next fallback
                }
            }
            return string.Empty;
        }

        private static decimal ReadDecimal(SqlDataReader reader, params string[] columns)
        {
            foreach (var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (!reader.IsDBNull(ordinal)) return Convert.ToDecimal(reader.GetValue(ordinal));
                }
                catch
                {
                    // Continue
                }
            }
            return 0m;
        }

        private static int? ReadNullableInt(SqlDataReader reader, params string[] columns)
        {
            foreach (var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (!reader.IsDBNull(ordinal)) return Convert.ToInt32(reader.GetValue(ordinal));
                }
                catch
                {
                    // Continue
                }
            }
            return null;
        }

        private static bool ReadBool(SqlDataReader reader, params string[] columns)
        {
            foreach (var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (reader.IsDBNull(ordinal)) continue;
                    var val = reader.GetValue(ordinal);
                    return val switch
                    {
                        bool b   => b,
                        int i    => i != 0,
                        byte by  => by != 0,
                        short s  => s != 0,
                        long l   => l != 0,
                        _        => Convert.ToBoolean(val)
                    };
                }
                catch
                {
                    // Continue
                }
            }
            return false;
        }

        private static bool? ReadNullableBool(SqlDataReader reader, params string[] columns)
        {
            foreach (var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (reader.IsDBNull(ordinal)) continue;
                    var val = reader.GetValue(ordinal);
                    return val switch
                    {
                        bool b   => b,
                        int i    => i != 0,
                        byte by  => by != 0,
                        short s  => s != 0,
                        long l   => l != 0,
                        _        => Convert.ToBoolean(val)
                    };
                }
                catch
                {
                    // Continue
                }
            }
            return null;
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, params string[] columns)
        {
            foreach (var column in columns)
            {
                try
                {
                    var ordinal = reader.GetOrdinal(column);
                    if (!reader.IsDBNull(ordinal)) return reader.GetDateTime(ordinal);
                }
                catch
                {
                    // Continue
                }
            }
            return null;
        }

        // ==================== DELTA DEĞİŞİKLİK SORGUSU (HotPoll) ====================

        /// <inheritdoc/>
        public async Task<List<MikroUnifiedProductDto>> GetDeltaChangedProductsAsync(
            DateTime since,
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[MikroDbService] SqlConnectionString yapılandırılmamış (delta sorgusu).");
                return [];
            }

            var sql = BuildDeltaChangedProductQuery(since, fiyatListesiNo, depoNo);

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = new SqlCommand(sql, conn)
                {
                    CommandTimeout = _settings.SqlCommandTimeoutSeconds,
                    CommandType = CommandType.Text
                };

                // Parametrik sorgu — SQL injection riski sıfır
                cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTime) { Value = since });

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var results = new List<MikroUnifiedProductDto>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var stokKod = ReadString(reader, "stokkod");
                    if (string.IsNullOrWhiteSpace(stokKod) || !seen.Add(stokKod))
                        continue;

                    results.Add(new MikroUnifiedProductDto
                    {
                        StokKod              = stokKod,
                        StokAd               = ReadString(reader, "stokad"),
                        Fiyat                = ReadDecimal(reader, "fiyat"),
                        StokMiktar           = ReadDecimal(reader, "stok_miktar"),
                        DepoNo               = ReadNullableInt(reader, "depo_no"),
                        Barkod               = ReadString(reader, "barkod"),
                        GrupKod              = ReadString(reader, "grup_kod"),
                        AnagrupKod           = ReadString(reader, "anagrup_kod"),
                        Birim                = ReadString(reader, "birim"),
                        KdvOrani             = ReadDecimal(reader, "kdv_orani"),
                        WebeGonderilecekFl   = ReadBool(reader, "webe_gonderilecek_fl"),
                        SonHareketTarihi     = ReadNullableDateTime(reader, "son_hareket_tarihi")
                    });
                }

                _logger.LogInformation(
                    "[MikroDbService] Delta sorgusu tamamlandı. Değişen: {Count}, Since: {Since:HH:mm:ss}",
                    results.Count, since);

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[MikroDbService] Delta sorgusu SQL hatası. Number: {Number}, Severity: {Class}",
                    ex.Number, ex.Class);
                return [];
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[MikroDbService] Delta sorgusu iptal edildi / timeout.");
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Delta sorgusu beklenmeyen hata.");
                return [];
            }
        }

        /// <summary>
        /// HotPoll delta sorgusu oluşturur.
        /// 
        /// STRATEJİ: 2 kaynaktan "son değişiklik" tespiti:
        /// 1. STOKLAR.sto_lastup_date >= @since (stok kartı güncelleme)
        /// 2. STOK_HAREKETLERI.sth_tarih >= @since (stok hareketi — satış/alış/iade)
        /// 
        /// FİYAT: Yalnızca web fiyat listesi (Liste 11) üzerinden okunur.
        /// </summary>
        private static string BuildDeltaChangedProductQuery(
            DateTime since, int? fiyatListesiNo, int? depoNo)
        {
            const int hedefListe = 11;
            var hedefDepo  = depoNo.HasValue ? depoNo.Value : 0;

            // @since parametresi CMD üzerinden bağlanır — injection güvenli
            return $@"SELECT
    S.sto_kod                                     AS stokkod,
    ISNULL(S.sto_isim, '')                        AS stokad,
    -- 1. Öncelik: Liste 11 (Web Fiyatı), Fallback: Liste 1 (Eski Ürünler)
    COALESCE(
        NULLIF(Hedef.sfiyat_fiyati, 0),
        NULLIF(Kaynak.MaxFiyat, 0),
        0
    )                                             AS fiyat,
    ISNULL(ST.stok_miktar, 0)                     AS stok_miktar,
    {hedefDepo}                                   AS depo_no,
    ISNULL(BK.bar_kodu, '')                       AS barkod,
    ISNULL(S.sto_altgrup_kod, '')                 AS grup_kod,
    ISNULL(S.sto_anagrup_kod, '')                 AS anagrup_kod,
    ISNULL(S.sto_birim1_ad, 'ADET')               AS birim,
    CASE ISNULL(S.sto_perakende_vergi, 0)
        WHEN 0 THEN 0
        WHEN 1 THEN 0
        WHEN 2 THEN 1
        WHEN 3 THEN 10
        WHEN 4 THEN 10
        WHEN 5 THEN 10
        WHEN 6 THEN 20
        ELSE 20
    END                                           AS kdv_orani,
    1                                             AS webe_gonderilecek_fl,
    (SELECT MAX(H.sth_tarih)
     FROM STOK_HAREKETLERI H
     WHERE H.sth_stok_kod = S.sto_kod)            AS son_hareket_tarihi
FROM STOKLAR S
-- 1. Öncelik: Liste 11 (Web Fiyat Listesi)
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS sfiyat_fiyati
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = {hedefListe}
      AND  sfiyat_fiyati      > 0
    GROUP BY sfiyat_stokkod
) Hedef ON Hedef.sfiyat_stokkod = S.sto_kod
-- 2. Fallback: Liste 1 (Eski Ürünler)
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
      AND  sfiyat_fiyati      > 0
    GROUP BY sfiyat_stokkod
) Kaynak ON Kaynak.sfiyat_stokkod = S.sto_kod
LEFT JOIN (
    SELECT sth_stok_kod,
           SUM(ISNULL(sth_eldeki_miktar, 0)) AS stok_miktar
    FROM   STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW
    GROUP BY sth_stok_kod
) ST ON ST.sth_stok_kod = S.sto_kod
OUTER APPLY (
    SELECT TOP 1 bar_kodu
    FROM   BARKOD_TANIMLARI
    WHERE  bar_stokkodu = S.sto_kod
) BK
WHERE S.sto_webe_gonderilecek_fl = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
  AND (
      S.sto_lastup_date >= @since
      OR
      EXISTS (
          SELECT 1 FROM STOK_HAREKETLERI H
          WHERE  H.sth_stok_kod = S.sto_kod
            AND  H.sth_tarih >= @since
      )
  )
ORDER BY S.sto_kod;";
        }
    }
}
