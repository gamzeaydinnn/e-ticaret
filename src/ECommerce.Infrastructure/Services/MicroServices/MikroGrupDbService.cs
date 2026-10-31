using System.Data;
using System.Globalization;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Helpers;
using ECommerce.Infrastructure.Config;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// Mikro stok grup tablolarını (STOK_ANA_GRUPLARI / STOK_ALT_GRUPLARI) doğrudan SqlConnection ile okur
    /// ve yalnızca Admin panelinden yeni kategori/alt kategori oluşturulduğunda güvenli INSERT yapar.
    /// </summary>
    public class MikroGrupDbService : IMikroGrupDbService
    {
        private static readonly CultureInfo TrCulture = new("tr-TR");
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroGrupDbService> _logger;

        public MikroGrupDbService(
            IOptions<MikroSettings> settings,
            ILogger<MikroGrupDbService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.SqlConnectionString);

        private const string AnaGrupSql = @"
SELECT san_kod, san_isim
FROM   STOK_ANA_GRUPLARI
WHERE  ISNULL(san_iptal, 0) = 0
  AND  san_kod IS NOT NULL
  AND  LTRIM(RTRIM(san_kod)) <> ''
ORDER BY san_kod;";

        private const string AltGrupSql = @"
SELECT sta_kod, sta_isim, sta_ana_grup_kod
FROM   STOK_ALT_GRUPLARI
WHERE  ISNULL(sta_iptal, 0) = 0
  AND  sta_kod IS NOT NULL
  AND  LTRIM(RTRIM(sta_kod)) <> ''
ORDER BY sta_ana_grup_kod, sta_kod;";

        // Web filtresi, MikroDbService'teki ürün sorgularıyla aynıdır (bayrak=1, sto_iptal=0).
        private const string WebUrunSayisiSql = @"
SELECT ISNULL(S.sto_anagrup_kod, '') AS ana_kod,
       ISNULL(S.sto_altgrup_kod, '') AS alt_kod,
       COUNT(*)                      AS adet
FROM   STOKLAR S
WHERE  ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND  ISNULL(S.sto_iptal, 0) = 0
  AND  S.sto_kod IS NOT NULL
  AND  LTRIM(RTRIM(S.sto_kod)) <> ''
GROUP BY ISNULL(S.sto_anagrup_kod, ''), ISNULL(S.sto_altgrup_kod, '');";

        public async Task<MikroGrupReadResult<MikroAnaGrupDto>> GetAnaGruplariAsync(
            CancellationToken cancellationToken = default)
        {
            return await ReadAsync(
                AnaGrupSql,
                "Ana gruplar",
                reader => new MikroAnaGrupDto
                {
                    Kod = ReadText(reader, 0),
                    Isim = ReadText(reader, 1)
                },
                cancellationToken);
        }

        public async Task<MikroGrupReadResult<MikroAltGrupDto>> GetAltGruplariAsync(
            CancellationToken cancellationToken = default)
        {
            return await ReadAsync(
                AltGrupSql,
                "Alt gruplar",
                reader => new MikroAltGrupDto
                {
                    Kod = ReadText(reader, 0),
                    Isim = ReadText(reader, 1),
                    AnaGrupKod = ReadText(reader, 2)
                },
                cancellationToken);
        }

        public async Task<MikroGrupReadResult<MikroGrupUrunSayisiDto>> GetWebUrunSayilariAsync(
            CancellationToken cancellationToken = default)
        {
            var raw = await ReadAsync(
                WebUrunSayisiSql,
                "Web ürün sayıları",
                reader => new MikroGrupUrunSayisiDto
                {
                    AnaGrupKod = MikroGrupKodHelper.NormalizeAnaKod(ReadText(reader, 0)),
                    AltGrupKod = MikroGrupKodHelper.NormalizeAltKod(ReadText(reader, 1)),
                    WebUrunSayisi = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2))
                },
                cancellationToken);

            if (!raw.Success)
                return raw;

            // "2" ve "02" gibi normalleştirme sonrası aynı anahtara düşen satırları topla.
            raw.Items = raw.Items
                .GroupBy(x => (x.AnaGrupKod, x.AltGrupKod))
                .Select(g => new MikroGrupUrunSayisiDto
                {
                    AnaGrupKod = g.Key.AnaGrupKod,
                    AltGrupKod = g.Key.AltGrupKod,
                    WebUrunSayisi = g.Sum(x => x.WebUrunSayisi)
                })
                .ToList();

            return raw;
        }

        // ==================== MİKRO YAZMA (YALNIZCA ADMİN KATEGORİ EKLEDİĞİNDE) ====================

        public async Task<(bool Success, string? Kod, string? Error)> CreateAnaGrupInMikroAsync(
            string isim,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return (false, null, "Mikro SQL bağlantısı yapılandırılmamış.");
            }

            var normalizedIsim = NormalizeMikroIsim(isim);
            if (string.IsNullOrWhiteSpace(normalizedIsim))
            {
                return (false, null, "Kategori adı boş olamaz.");
            }

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

                await AcquireAppLockAsync(conn, tx, cancellationToken);

                // 1) Aynı isimde yeni düzen (3-4 haneli) ana grup zaten var mı kontrol et (idempotent)
                const string checkExistingByNameSql = @"
