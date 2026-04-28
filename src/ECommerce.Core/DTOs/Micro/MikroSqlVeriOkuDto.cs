using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI SqlVeriOkuV2 endpoint'i için istek DTO'su.
    /// 
    /// Bu endpoint custom SQL sorguları çalıştırarak veri çekmeyi sağlar.
    /// Fiyat listesi, stok miktarları gibi veriler bu endpoint üzerinden alınır.
    /// 
    /// Endpoint: /Api/APIMethods/SqlVeriOkuV2
    /// </summary>
    public class MikroSqlVeriOkuRequestDto
    {
        /// <summary>
        /// Çalıştırılacak SQL sorgusu.
        /// SADECE SELECT sorguları çalışır!
        /// </summary>
        [JsonPropertyName("SQLSorgu")]
        public string SQLSorgu { get; set; } = string.Empty;
    }

    /// <summary>
    /// SqlVeriOkuV2 genel response wrapper.
    /// API dinamik sütunlar döndürdüğü için generic list kullanılır.
    /// </summary>
    public class MikroSqlVeriOkuResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Hata veya bilgi mesajı.
        /// </summary>
        [JsonPropertyName("Message")]
        public string? Message { get; set; }

        /// <summary>
        /// Dönen satır sayısı.
        /// </summary>
        [JsonPropertyName("RowCount")]
        public int RowCount { get; set; }

        /// <summary>
        /// Sorgu sonuç verileri.
        /// Her satır bir Dictionary olarak döner (sütun adı -> değer).
        /// </summary>
        [JsonPropertyName("Data")]
        public List<Dictionary<string, object>>? Data { get; set; }
    }

    /// <summary>
    /// Fiyat listesi sorgusu sonuç satırı.
    /// STOK_SATIS_FIYAT_LISTELERI tablosundan çekilen veriler.
    /// </summary>
    public class MikroFiyatSatirDto
    {
        /// <summary>
        /// Fiyat kaydı GUID.
        /// </summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// Stok kodu (SKU).
        /// </summary>
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı.
        /// SQL fallback senaryosunda StokListesiV2 boşsa görüntüleme için kullanılır.
        /// </summary>
        public string? UrunAdi { get; set; }

        /// <summary>
        /// Satış fiyatı.
        /// </summary>
        public decimal Fiyat { get; set; }

        /// <summary>
        /// Ürün barkodu.
        /// </summary>
        public string Barkod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün webde gösterime açık mı? (STOKLAR.sto_webe_gonderilecek_fl)
        /// </summary>
        public bool? WebeGonderilecekFl { get; set; }
    }

    /// <summary>
    /// Stok miktarı sorgusu sonuç satırı.
    /// </summary>
    public class MikroStokMiktarSatirDto
    {
        /// <summary>
        /// Stok kodu (SKU).
        /// </summary>
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Depo numarası.
        /// </summary>
        public int DepoNo { get; set; }

        /// <summary>
        /// Stok miktarı.
        /// </summary>
        public decimal Miktar { get; set; }

        /// <summary>
        /// Rezerve miktar.
        /// </summary>
        public decimal RezerveMiktar { get; set; }

        /// <summary>
        /// Satılabilir miktar (Miktar - Rezerve).
        /// </summary>
        public decimal SatilabilirMiktar => Miktar - RezerveMiktar;
    }

    /// <summary>
    /// Fiyat ve stok bilgisi birleşik DTO.
    /// Frontend'e gönderilecek zengin veri yapısı.
    /// </summary>
    public class MikroUrunFiyatStokDto
    {
        /// <summary>
        /// Stok kodu (SKU).
        /// </summary>
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün barkodu.
        /// </summary>
        public string Barkod { get; set; } = string.Empty;

        /// <summary>
        /// Satış fiyatı.
        /// </summary>
        public decimal Fiyat { get; set; }

        /// <summary>
        /// Fiyat listesi numarası (1-10).
        /// </summary>
        public int FiyatListesiNo { get; set; } = 1;

        /// <summary>
        /// Toplam stok miktarı.
        /// </summary>
        public decimal StokMiktari { get; set; }

        /// <summary>
        /// Satılabilir stok miktarı.
        /// </summary>
        public decimal SatilabilirMiktar { get; set; }

        /// <summary>
        /// Fiyat kaydı GUID.
        /// </summary>
        public string? FiyatGuid { get; set; }
    }

    /// <summary>
    /// Fiyat listesi çekme sonucu.
    /// </summary>
    public class MikroFiyatListesiSonucDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Hata veya bilgi mesajı.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Toplam kayıt sayısı.
        /// </summary>
        public int ToplamKayit { get; set; }

        /// <summary>
        /// Fiyat listesi verileri.
        /// </summary>
        public List<MikroFiyatSatirDto> Fiyatlar { get; set; } = new();

        /// <summary>
        /// Çekme işlemi süresi (ms).
        /// </summary>
        public long SureMs { get; set; }
    }

    /// <summary>
    /// Birleşik ürün bilgisi DTO'su (SQL tabanlı senkronizasyon için).
    /// 
    /// Bu DTO, Mikro ERP'den SQL sorgusu ile çekilen ürün verilerini temsil eder.
    /// STOK_SATIS_FIYAT_LISTELERI_YONETIM + fn_Stok_Depo_Dagilim + STOKLAR tablolarından
    /// birleştirilmiş veri içerir.
    /// 
    /// FIELD MAPPING:
    /// - msg_S_0001 → StokKod (Stok kodu)
    /// - msg_S_0005 → UrunAdi (Ürün adı)
    /// - msg_S_0002 → Fiyat (Satış fiyatı)
    /// - msg_S_0343 → StokMiktar (Kullanılabilir stok)
    /// - msg_S_1266 → DepoAdi (Depo adı)
    /// - msg_S_0873 → DepoNo (Depo numarası)
    /// - sto_webe_gonderilecek_fl → IsWebActive (Web'e gönderilecek mi)
    /// - sto_birim1_ad → Birim (Ölçü birimi)
    /// - sto_grup_kod → GrupKod (Grup kodu)
    /// 
    /// Gereksinim: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7
    /// </summary>
    public class MikroUrunDto
    {
        /// <summary>
        /// Stok kodu (SKU).
        /// Mikro Field: msg_S_0001
        /// </summary>
        [JsonPropertyName("stok_kod")]
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı.
        /// Mikro Field: msg_S_0005
        /// </summary>
        [JsonPropertyName("urun_adi")]
        public string UrunAdi { get; set; } = string.Empty;

        /// <summary>
        /// Satış fiyatı (TL).
        /// Mikro Field: msg_S_0002
        /// </summary>
        [JsonPropertyName("fiyat")]
        public decimal Fiyat { get; set; }

        /// <summary>
        /// Kullanılabilir stok miktarı.
        /// Mikro Field: msg_S_0343
        /// </summary>
        [JsonPropertyName("stok_miktar")]
        public decimal StokMiktar { get; set; }

        /// <summary>
        /// Depo adı.
        /// Mikro Field: msg_S_1266
        /// </summary>
        [JsonPropertyName("depo_adi")]
        public string DepoAdi { get; set; } = string.Empty;

        /// <summary>
        /// Depo numarası.
        /// Mikro Field: msg_S_0873
        /// </summary>
        [JsonPropertyName("depo_no")]
        public int DepoNo { get; set; }

        /// <summary>
        /// Web'e gönderilecek mi?
        /// Mikro Field: sto_webe_gonderilecek_fl
        /// </summary>
        [JsonPropertyName("web_aktif")]
        public bool IsWebActive { get; set; }

        /// <summary>
        /// Ölçü birimi (Adet, Kg, vb.).
        /// Mikro Field: sto_birim1_ad
        /// </summary>
        [JsonPropertyName("birim")]
        public string Birim { get; set; } = string.Empty;

        /// <summary>
        /// Grup kodu (kategori).
        /// Mikro Field: sto_grup_kod
        /// </summary>
        [JsonPropertyName("grup_kod")]
        public string GrupKod { get; set; } = string.Empty;

        /// <summary>
        /// KDV oranı (%).
        /// Mikro Field: sto_perakende_vergi
        /// </summary>
        [JsonPropertyName("kdv_orani")]
        public decimal KdvOrani { get; set; }

        /// <summary>
        /// Barkod numarası.
        /// Mikro Field: bar_kodu
        /// </summary>
        [JsonPropertyName("barkod")]
        public string Barkod { get; set; } = string.Empty;

        /// <summary>
        /// SQL toplam kayıt sayısı (sayfalama metaverisi).
        /// Mikro Field: toplam_kayit
        /// </summary>
        [JsonPropertyName("toplam_kayit")]
        public int ToplamKayit { get; set; }

        // ==================== HESAPLANAN PROPERTY'LER ====================

        /// <summary>
        /// Ürün stokta var mı?
        /// </summary>
        [JsonIgnore]
        public bool IsStokta => StokMiktar > 0;

        /// <summary>
        /// Stok durumu metni.
        /// </summary>
        [JsonPropertyName("stok_durumu")]
        public string StokDurumu => IsStokta ? "Stokta" : "Stokta Yok";

        /// <summary>
        /// Formatlanmış fiyat (₺XX.XX).
        /// </summary>
        [JsonPropertyName("fiyat_formatli")]
        public string FiyatFormatli => $"₺{Fiyat:N2}";
    }
}
