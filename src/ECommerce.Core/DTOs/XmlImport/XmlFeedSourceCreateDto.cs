// XmlFeedSourceCreateDto: Yeni XML feed kaynağı oluşturmak için kullanılan DTO.
// Validation kuralları ile güvenli veri girişi sağlanır.
// URL formatı ve authentication bilgileri kontrol edilir.

using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.XmlImport
{
    /// <summary>
    /// Yeni XML feed kaynağı oluşturmak için kullanılan DTO.
    /// Tedarikçi XML entegrasyonu için kaynak tanımı.
    /// </summary>
    public class XmlFeedSourceCreateDto
    {
        #region Temel Bilgiler

        /// <summary>
        /// Kaynak adı (benzersiz olmalı)
        /// Örn: "Ana Tedarikçi XML", "B2B Feed"
        /// </summary>
        [Required(ErrorMessage = "Kaynak adı zorunludur")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Kaynak adı 2-200 karakter arasında olmalıdır")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// XML feed URL'si
        /// HTTP veya HTTPS protokolü desteklenir
        /// </summary>
        [Required(ErrorMessage = "URL zorunludur")]
        [StringLength(1000, ErrorMessage = "URL en fazla 1000 karakter olabilir")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Tedarikçi/Supplier adı (opsiyonel)
        /// Örn: "Migros", "Metro", "Getir Tedarik"
        /// </summary>
        [StringLength(200, ErrorMessage = "Tedarikçi adı en fazla 200 karakter olabilir")]
        public string? SupplierName { get; set; }

        /// <summary>
        /// XML alan eşleştirme konfigürasyonu (JSON formatında)
        /// Null ise varsayılan mapping kullanılır
        /// </summary>
        public string? MappingConfig { get; set; }

        #endregion

        #region Senkronizasyon Ayarları

        /// <summary>
        /// Senkronizasyon aralığı (dakika)
        /// 0 veya null: Sadece manuel senkronizasyon
        /// Minimum: 15 dakika (sunucu yükü için)
        /// </summary>
        [Range(0, 10080, ErrorMessage = "Senkronizasyon aralığı 0-10080 dakika (1 hafta) arasında olmalıdır")]
        public int? SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Otomatik senkronizasyon aktif mi?
        /// true ise SyncIntervalMinutes'a göre otomatik çalışır
        /// </summary>
        public bool AutoSyncEnabled { get; set; } = false;

        #endregion

        #region Kimlik Doğrulama

        /// <summary>
        /// Authentication tipi
        /// Desteklenen: "None", "Basic", "Bearer", "ApiKey"
        /// </summary>
        [StringLength(50, ErrorMessage = "Auth tipi en fazla 50 karakter olabilir")]
        [RegularExpression(@"^(None|Basic|Bearer|ApiKey)?$", ErrorMessage = "Geçersiz authentication tipi")]
        public string? AuthType { get; set; }

        /// <summary>
        /// Authentication kullanıcı adı veya API key
        /// Basic auth için kullanıcı adı, ApiKey için key değeri
        /// </summary>
        [StringLength(200, ErrorMessage = "Kullanıcı adı en fazla 200 karakter olabilir")]
        public string? AuthUsername { get; set; }

        /// <summary>
        /// Authentication şifresi veya token
        /// Basic auth için şifre, Bearer için token
        /// ÖNEMLİ: Şifrelenmiş olarak saklanmalı
        /// </summary>
        [StringLength(200, ErrorMessage = "Şifre en fazla 200 karakter olabilir")]
        public string? AuthPassword { get; set; }

        #endregion

        #region Diğer

        /// <summary>
        /// Aktif/Pasif durumu
        /// Pasif kaynaklar otomatik senkronize edilmez
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Admin notları (opsiyonel)
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        #endregion

        #region Validation

        /// <summary>
        /// Authentication bilgilerinin tutarlılığını kontrol eder.
        /// Basic ve ApiKey için kullanıcı adı zorunlu.
        /// </summary>
        public bool ValidateAuthentication(out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(AuthType) || AuthType == "None")
            {
                return true;
            }

            if (AuthType == "Basic" && string.IsNullOrEmpty(AuthUsername))
            {
                errorMessage = "Basic authentication için kullanıcı adı zorunludur";
                return false;
            }

            if (AuthType == "Bearer" && string.IsNullOrEmpty(AuthPassword))
            {
                errorMessage = "Bearer authentication için token zorunludur";
                return false;
            }

            if (AuthType == "ApiKey" && string.IsNullOrEmpty(AuthUsername))
            {
                errorMessage = "ApiKey authentication için API key zorunludur";
                return false;
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Mevcut XML feed kaynağını güncellemek için kullanılan DTO.
    /// Partial update desteklenir.
    /// </summary>
    public class XmlFeedSourceUpdateDto
    {
        /// <summary>
        /// Kaynak adı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Kaynak adı 2-200 karakter arasında olmalıdır")]
        public string? Name { get; set; }

        /// <summary>
        /// XML feed URL'si
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(1000, ErrorMessage = "URL en fazla 1000 karakter olabilir")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz")]
        public string? Url { get; set; }

        /// <summary>
        /// Tedarikçi adı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(200, ErrorMessage = "Tedarikçi adı en fazla 200 karakter olabilir")]
        public string? SupplierName { get; set; }

        /// <summary>
        /// Mapping konfigürasyonu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public string? MappingConfig { get; set; }

        /// <summary>
        /// Senkronizasyon aralığı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0, 10080, ErrorMessage = "Senkronizasyon aralığı 0-10080 dakika arasında olmalıdır")]
        public int? SyncIntervalMinutes { get; set; }

        /// <summary>
        /// Otomatik senkronizasyon durumu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public bool? AutoSyncEnabled { get; set; }

        /// <summary>
        /// Authentication tipi
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(50, ErrorMessage = "Auth tipi en fazla 50 karakter olabilir")]
        public string? AuthType { get; set; }

        /// <summary>
        /// Authentication kullanıcı adı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(200, ErrorMessage = "Kullanıcı adı en fazla 200 karakter olabilir")]
        public string? AuthUsername { get; set; }

        /// <summary>
        /// Authentication şifresi
        /// Null gönderilirse güncellenmez
        /// Boş string gönderilirse silinir
        /// </summary>
        [StringLength(200, ErrorMessage = "Şifre en fazla 200 karakter olabilir")]
        public string? AuthPassword { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Admin notları
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        /// <summary>
        /// DTO'nun herhangi bir güncelleme içerip içermediğini kontrol eder.
        /// </summary>
        public bool HasAnyUpdate()
        {
            return Name != null ||
                   Url != null ||
                   SupplierName != null ||
                   MappingConfig != null ||
                   SyncIntervalMinutes.HasValue ||
                   AutoSyncEnabled.HasValue ||
                   AuthType != null ||
                   AuthUsername != null ||
                   AuthPassword != null ||
                   IsActive.HasValue ||
                   Notes != null;
        }
    }
}