SELECT TOP 1 LTRIM(RTRIM(san_kod))
FROM   STOK_ANA_GRUPLARI
WHERE  ISNULL(san_iptal, 0) = 0
  AND  LEN(LTRIM(RTRIM(san_kod))) >= 3
  AND  UPPER(LTRIM(RTRIM(san_isim))) = @Isim;";

                await using (var checkCmd = new SqlCommand(checkExistingByNameSql, conn, tx))
                {
                    checkCmd.Parameters.Add(new SqlParameter("@Isim", SqlDbType.NVarChar, 90) { Value = normalizedIsim });
                    var existingKod = await checkCmd.ExecuteScalarAsync(cancellationToken) as string;
                    if (!string.IsNullOrWhiteSpace(existingKod))
                    {
                        await tx.CommitAsync(cancellationToken);
                        _logger.LogInformation("[MikroGrupDbService] Ana grup zaten Mikro'da mevcut: {Kod} - {Isim}", existingKod, normalizedIsim);
                        return (true, existingKod.Trim(), null);
                    }
                }

                // 2) Sıradaki 100'lük ana grup kodunu hesapla (en az 2300)
                const string maxKodSql = @"
SELECT ISNULL(MAX(TRY_CAST(LTRIM(RTRIM(san_kod)) AS INT)), 2200)
FROM   STOK_ANA_GRUPLARI
WHERE  TRY_CAST(LTRIM(RTRIM(san_kod)) AS INT) >= 100
  AND  TRY_CAST(LTRIM(RTRIM(san_kod)) AS INT) % 100 = 0;";

                int maxKod = 2200;
                await using (var maxCmd = new SqlCommand(maxKodSql, conn, tx))
                {
                    var scalar = await maxCmd.ExecuteScalarAsync(cancellationToken);
                    if (scalar != null && scalar != DBNull.Value)
                    {
                        maxKod = Math.Max(2200, Convert.ToInt32(scalar));
                    }
                }

                int nextKodInt = ((maxKod / 100) + 1) * 100;
                string nextKod = nextKodInt.ToString(CultureInfo.InvariantCulture);

                // 3) Çakışma kontrolü
                const string dupCheckSql = @"SELECT COUNT(1) FROM STOK_ANA_GRUPLARI WHERE LTRIM(RTRIM(san_kod)) = @Kod;";
                await using (var dupCmd = new SqlCommand(dupCheckSql, conn, tx))
                {
                    dupCmd.Parameters.Add(new SqlParameter("@Kod", SqlDbType.NVarChar, 25) { Value = nextKod });
                    var count = Convert.ToInt32(await dupCmd.ExecuteScalarAsync(cancellationToken));
                    if (count > 0)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return (false, null, $"Hesaplanan ana grup kodu ({nextKod}) Mikro'da zaten mevcut.");
                    }
                }

                // 4) Doğrulanmış Mikro V16 şablonu ile INSERT (san_fileid = 11, special1..3 = '')
                const string insertAnaSql = @"
