using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 DahiliStokHareketKaydetV2 istek gövdesi (auth CreateMikroRequest ile eklenir).
    /// </summary>
    public class MikroDahiliStokHareketKaydetRequestDto
    {
        [JsonPropertyName("evraklar")]
        public List<MikroDahiliStokHareketEvrakDto> Evraklar { get; set; } = new();
    }

    public class MikroDahiliStokHareketEvrakDto
    {
        [JsonPropertyName("evrak_aciklamalari")]
        public List<MikroDahiliStokHareketAciklamaDto>? EvrakAciklamalari { get; set; }

        [JsonPropertyName("satirlar")]
        public List<MikroDahiliStokHareketSatirDto> Satirlar { get; set; } = new();
    }

    public class MikroDahiliStokHareketAciklamaDto
    {
        [JsonPropertyName("aciklama")]
        public string Aciklama { get; set; } = string.Empty;
    }

    public class MikroDahiliStokHareketSatirDto
    {
        [JsonPropertyName("sth_stok_kod")]
        public string SthStokKod { get; set; } = string.Empty;

        [JsonPropertyName("sth_miktar")]
        public decimal SthMiktar { get; set; }

        [JsonPropertyName("sth_tutar")]
        public decimal SthTutar { get; set; }

        /// <summary>2 = çıkış yönlü tip (örnek doküman).</summary>
        [JsonPropertyName("sth_tip")]
        public string SthTip { get; set; } = "2";

        [JsonPropertyName("sth_cins")]
        public string SthCins { get; set; } = "6";

        [JsonPropertyName("sth_evraktip")]
        public string SthEvraktip { get; set; } = "2";

        [JsonPropertyName("sth_evrakno_seri")]
        public string SthEvraknoSeri { get; set; } = "WEB";

        [JsonPropertyName("sth_cikis_depo_no")]
        public int SthCikisDepoNo { get; set; }

        [JsonPropertyName("sth_giris_depo_no")]
        public int SthGirisDepoNo { get; set; }

        [JsonPropertyName("sth_tarih")]
        public string SthTarih { get; set; } = string.Empty;

        [JsonPropertyName("sth_normal_iade")]
        public string SthNormalIade { get; set; } = "0";

        [JsonPropertyName("sth_birim_pntr")]
        public int SthBirimPntr { get; set; } = 1;

        [JsonPropertyName("sth_vergi_pntr")]
        public int SthVergiPntr { get; set; } = 0;

        [JsonPropertyName("sth_vergisiz_fl")]
        public int SthVergisizFl { get; set; } = 1;

        [JsonPropertyName("sth_isk_mas1")]
        public string SthIskMas1 { get; set; } = "0";

        [JsonPropertyName("sth_isk_mas2")]
        public string SthIskMas2 { get; set; } = "1";

        [JsonPropertyName("sth_cari_kodu")]
        public string? SthCariKodu { get; set; }

        [JsonPropertyName("sth_cari_cinsi")]
        public string SthCariCinsi { get; set; } = "0";
    }

    public class MikroDahiliStokHareketKaydetResponseDto
    {
        [JsonPropertyName("Basarili")]
        public bool? Basarili { get; set; }

        [JsonPropertyName("Mesaj")]
        public string? Mesaj { get; set; }

        [JsonPropertyName("EvrakSeri")]
        public string? EvrakSeri { get; set; }

        [JsonPropertyName("EvrakSira")]
        public int? EvrakSira { get; set; }
    }
}
