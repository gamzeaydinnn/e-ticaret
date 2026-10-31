using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Mikro STOK_ANA_GRUPLARI satırı (salt okunur).
    /// Kod/isim alanları Mikro'daki değerlerdir, Trim edilmiş olarak gelir.
    /// </summary>
    public class MikroAnaGrupDto
    {
        /// <summary>san_kod (örn. "100", "500", eski düzende "06").</summary>
        public string Kod { get; set; } = string.Empty;

        /// <summary>san_isim (örn. "İÇECEKLER").</summary>
        public string Isim { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mikro STOK_ALT_GRUPLARI satırı (salt okunur).
    /// Eski düzende alt grup kodları farklı ana gruplarda tekrar eder,
    /// bu yüzden alt grup kimliği daima (AnaGrupKod + Kod) bileşiğidir.
    /// </summary>
    public class MikroAltGrupDto
    {
        /// <summary>sta_kod (örn. "101", eski düzende "01").</summary>
        public string Kod { get; set; } = string.Empty;

        /// <summary>sta_isim (örn. "ÇAY").</summary>
        public string Isim { get; set; } = string.Empty;

        /// <summary>sta_ana_grup_kod. Bazı eski kayıtlarda kod yerine isim yazılı olabilir (veri kirliliği).</summary>
        public string AnaGrupKod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Web'e açık (sto_webe_gonderilecek_fl = 1, sto_iptal = 0) ürünlerin ana/alt grup bazında sayısı.
    /// Yalnızca admin ekranında gösterilir, vitrinde gösterilmez.
    /// </summary>
    public class MikroGrupUrunSayisiDto
    {
        public string AnaGrupKod { get; set; } = string.Empty;
        public string AltGrupKod { get; set; } = string.Empty;
        public int WebUrunSayisi { get; set; }
    }

    /// <summary>
    /// Mikro okuma sonucu. Başarısız okuma ile gerçekten boş sonucu ayırt etmek için kullanılır;
    /// senkron servisi başarısız okumada hiçbir kategoriyi pasife çekmemelidir.
    /// </summary>
    public class MikroGrupReadResult<T>
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
