using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// MikroAPI senkronizasyon durumu takip entity'si.
    /// 
    /// NEDEN: Delta senkronizasyon için son başarılı sync zamanını bilmek kritik.
    /// Bu entity, her sync tipinin (Stok, Fiyat, Siparis vb.) son durumunu saklar.
    /// 
    /// KULLANIM:
    /// - Her sync job'ı başlamadan önce LastSyncTime'ı kontrol eder
    /// - Başarılı sync sonrası bu tablo güncellenir
    /// - Admin panelinde sync durumları gösterilebilir
    /// </summary>
    public class MikroSyncState
    {
        public int Id { get; set; }

        /// <summary>
        /// Senkronizasyon tipi.
        /// Değerler: "Stok", "Fiyat", "Siparis", "Cari"
        /// </summary>
        public string SyncType { get; set; } = string.Empty;

        /// <summary>
        /// Senkronizasyon yönü.
        /// "ToERP" = E-Ticaret → Mikro
        /// "FromERP" = Mikro → E-Ticaret
        /// </summary>
        public string Direction { get; set; } = "FromERP";

        /// <summary>
        /// Son başarılı senkronizasyon zamanı (UTC).
        /// Delta sync bu tarihten sonra değişenleri çeker.
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// Son sync'te işlenen kayıt sayısı.
        /// </summary>
        public int LastSyncCount { get; set; }

        /// <summary>
        /// Son sync süresi (milisaniye).
        /// Performans takibi için.
        /// </summary>
        public long LastSyncDurationMs { get; set; }

        /// <summary>
        /// Son sync başarılı mı?
        /// </summary>
        public bool LastSyncSuccess { get; set; }

        /// <summary>
        /// Son hata mesajı (başarısız ise).
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Ardışık hata sayısı.
        /// NEDEN: Belirli sayıda ardışık hata sonrası alert üretilebilir.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Sync aktif mi?
        /// NEDEN: Bakım için geçici olarak devre dışı bırakılabilir.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Kayıt oluşturulma tarihi.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme tarihi.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Sync tipi için unique key oluşturur.
        /// </summary>
        public static string GetKey(string syncType, string direction) => $"{syncType}_{direction}";
    }
}
