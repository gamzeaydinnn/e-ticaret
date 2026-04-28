using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Mikro ERP'den çekilen ürünlerin yerel cache tablosu.
    /// 
    /// AMAÇ:
    /// - 6000+ ürünü her seferinde API'den çekmek yerine local DB'de saklar
    /// - Sayfalama için hızlı erişim sağlar (milisaniyeler içinde)
    /// - Delta sync ile sadece değişen ürünleri günceller
    /// 
    /// KULLANIM:
    /// 1. İlk çekimde tüm ürünler bu tabloya kaydedilir
    /// 2. Sayfa değişince local DB'den çekilir (çok hızlı)
    /// 3. "Yenile" butonu sadece değişen ürünleri günceller
    /// </summary>
    [Table("MikroProductCache")]
    public class MikroProductCache
    {
        /// <summary>
        /// Birincil anahtar - Mikro'dan gelen stok kodu
        /// </summary>
        [Key]
        [StringLength(50)]
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı (Mikro'dan StoIsim)
        /// </summary>
        [StringLength(500)]
        public string? StokAd { get; set; }

        /// <summary>
        /// Barkod numarası
        /// </summary>
        [StringLength(50)]
        public string? Barkod { get; set; }

        /// <summary>
        /// Grup kodu (kategori mapping için)
        /// </summary>
        [StringLength(50)]
        public string? GrupKod { get; set; }

        /// <summary>
        /// Birim adı (ADET, KG, vb.)
        /// </summary>
        [StringLength(20)]
        public string? Birim { get; set; }

        /// <summary>
        /// KDV oranı (%)
        /// </summary>
        public decimal KdvOrani { get; set; }

        /// <summary>
        /// Satış fiyatı - Seçilen fiyat listesinden
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal SatisFiyati { get; set; }

        /// <summary>
        /// Fiyat listesi numarası (1-10)
        /// </summary>
        public int FiyatListesiNo { get; set; } = 1;

        /// <summary>
        /// Depo miktarı (stok adedi)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal DepoMiktari { get; set; }

        /// <summary>
        /// Satılabilir miktar (rezerveler düşülmüş)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal SatilabilirMiktar { get; set; }

        /// <summary>
        /// Depo numarası (0 = tüm depolar)
        /// </summary>
        public int DepoNo { get; set; }

        /// <summary>
        /// JSON formatında tüm fiyat listeleri (1-10)
        /// Örnek: [{"no":1,"fiyat":100.50},{"no":2,"fiyat":95.00}]
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? TumFiyatlarJson { get; set; }

        /// <summary>
        /// JSON formatında tüm depo stokları
        /// Örnek: [{"depoNo":1,"miktar":50},{"depoNo":2,"miktar":30}]
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? TumDepolarJson { get; set; }

        /// <summary>
        /// Mikro'daki son güncelleme tarihi (değişiklik tespiti için)
        /// </summary>
        public DateTime? MikroGuncellemeTarihi { get; set; }

        /// <summary>
        /// Cache'e ilk eklenme tarihi
        /// </summary>
        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Cache'deki son güncelleme tarihi
        /// </summary>
        public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ürün aktif mi? (pasif ürünler gösterilmez)
        /// </summary>
        public bool Aktif { get; set; } = true;

        /// <summary>
        /// E-ticaret sistemindeki karşılık ürün ID'si (null = henüz import edilmemiş)
        /// </summary>
        public int? LocalProductId { get; set; }

        /// <summary>
        /// Senkronizasyon durumu: 
        /// 0 = Senkronize değil
        /// 1 = Senkronize
        /// 2 = Güncelleme bekliyor
        /// </summary>
        public int SyncStatus { get; set; } = 0;

        /// <summary>
        /// Hash değeri - veri değişikliği tespiti için
        /// MD5(StokAd + SatisFiyati + DepoMiktari + KdvOrani)
        /// </summary>
        [StringLength(32)]
        public string? DataHash { get; set; }
    }

    /// <summary>
    /// Senkronizasyon durumu enum'ı
    /// </summary>
    public enum MikroSyncStatus
    {
        NotSynced = 0,
        Synced = 1,
        PendingUpdate = 2
    }
}