INSERT INTO STOK_ANA_GRUPLARI (
    san_Guid, san_DBCno, san_SpecRECno, san_iptal, san_fileid,
    san_hidden, san_kilitli, san_degisti, san_checksum,
    san_create_user, san_create_date, san_lastup_user, san_lastup_date,
    san_special1, san_special2, san_special3,
    san_kod, san_isim
) VALUES (
    NEWID(), 0, 0, 0, 11,
    0, 0, 0, 0,
    1, GETDATE(), 1, GETDATE(),
    N'', N'', N'',
    @Kod, @Isim
);";

                await using (var insertCmd = new SqlCommand(insertAnaSql, conn, tx))
                {
                    insertCmd.Parameters.Add(new SqlParameter("@Kod", SqlDbType.NVarChar, 25) { Value = nextKod });
                    insertCmd.Parameters.Add(new SqlParameter("@Isim", SqlDbType.NVarChar, 90) { Value = normalizedIsim });

                    var affected = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                    if (affected != 1)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return (false, null, $"STOK_ANA_GRUPLARI INSERT beklenen satır sayısını döndürmedi ({affected}).");
                    }
                }

                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("[MikroGrupDbService] ✅ Yeni Mikro ANA GRUP eklendi: {Kod} - {Isim}", nextKod, normalizedIsim);
                return (true, nextKod, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroGrupDbService] ❌ CreateAnaGrupInMikroAsync hatası ({Isim})", normalizedIsim);
                return (false, null, $"Mikro ana grup yazma hatası: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Kod, string? Error)> CreateAltGrupInMikroAsync(
            string anaGrupKod,
            string isim,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return (false, null, "Mikro SQL bağlantısı yapılandırılmamış.");
            }

            var cleanAnaKod = MikroGrupKodHelper.NormalizeAnaKod(anaGrupKod);
            var normalizedIsim = NormalizeMikroIsim(isim);

            if (string.IsNullOrWhiteSpace(cleanAnaKod))
            {
                return (false, null, "Üst (ana) grup Mikro kodu boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(normalizedIsim))
            {
                return (false, null, "Alt kategori adı boş olamaz.");
            }

            if (!int.TryParse(cleanAnaKod, NumberStyles.Integer, CultureInfo.InvariantCulture, out int anaInt) || anaInt < 100)
            {
                return (false, null, $"Alt grup yalnızca yeni düzen (100+) ana gruplar altına eklenebilir (Verilen: {cleanAnaKod}).");
            }

            try
            {
                await using var conn = new SqlConnection(_settings.SqlConnectionString);
                await conn.OpenAsync(cancellationToken);
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

                await AcquireAppLockAsync(conn, tx, cancellationToken);

                // 1) Ana grup Mikro'da mevcut mu?
                const string parentCheckSql = @"
SELECT COUNT(1)
FROM   STOK_ANA_GRUPLARI
WHERE  ISNULL(san_iptal, 0) = 0
  AND  LTRIM(RTRIM(san_kod)) = @AnaKod;";

                await using (var parentCmd = new SqlCommand(parentCheckSql, conn, tx))
                {
                    parentCmd.Parameters.Add(new SqlParameter("@AnaKod", SqlDbType.NVarChar, 25) { Value = cleanAnaKod });
                    var parentExists = Convert.ToInt32(await parentCmd.ExecuteScalarAsync(cancellationToken)) > 0;
                    if (!parentExists)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return (false, null, $"Üst ana grup ({cleanAnaKod}) Mikro STOK_ANA_GRUPLARI tablosunda bulunamadı.");
                    }
                }

                // 2) Aynı ana grup altında aynı isimli alt grup zaten var mı? (idempotent)
                const string existingAltSql = @"
SELECT TOP 1 LTRIM(RTRIM(sta_kod))
FROM   STOK_ALT_GRUPLARI
WHERE  ISNULL(sta_iptal, 0) = 0
  AND  LTRIM(RTRIM(sta_ana_grup_kod)) = @AnaKod
  AND  UPPER(LTRIM(RTRIM(sta_isim))) = @Isim;";

                await using (var checkCmd = new SqlCommand(existingAltSql, conn, tx))
                {
                    checkCmd.Parameters.Add(new SqlParameter("@AnaKod", SqlDbType.NVarChar, 25) { Value = cleanAnaKod });
                    checkCmd.Parameters.Add(new SqlParameter("@Isim", SqlDbType.NVarChar, 90) { Value = normalizedIsim });
                    var existingAltKod = await checkCmd.ExecuteScalarAsync(cancellationToken) as string;
                    if (!string.IsNullOrWhiteSpace(existingAltKod))
                    {
                        await tx.CommitAsync(cancellationToken);
                        _logger.LogInformation("[MikroGrupDbService] Alt grup zaten Mikro'da mevcut: {AnaKod}/{AltKod} - {Isim}",
                            cleanAnaKod, existingAltKod, normalizedIsim);
                        return (true, existingAltKod.Trim(), null);
                    }
                }

                // 3) Bu ana grup altındaki en büyük alt grup kodunu bul (anaInt + 1 .. anaInt + 99)
                const string maxAltSql = @"
SELECT MAX(TRY_CAST(LTRIM(RTRIM(sta_kod)) AS INT))
FROM   STOK_ALT_GRUPLARI
WHERE  LTRIM(RTRIM(sta_ana_grup_kod)) = @AnaKod
  AND  TRY_CAST(LTRIM(RTRIM(sta_kod)) AS INT) > @MinKod
  AND  TRY_CAST(LTRIM(RTRIM(sta_kod)) AS INT) < @MaxKod;";

                int nextAltInt = anaInt + 1;
                await using (var maxCmd = new SqlCommand(maxAltSql, conn, tx))
                {
                    maxCmd.Parameters.Add(new SqlParameter("@AnaKod", SqlDbType.NVarChar, 25) { Value = cleanAnaKod });
                    maxCmd.Parameters.Add(new SqlParameter("@MinKod", SqlDbType.Int) { Value = anaInt });
                    maxCmd.Parameters.Add(new SqlParameter("@MaxKod", SqlDbType.Int) { Value = anaInt + 100 });

                    var scalar = await maxCmd.ExecuteScalarAsync(cancellationToken);
                    if (scalar != null && scalar != DBNull.Value)
                    {
                        nextAltInt = Convert.ToInt32(scalar) + 1;
                    }
                }

                if (nextAltInt >= anaInt + 100)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return (false, null, $"Ana grup ({cleanAnaKod}) için alt grup kod aralığı ({anaInt + 1}-{anaInt + 99}) dolmuş.");
                }

                string nextAltKod = nextAltInt.ToString(CultureInfo.InvariantCulture);

                // 4) sta_kod çakışma kontrolü
                const string dupAltSql = @"SELECT COUNT(1) FROM STOK_ALT_GRUPLARI WHERE LTRIM(RTRIM(sta_kod)) = @AltKod;";
                await using (var dupCmd = new SqlCommand(dupAltSql, conn, tx))
                {
                    dupCmd.Parameters.Add(new SqlParameter("@AltKod", SqlDbType.NVarChar, 25) { Value = nextAltKod });
                    var count = Convert.ToInt32(await dupCmd.ExecuteScalarAsync(cancellationToken));
                    if (count > 0)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return (false, null, $"Hesaplanan alt grup kodu ({nextAltKod}) Mikro'da zaten kullanılıyor.");
                    }
                }

                // 5) Doğrulanmış Mikro V16 şablonu ile INSERT (sta_fileid = 12, special1..3 = '')
                const string insertAltSql = @"
