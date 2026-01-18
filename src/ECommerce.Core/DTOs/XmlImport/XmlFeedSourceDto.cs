// XmlFeedSourceDto: XML feed kaynak bilgilerini döndürmek için kullanılan DTO.
// Tedarikçi XML entegrasyonları için kaynak tanımlarını içerir.
// Hassas bilgiler (şifre vb.) filtrelenir.

using System;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// XML feed kaynağı bilgilerini içeren DTO.
    /// API response'larında ve admin panelinde kullanılır.
    /// Şifre gibi hassas bilgiler filtrelenir.
    /// </summary>
    public class XmlFeedSourceDto
    {
        #region Temel Bilgiler

        /// <summary>
        /// Kaynak benzersiz ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kaynak adı (kullanıcı dostu)
        /// Örn: "Ana Tedarikçi XML", "B2B Feed"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// XML feed URL'si
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Tedarikçi/Supplier adı
        /// Örn: "Migros", "Metro"
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool IsActive { get; set; }

        #endregion

        #region Senkronizasyon Ayarları

        /// <summary>
        /// Senkronizasyon aralığı (dakika)
        /// 0 veya null: Manuel senkronizasyon
        /// </summary>
        public int? SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Otomatik senkronizasyon aktif mi?
        /// </summary>
        public bool AutoSyncEnabled { get; set; }

        /// <summary>
        /// Formatlanmış senkronizasyon aralığı
        /// </summary>
        public string SyncIntervalText => SyncIntervalMinutes switch
        {
            null or 0 => "Manuel",
            <= 60 => $"{SyncIntervalMinutes} dakika",
            <= 1440 => $"{SyncIntervalMinutes / 60} saat",
            _ => $"{SyncIntervalMinutes / 1440} gün"
        };

        #endregion

        #region Senkronizasyon Durumu

        /// <summary>
        /// Son senkronizasyon tarihi
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Son senkronizasyon başarılı mı?
        /// </summary>
        public bool? LastSyncSuccess { get; set; }

        /// <summary>
        /// Son senkronizasyon hata mesajı
        /// </summary>
        public string? LastSyncError { get; set; }

        /// <summary>
        /// Bir sonraki planlanan senkronizasyon
        /// </summary>
        public DateTime? NextSyncAt { get; set; }

        /// <summary>
        /// Senkronizasyon durum açıklaması
        /// </summary>
        public string SyncStatus
        {
            get
            {
                if (LastSyncAt == null) return "Hiç senkronize edilmedi";
                if (LastSyncSuccess == true) return "Başarılı";
                if (LastSyncSuccess == false) return "Başarısız";
                return "Bilinmiyor";
            }
        }

        #endregion

        #region İstatistikler

        /// <summary>
        /// Son senkronizasyonda eklenen kayıt sayısı
        /// </summary>
        public int LastSyncCreatedCount { get; set; }

        /// <summary>
        /// Son senkronizasyonda güncellenen kayıt sayısı
        /// </summary>
        public int LastSyncUpdatedCount { get; set; }

        /// <summary>
        /// Son senkronizasyonda hatalı kayıt sayısı
        /// </summary>
        public int LastSyncFailedCount { get; set; }

        /// <summary>
        /// Toplam senkronizasyon sayısı
        /// </summary>
        public int TotalSyncCount { get; set; }

        #endregion

        #region Kimlik Doğrulama

        /// <summary>
        /// Authentication tipi
        /// "None", "Basic", "Bearer", "ApiKey"
        /// </summary>
        public string? AuthType { get; set; }

        /// <summary>
        /// Authentication yapılandırılmış mı?
        /// Şifre gösterilmez, sadece durum bilgisi
        /// </summary>
        public bool HasAuthentication => !string.IsNullOrEmpty(AuthType) && AuthType != "None";

        #endregion

        #region Tarihler

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        #endregion

        #region Notlar

        /// <summary>
        /// Admin notları
        /// </summary>
        public string? Notes { get; set; }

        #endregion
    }

    /// <summary>
    /// Feed kaynağı listesi için basitleştirilmiş DTO.
    /// </summary>
    public class XmlFeedSourceListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public bool? LastSyncSuccess { get; set; }
        public bool AutoSyncEnabled { get; set; }
    }
}
