// ==========================================================================
// IShippingService.cs - Kargo Servisi Interface'i
// ==========================================================================
// Kargo ücreti yönetimi için servis sözleşmesi.
// Araç tipi bazlı dinamik fiyatlandırma ve admin güncelleme desteği.
// ==========================================================================

using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Kargo işlemleri için servis interface'i.
    /// Repository pattern ile DbContext'e erişim sağlar.
    /// </summary>
    public interface IShippingService
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // KARGO AYARLARI SORGULAMA
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tüm kargo ayarlarını getirir (aktif/pasif dahil).
        /// Admin paneli için kullanılır.
        /// </summary>
        Task<IEnumerable<ShippingSettingDto>> GetAllSettingsAsync();

        /// <summary>
        /// Sadece aktif kargo seçeneklerini getirir.
        /// Müşteri sepet sayfası için kullanılır.
        /// </summary>
        Task<IEnumerable<ShippingSettingDto>> GetActiveSettingsAsync();

        /// <summary>
        /// Belirli bir araç tipinin kargo ücretini getirir.
        /// </summary>
        /// <param name="vehicleType">Araç tipi: "motorcycle" veya "car"</param>
        /// <returns>Kargo ücreti (TL), bulunamazsa null</returns>
        Task<decimal?> GetPriceByVehicleTypeAsync(string vehicleType);

        /// <summary>
        /// ID ile tek bir kargo ayarını getirir.
        /// </summary>
        Task<ShippingSettingDto?> GetByIdAsync(int id);

        /// <summary>
        /// Araç tipi ile tek bir kargo ayarını getirir.
        /// </summary>
        Task<ShippingSettingDto?> GetByVehicleTypeAsync(string vehicleType);

        // ═══════════════════════════════════════════════════════════════════════════════
        // KARGO AYARLARI GÜNCELLEME (ADMIN)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kargo ayarını günceller (fiyat, açıklama vb.).
        /// Sadece Admin yetkisi ile çağrılmalı.
        /// </summary>
        /// <param name="id">Güncellenecek ayar ID'si</param>
        /// <param name="updateDto">Güncellenecek alanlar</param>
        /// <param name="updatedByUserId">Güncellemeyi yapan admin ID'si</param>
        /// <param name="updatedByUserName">Güncellemeyi yapan admin adı</param>
        /// <returns>Başarılı ise true</returns>
        Task<bool> UpdateSettingAsync(int id, ShippingSettingUpdateDto updateDto, int updatedByUserId, string updatedByUserName);

        /// <summary>
        /// Kargo ayarının aktif/pasif durumunu değiştirir.
        /// </summary>
        Task<bool> ToggleActiveAsync(int id, bool isActive, int updatedByUserId, string updatedByUserName);

        // ═══════════════════════════════════════════════════════════════════════════════
        // ESKİ METODLAR (Geriye Dönük Uyumluluk)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// [DEPRECATED] Sipariş bazlı kargo ücreti hesaplama.
        /// Yeni sistemde GetPriceByVehicleTypeAsync kullanılmalı.
        /// </summary>
        Task<decimal> CalculateShippingCostAsync(int orderId);

        /// <summary>
        /// Tahmini teslimat süresini döndürür.
        /// </summary>
        Task<string> GetEstimatedDeliveryAsync(int orderId);

        /// <summary>
        /// Sipariş gönderimini başlatır.
        /// </summary>
        Task<bool> ShipOrderAsync(int orderId);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DTO'LAR (Data Transfer Objects)
    // Interface ile birlikte tanımlanıyor, ayrı dosyaya da taşınabilir
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Kargo ayarı okuma DTO'su.
    /// API response ve frontend için kullanılır.
    /// </summary>
    public class ShippingSettingDto
    {
        public int Id { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string EstimatedDeliveryTime { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public decimal? MaxWeight { get; set; }
        public decimal? MaxVolume { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    /// <summary>
    /// Kargo ayarı güncelleme DTO'su.
    /// Admin panelinden gelen update request için.
    /// </summary>
    public class ShippingSettingUpdateDto
    {
        /// <summary>
        /// Yeni kargo ücreti (null ise değişmez)
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Görüntüleme adı (null ise değişmez)
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Tahmini teslimat süresi (null ise değişmez)
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Açıklama (null ise değişmez)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Sıralama (null ise değişmez)
        /// </summary>
        public int? SortOrder { get; set; }

        /// <summary>
        /// Maksimum ağırlık (null ise değişmez)
        /// </summary>
        public decimal? MaxWeight { get; set; }

        /// <summary>
        /// Aktif durumu (null ise değişmez)
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