INSERT INTO STOK_ALT_GRUPLARI (
    sta_Guid, sta_DBCno, sta_SpecRECno, sta_iptal, sta_fileid,
    sta_hidden, sta_kilitli, sta_degisti, sta_checksum,
    sta_create_user, sta_create_date, sta_lastup_user, sta_lastup_date,
    sta_special1, sta_special2, sta_special3,
    sta_kod, sta_isim, sta_ana_grup_kod
) VALUES (
    NEWID(), 0, 0, 0, 12,
    0, 0, 0, 0,
    1, GETDATE(), 1, GETDATE(),
    N'', N'', N'',
    @AltKod, @Isim, @AnaKod
);";

                await using (var insertCmd = new SqlCommand(insertAltSql, conn, tx))
                {
                    insertCmd.Parameters.Add(new SqlParameter("@AltKod", SqlDbType.NVarChar, 25) { Value = nextAltKod });
                    insertCmd.Parameters.Add(new SqlParameter("@Isim", SqlDbType.NVarChar, 90) { Value = normalizedIsim });
                    insertCmd.Parameters.Add(new SqlParameter("@AnaKod", SqlDbType.NVarChar, 25) { Value = cleanAnaKod });

                    var affected = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                    if (affected != 1)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return (false, null, $"STOK_ALT_GRUPLARI INSERT beklenen satır sayısını döndürmedi ({affected}).");
                    }
                }

                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("[MikroGrupDbService] ✅ Yeni Mikro ALT GRUP eklendi: {AnaKod}/{AltKod} - {Isim}",
                    cleanAnaKod, nextAltKod, normalizedIsim);
                return (true, nextAltKod, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroGrupDbService] ❌ CreateAltGrupInMikroAsync hatası ({AnaKod} / {Isim})", cleanAnaKod, normalizedIsim);
                return (false, null, $"Mikro alt grup yazma hatası: {ex.Message}");
            }
        }

        private static async Task AcquireAppLockAsync(SqlConnection conn, SqlTransaction tx, CancellationToken ct)
        {
            const string lockSql = @"
DECLARE @res INT;
EXEC @res = sp_getapplock
    @Resource = N'GolkoyMikroGrupYazma',
    @LockMode = N'Exclusive',
    @LockOwner = N'Transaction',
    @LockTimeout = 10000;
IF @res < 0
    THROW 50001, N'Mikro grup yazma kilidi (sp_getapplock) alınamadı.', 1;";

            await using var cmd = new SqlCommand(lockSql, conn, tx);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static string NormalizeMikroIsim(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var cleaned = string.Join(' ', raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpper(TrCulture);

            return cleaned.Length > 90 ? cleaned[..90].Trim() : cleaned;
        }

        // ==================== ORTAK OKUMA ====================

        private async Task<MikroGrupReadResult<T>> ReadAsync<T>(
            string sql,
            string label,
            Func<SqlDataReader, T> map,
            CancellationToken cancellationToken)
        {
            var result = new MikroGrupReadResult<T>();

            if (!IsConfigured)
            {
                result.Error = "MikroSettings:SqlConnectionString yapılandırılmamış.";
                _logger.LogWarning("[MikroGrupDbService] {Label}: {Error}", label, result.Error);
                return result;
            }

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
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Items.Add(map(reader));
                }

                result.Success = true;
                _logger.LogInformation("[MikroGrupDbService] {Label} okundu. Satır: {Count}", label, result.Items.Count);
                return result;
            }
            catch (SqlException ex)
            {
                result.Error = $"SQL hatası ({ex.Number}): {ex.Message}";
                _logger.LogError(ex, "[MikroGrupDbService] {Label} SQL hatası. Number: {Number}", label, ex.Number);
                result.Items.Clear();
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Error = "Sorgu iptal edildi veya zaman aşımına uğradı.";
                _logger.LogWarning("[MikroGrupDbService] {Label} iptal / timeout ({Timeout}s).", label, _settings.SqlCommandTimeoutSeconds);
                result.Items.Clear();
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Beklenmeyen hata: {ex.Message}";
                _logger.LogError(ex, "[MikroGrupDbService] {Label} beklenmeyen hata.", label);
                result.Items.Clear();
                return result;
            }
        }

        private static string ReadText(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal))?.Trim() ?? string.Empty;
        }
    }
}
