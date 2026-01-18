// XmlFeedSource: XML feed kaynaklarını tanımlar.
// Tedarikçi XML URL'leri, mapping konfigürasyonları ve senkronizasyon ayarları bu entity'de tutulur.
// Background job'lar bu kayıtları kullanarak periyodik stok/fiyat güncellemesi yapar.

using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// XML feed kaynak tanımları entity'si.
    /// Tedarikçi XML entegrasyonları için kullanılır.
    /// </summary>
    public class XmlFeedSource : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Feed kaynağı adı (kullanıcı dostu)
        /// Örn: "Ana Tedarikçi XML", "B2B Feed", "Stok Güncellemesi"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// XML feed URL'si
        /// HTTP/HTTPS protokolü ile erişilebilir olmalı
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Tedarikçi/Supplier adı
        /// Örn: "Migros", "Metro", "Getir Tedarik"
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// XML alan eşleştirme konfigürasyonu (JSON formatında)
        /// Hangi XML tag'ının hangi entity alanına map edileceğini tanımlar
        /// Örnek: {"sku": "ProductCode", "price": "SalesPrice", "stock": "Quantity"}
        /// </summary>
        public string? MappingConfig { get; set; }

        #endregion

        #region Senkronizasyon Ayarları

        /// <summary>
        /// Son başarılı senkronizasyon zamanı
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Son senkronizasyon sonucu (başarılı/hatalı)
        /// </summary>
        public bool? LastSyncSuccess { get; set; }

        /// <summary>
        /// Son senkronizasyon hata mesajı (varsa)
        /// </summary>
        public string? LastSyncError { get; set; }

        /// <summary>
        /// Senkronizasyon aralığı (dakika cinsinden)
        /// 0 veya null: Manuel senkronizasyon
        /// Örn: 60 = Her saat, 1440 = Günde bir
        /// </summary>
        public int? SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Otomatik senkronizasyon aktif mi?
        /// false ise sadece manuel tetikleme ile çalışır
        /// </summary>
        public bool AutoSyncEnabled { get; set; } = false;

        /// <summary>
        /// Bir sonraki planlanan senkronizasyon zamanı
        /// Background job tarafından güncellenir
        /// </summary>
        public DateTime? NextSyncAt { get; set; }

        #endregion

        #region İstatistikler

        /// <summary>
        /// Son senkronizasyonda eklenen varyant sayısı
        /// </summary>
        public int LastSyncCreatedCount { get; set; } = 0;

        /// <summary>
        /// Son senkronizasyonda güncellenen varyant sayısı
        /// </summary>
        public int LastSyncUpdatedCount { get; set; } = 0;

        /// <summary>
        /// Son senkronizasyonda hatalı kayıt sayısı
        /// </summary>
        public int LastSyncFailedCount { get; set; } = 0;

        /// <summary>
        /// Toplam senkronizasyon sayısı
        /// </summary>
        public int TotalSyncCount { get; set; } = 0;

        #endregion

        #region Kimlik Doğrulama (Opsiyonel)

        /// <summary>
        /// Feed için gerekli authentication tipi
        /// Örn: "None", "Basic", "Bearer", "ApiKey"
        /// </summary>
        public string? AuthType { get; set; }

        /// <summary>
        /// Authentication için kullanıcı adı veya API key
        /// Güvenlik: Şifrelenmiş saklanmalı
        /// </summary>
        public string? AuthUsername { get; set; }

        /// <summary>
        /// Authentication için şifre veya token
        /// Güvenlik: Şifrelenmiş saklanmalı
        /// </summary>
        public string? AuthPassword { get; set; }

        #endregion

        #region Notlar

        /// <summary>
        /// Admin notları
        /// </summary>
        public string? Notes { get; set; }

        #endregion
    }
}
