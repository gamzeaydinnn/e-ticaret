using System.Data;
using ECommerce.Core.DTOs.Micro;
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
                    var stokKod = ReadString(reader, "stokkod");
                    if (string.IsNullOrWhiteSpace(stokKod) || !seen.Add(stokKod))
                        continue;

                    results.Add(new MikroUnifiedProductDto
                    {
                        StokKod         = stokKod,
                        StokAd          = ReadString(reader, "stokad"),
                        Fiyat           = ReadDecimal(reader, "fiyat"),
                        StokMiktar      = ReadDecimal(reader, "stok_miktar"),
                        DepoNo          = ReadNullableInt(reader, "depo_no"),
                        Barkod          = ReadString(reader, "barkod"),
                        GrupKod         = ReadString(reader, "grup_kod"),
                        AnagrupKod      = ReadString(reader, "anagrup_kod"),
                        Birim           = ReadString(reader, "birim"),
                        KdvOrani        = ReadDecimal(reader, "kdv_orani"),
                        WebeGonderilecekFl = ReadBool(reader, "webe_gonderilecek_fl"),
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
                    var stokKod = ReadString(reader, "stokkod");
                    if (string.IsNullOrWhiteSpace(stokKod) ||
                        string.Equals(stokKod, "TANIMSIZ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    results.Add(new MikroFiyatSatirDto
                    {
                        Guid            = ReadString(reader, "guid"),
                        StokKod         = stokKod.Trim(),
                        UrunAdi         = ReadString(reader, "stokad"),
                        Fiyat           = ReadDecimal(reader, "fiyat"),
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
                    var stokKod = ReadString(reader, "stokkod");
                    if (string.IsNullOrWhiteSpace(stokKod))
                        continue;

                    var miktar = ReadDecimal(reader, "stok_miktar");
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

            // Basit COUNT(*) — tüm veriyi çekmekten çok daha verimli
            const string sql = @"
                SELECT COUNT(*)
                FROM   STOKLAR
                WHERE  sto_webe_gonderilecek_fl = 1
                  AND  ISNULL(sto_iptal, 0) = 0
                  AND  sto_kod IS NOT NULL
                  AND  LTRIM(RTRIM(sto_kod)) <> ''";

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
            if (!IsConfigured)
            {
                _logger.LogWarning("[MikroDbService] SqlConnectionString yapılandırılmamış (fiyat hazırlama).");
                return (0, 0, 0);
            }

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);

                // Transaction ile atomik işlem — ya hepsi başarılı olur ya hiçbiri
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(cancellationToken);

                try
                {
                    // ── ADIM 1: Hedef listeyi (11) tamamen temizle — sıfırdan doldurulacak ──
                    const string deleteSql = @"
                        DELETE FROM STOK_SATIS_FIYAT_LISTELERI
                        WHERE sfiyat_listesirano = @HedefListeNo;";

                    await using var deleteCmd = new SqlCommand(deleteSql, conn, tx)
                    {
                        CommandTimeout = _settings.SqlCommandTimeoutSeconds
                    };
                    deleteCmd.Parameters.Add(new SqlParameter("@HedefListeNo", SqlDbType.Int) { Value = hedefListeNo });

                    var deleted = await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
                    _logger.LogInformation(
                        "[MikroDbService] Liste {HedefListe} temizlendi. Silinen: {Deleted}",
                        hedefListeNo, deleted);

                    // ── ADIM 2: Web-aktif stokları hedef listeye ekle ──
                    // DELETE sonrası liste boş olduğu için tüm web-aktif stoklar eklenir
                    const string insertSql = @"
                        INSERT INTO STOK_SATIS_FIYAT_LISTELERI (
                            sfiyat_DBCno, sfiyat_SpecRECno, sfiyat_iptal, sfiyat_fileid,
                            sfiyat_hidden, sfiyat_kilitli, sfiyat_degisti, sfiyat_checksum,
                            sfiyat_create_user, sfiyat_create_date,
                            sfiyat_lastup_user, sfiyat_lastup_date,
                            sfiyat_special1, sfiyat_special2, sfiyat_special3,
                            sfiyat_stokkod, sfiyat_listesirano, sfiyat_deposirano,
                            sfiyat_odemeplan, sfiyat_birim_pntr, sfiyat_fiyati,
                            sfiyat_doviz, sfiyat_iskontokod, sfiyat_deg_nedeni,
                            sfiyat_primyuzdesi, sfiyat_kampanyakod, sfiyat_doviz_kuru
                        )
                        SELECT
                            0, 0, 0, 228, 0, 0, 0, 0,
                            1, GETDATE(), 1, GETDATE(),
                            '', '', '',
                            s.sto_kod,
                            @HedefListeNo,
                            @HedefDepoNo,
                            0, 1, 0.0,
                            0, '', '',
                            0, '', 0
                        FROM STOKLAR s
                        WHERE s.sto_webe_gonderilecek_fl = 1;";

                    await using var insertCmd = new SqlCommand(insertSql, conn, tx)
                    {
                        CommandTimeout = _settings.SqlCommandTimeoutSeconds
                    };
                    insertCmd.Parameters.Add(new SqlParameter("@HedefListeNo", SqlDbType.Int) { Value = hedefListeNo });
                    insertCmd.Parameters.Add(new SqlParameter("@HedefDepoNo", SqlDbType.Int) { Value = hedefDepoNo });

                    var inserted = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                    _logger.LogInformation(
                        "[MikroDbService] Web-aktif stoklar listeye eklendi. Eklenen: {Inserted}, Hedef: {Hedef}, Depo: {Depo}",
                        inserted, hedefListeNo, hedefDepoNo);

                    // ── ADIM 3: Hedef listedeki fiyatları KAYNAK listeden (1) MAX ile güncelle ──
                    // NEDEN: Kaynak liste (1) Mikro'daki orijinal fiyatları barındırır.
                    // Bir stok kodunun birden fazla satırı olabilir → MAX ile en yüksek fiyat alınır.
                    const string updateSql = @"
                        UPDATE f_hedef
                        SET
                            f_hedef.sfiyat_fiyati      = ISNULL(f_kaynak.MaxFiyat, 0),
                            f_hedef.sfiyat_lastup_date = GETDATE()
                        FROM STOK_SATIS_FIYAT_LISTELERI f_hedef
                        INNER JOIN (
                            SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
                            FROM STOK_SATIS_FIYAT_LISTELERI
                            WHERE sfiyat_listesirano = @KaynakListeNo
                              AND sfiyat_deposirano  = 1   -- Yalnızca Depo 1 (ana perakende deposu)
                            GROUP BY sfiyat_stokkod
                        ) f_kaynak ON f_hedef.sfiyat_stokkod = f_kaynak.sfiyat_stokkod
                        INNER JOIN STOKLAR s ON s.sto_kod = f_hedef.sfiyat_stokkod
                        WHERE f_hedef.sfiyat_listesirano = @HedefListeNo
                          AND f_hedef.sfiyat_deposirano  = @HedefDepoNo
                          AND s.sto_webe_gonderilecek_fl = 1;";

                    await using var updateCmd = new SqlCommand(updateSql, conn, tx)
                    {
                        CommandTimeout = _settings.SqlCommandTimeoutSeconds
                    };
                    updateCmd.Parameters.Add(new SqlParameter("@HedefListeNo", SqlDbType.Int) { Value = hedefListeNo });
                    updateCmd.Parameters.Add(new SqlParameter("@KaynakListeNo", SqlDbType.Int) { Value = kaynakListeNo });
                    updateCmd.Parameters.Add(new SqlParameter("@HedefDepoNo", SqlDbType.Int) { Value = hedefDepoNo });

                    var updated = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                    _logger.LogInformation(
                        "[MikroDbService] Fiyatlar güncellendi. Güncellenen: {Updated}", updated);

                    await tx.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "[MikroDbService] Web fiyat listesi hazırlama tamamlandı. " +
                        "Silinen: {Deleted}, Eklenen: {Inserted}, Güncellenen: {Updated}",
                        deleted, inserted, updated);

                    return (deleted, inserted, updated);
                }
                catch
                {
                    // Hata durumunda tüm işlemleri geri al — atomiklik garantisi
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "[MikroDbService] Web fiyat listesi hazırlama SQL hatası. Number: {Number}, Severity: {Class}",
                    ex.Number, ex.Class);
                return (0, 0, 0);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[MikroDbService] Web fiyat listesi hazırlama iptal edildi / timeout.");
                return (0, 0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDbService] Web fiyat listesi hazırlama beklenmeyen hata.");
                return (0, 0, 0);
            }
        }

        // ==================== SQL SORGU BUILDERları ====================
        // Tüm SELECT sorguları hedef liste 2'den doğrudan okur.
        // NEDEN: PrepareWebPriceListAsync ile veri önceden hazırlandığı için
        // fallback JOIN (diğer listelerden MAX) artık gereksiz — tek kaynak, temiz sorgu.

        /// <summary>
        /// Birleşik ürün sorgusunu oluşturur.
        ///
/// FİYAT: PrepareWebPriceListAsync ile liste 11 önceden doldurulduğu için
    /// doğrudan hedef listeden okunur — fallback JOIN kaldırıldı.
    ///
    /// KDV: sto_perakende_vergi Mikro'da GRUP NUMARASI saklar, YÜZDE DEĞİL!
    ///   Grup 0,1 → %0 | Grup 2 → %1 (gıda) | Grup 3,4 → %10 | Grup 5 → %10 | Grup 6 → %20
    /// </summary>
        private static string BuildUnifiedProductQuery(int? fiyatListesiNo, int? depoNo)
        {
            // NEDEN: MicroService.PrepareWebPriceListAsync liste 11'e yazar,
            // null geçildiğinde de 11'den oku — eskiden 2'ydi, fiyat 0 döndürüyordu.
            var hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;
            var hedefDepo  = depoNo.HasValue ? depoNo.Value : 0;

            return $@"SELECT
    S.sto_kod                                     AS stokkod,
    ISNULL(S.sto_isim, '')                        AS stokad,
    -- NEDEN: COALESCE ile çoklu fiyat listesi fallback.
    -- Önce hedef liste (11), yoksa/0 ise kaynak liste (1), son çare 0.
    -- PrepareWebPriceListAsync çalışmamış bile olsa fiyatlar gelir.
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
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON  Hedef.sfiyat_stokkod     = S.sto_kod
       AND Hedef.sfiyat_listesirano = {hedefListe}
       AND Hedef.sfiyat_deposirano  = {hedefDepo}
-- Fallback: Orijinal fiyat listesi (1) — PrepareWebPriceListAsync çalışmadıysa buradan oku
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
      AND  sfiyat_deposirano  = 1   -- Yalnızca Depo 1 (ana perakende deposu)
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
        /// Fiyat satırları sorgusunu oluşturur.
        /// PrepareWebPriceListAsync ile liste 11 önceden doldurulduğu için tek kaynak okunur.
        /// </summary>
        private static string BuildSqlPriceQuery(int? fiyatListesiNo)
        {
            // NEDEN: PrepareWebPriceListAsync liste 11'e yazar — default 2 uyumsuzdu
            var hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;

            return $@"SELECT
    ISNULL(CONVERT(NVARCHAR(36), Hedef.sfiyat_Guid), '00000000-0000-0000-0000-000000000000') AS guid,
    S.sto_kod                                        AS stokkod,
    ISNULL(S.sto_isim, '')                           AS stokad,
    ISNULL(Hedef.sfiyat_fiyati, 0)                   AS fiyat,
    ISNULL(BK.bar_kodu, '-BARKODYOK-')               AS barkod,
    ISNULL(S.sto_webe_gonderilecek_fl, 0)            AS webe_gonderilecek_fl
FROM STOKLAR S
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON  Hedef.sfiyat_stokkod     = S.sto_kod
       AND Hedef.sfiyat_listesirano = {hedefListe}
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
        /// Depo bazlı stok miktarı sorgusunu oluşturur.
        /// STOKLAR → STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW ile doğrudan web-aktif ürün stoğu.
        /// GÖLKÖY ŞUBE DEPO ve yıl bazlı filtreler kaldırıldı.
        /// </summary>
        private static string BuildSqlStockQuery(int? depoNo)
        {
            // depoNo filtresi: kullanıcı belirli bir depo isterse uygulanır
            // Not: STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW depo kolonu yapısına bağlı
            _ = depoNo; // şimdilik kullanılmıyor — view toplam stok döner

            return @"SELECT
    S.sto_kod                                    AS stokkod,
    ISNULL(ST.stok_miktar, 0)                    AS stok_miktar
FROM STOKLAR S
LEFT JOIN (
    SELECT sth_stok_kod,
           SUM(ISNULL(sth_eldeki_miktar, 0)) AS stok_miktar
    FROM   STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW
    GROUP BY sth_stok_kod
) ST ON ST.sth_stok_kod = S.sto_kod
WHERE ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        // ==================== YARDIMCI OKUYUCULAR ====================
        // SqlDataReader'dan type-safe, null-safe okuma — her alan için ayrı metod.
        // NEDEN: reader[col] direkt kullanımı runtime exception riski taşır.

        private static string ReadString(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).Trim();
            }
            catch (IndexOutOfRangeException)
            {
                // Sütun yoksa boş döndür — schema değişikliğine karşı toleranslı
                return string.Empty;
            }
        }

        private static decimal ReadDecimal(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return 0m;

                // MSSQL farklı numeric tipler dönebilir — Convert ile güvenli çevrim
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch
            {
                return 0m;
            }
        }

        private static int? ReadNullableInt(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return null;
                return Convert.ToInt32(reader.GetValue(ordinal));
            }
            catch
            {
                return null;
            }
        }

        private static bool ReadBool(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return false;
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
                return false;
            }
        }

        private static bool? ReadNullableBool(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return null;
                return ReadBool(reader, column);
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return null;
                return reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
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
        /// FİYAT: PrepareWebPriceListAsync ile liste 11 önceden doldurulduğu için doğrudan okunur.
        /// </summary>
        private static string BuildDeltaChangedProductQuery(
            DateTime since, int? fiyatListesiNo, int? depoNo)
        {
            // NEDEN: PrepareWebPriceListAsync liste 11'e yazar — default 2 uyumsuzdu
            var hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;
            var hedefDepo  = depoNo.HasValue ? depoNo.Value : 0;

            // @since parametresi CMD üzerinden bağlanır — injection güvenli
            return $@"SELECT
    S.sto_kod                                     AS stokkod,
    ISNULL(S.sto_isim, '')                        AS stokad,
    -- Fiyat fallback: önce hedef liste (11), yoksa kaynak liste (1)
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
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON  Hedef.sfiyat_stokkod     = S.sto_kod
       AND Hedef.sfiyat_listesirano = {hedefListe}
       AND Hedef.sfiyat_deposirano  = {hedefDepo}
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
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
