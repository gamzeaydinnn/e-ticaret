using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// Mikro ERP için SQL sorgularını oluşturan yardımcı sınıf.
    /// SqlVeriOkuV2 için kullanılmak üzere parametreli sorgu yapısını destekler.
    /// </summary>
    public static class MikroSqlQueryBuilder
    {
        /// <summary>
        /// Tüm web ürünlerini tek bir sorguda çeken birleşik sorguyu oluşturur.
        /// STOK_SATIS_FIYAT_LISTELERI_YONETIM + fn_Stok_Depo_Dagilim + STOKLAR tabloları kullanılır.
        /// </summary>
        public static (string Query, Dictionary<string, object> Parameters) BuildUnifiedProductQuery(
            int? depoNo = null,
            int? fiyatListesiNo = 11,
            string? stokKod = null,
            string? grupKod = null,
            bool? sadeceStoklu = null,
            bool? sadeceAktif = true)
        {
            var parameters = new Dictionary<string, object>();
            
            // Parametre tanımları (Mikro SQL endpoint'ine gönderilecekse veya local dapper vb. için)
            // Not: SqlVeriOkuV2 HTTP endpoint'i parametre kabul etmediği için değerleri güvenli şekilde inline yapıyoruz.
            // Fakat Dapper/ADO.NET kullanılıyorsa parameters dict kullanılabilir.
            
            int hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;
            
            // fn_Stok_Depo_Dagilim fonksiyonu için tarih parametreleri (bu yıl)
            var currentYear = DateTime.Now.Year;
            var startDate = new DateTime(currentYear, 1, 1).ToString("yyyyMMdd");
            var endDate = new DateTime(currentYear, 12, 31).ToString("yyyyMMdd");

            var sb = new StringBuilder();

            sb.AppendLine("WITH CTE_Urunler AS (");
            sb.AppendLine("  SELECT ");
            sb.AppendLine("    Y.msg_S_0001 AS msg_S_0001, -- Stok Kod");
            sb.AppendLine("    Y.msg_S_0005 AS msg_S_0005, -- Urun Adi");
            sb.AppendLine("    Y.msg_S_0002 AS msg_S_0002, -- Fiyat");
            sb.AppendLine("    ISNULL(D.msg_S_0343, 0) AS msg_S_0343, -- Stok Miktar");
            sb.AppendLine("    D.msg_S_1266 AS msg_S_1266, -- Depo Adi");
            sb.AppendLine("    D.msg_S_0873 AS msg_S_0873, -- Depo No");
            sb.AppendLine("    S.sto_webe_gonderilecek_fl AS sto_webe_gonderilecek_fl,");
            sb.AppendLine("    S.sto_birim1_ad AS sto_birim1_ad,");
            sb.AppendLine("    S.sto_grup_kod AS sto_grup_kod,");
            sb.AppendLine("    S.sto_perakende_vergi AS sto_perakende_vergi,");
            sb.AppendLine("    ISNULL(BK.bar_kodu, '') AS bar_kodu,");
            sb.AppendLine("    COUNT(*) OVER() AS toplam_kayit,");
            sb.AppendLine("    ROW_NUMBER() OVER(PARTITION BY Y.msg_S_0001 ORDER BY Y.msg_S_0002 DESC) AS rn");
            sb.AppendLine("  FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM Y");
            sb.AppendLine("  INNER JOIN STOKLAR S ON S.sto_kod = Y.msg_S_0001");
            
            // fn_Stok_Depo_Dagilim parametreleri (depo bazlı stok durumu)
            if (depoNo.HasValue && depoNo.Value > 0)
            {
                sb.AppendLine($"  LEFT JOIN dbo.fn_Stok_Depo_Dagilim('', {depoNo.Value}) D ON D.msg_S_0001 = Y.msg_S_0001");
            }
            else
            {
                // Depo parametresi yoksa tüm depoları kapsayan view veya fonksiyon hali
                sb.AppendLine("  LEFT JOIN dbo.fn_Stok_Depo_Dagilim('', 0) D ON D.msg_S_0001 = Y.msg_S_0001");
            }
            
            sb.AppendLine("  OUTER APPLY (");
            sb.AppendLine("    SELECT TOP 1 bar_kodu FROM BARKOD_TANIMLARI WHERE bar_stokkodu = S.sto_kod");
            sb.AppendLine("  ) BK");
            
            // 🔴 KRİTİK: Liste 11 tek kaynak gerçeklik!
            sb.AppendLine($"  WHERE Y.msg_S_1265 = {hedefListe}");
            
            if (sadeceAktif == true)
            {
                sb.AppendLine("  AND ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1");
                sb.AppendLine("  AND ISNULL(S.sto_iptal, 0) = 0");
            }
            
            if (sadeceStoklu == true)
            {
                sb.AppendLine("  AND ISNULL(D.msg_S_0343, 0) > 0");
            }

            if (!string.IsNullOrWhiteSpace(stokKod))
            {
                // SQL Injection'a karşı güvenli hale getirme (basit escape)
                var safeStokKod = stokKod.Replace("'", "''");
                sb.AppendLine($"  AND Y.msg_S_0001 LIKE '%{safeStokKod}%'");
            }
            
            if (!string.IsNullOrWhiteSpace(grupKod))
            {
                var safeGrupKod = grupKod.Replace("'", "''");
                sb.AppendLine($"  AND S.sto_grup_kod = '{safeGrupKod}'");
            }

            // Bu yıl içinde hareket filtrelemesi istenmişti
            sb.AppendLine($"  AND (S.sto_create_date >= '{currentYear}-01-01' OR S.sto_lastup_date >= '{currentYear}-01-01')");

            sb.AppendLine(")");
            sb.AppendLine("SELECT * FROM CTE_Urunler WHERE rn = 1");
            
            return (sb.ToString(), parameters);
        }

        public static (string Query, Dictionary<string, object> Parameters) BuildSqlStockQuery(int? depoNo)
        {
            var parameters = new Dictionary<string, object>();
            int hedefDepo = depoNo ?? 0;
            
            var sb = new StringBuilder();
            sb.AppendLine("SELECT ");
            sb.AppendLine("  Y.msg_S_0001 AS msg_S_0001,");
            sb.AppendLine("  ISNULL(D.msg_S_0343, 0) AS msg_S_0343");
            sb.AppendLine("FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM Y");
            sb.AppendLine("INNER JOIN STOKLAR S ON S.sto_kod = Y.msg_S_0001");
            sb.AppendLine($"LEFT JOIN dbo.fn_Stok_Depo_Dagilim('', {hedefDepo}) D ON D.msg_S_0001 = Y.msg_S_0001");
            sb.AppendLine("WHERE Y.msg_S_1265 = 11"); // ZORUNLU
            
            return (sb.ToString(), parameters);
        }
        
        public static (string Query, Dictionary<string, object> Parameters) BuildSqlPriceQuery(int? fiyatListesiNo)
        {
            var parameters = new Dictionary<string, object>();
            int hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;
            
            var sb = new StringBuilder();
            sb.AppendLine("SELECT ");
            sb.AppendLine("  Y.msg_S_0001 AS msg_S_0001,");
            sb.AppendLine("  Y.msg_S_0005 AS msg_S_0005,");
            sb.AppendLine("  Y.msg_S_0002 AS msg_S_0002");
            sb.AppendLine("FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM Y");
            sb.AppendLine($"WHERE Y.msg_S_1265 = {hedefListe}");
            sb.AppendLine("  AND Y.msg_S_0002 > 0");
            
            return (sb.ToString(), parameters);
        }
    }
}
