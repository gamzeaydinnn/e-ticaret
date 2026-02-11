// ==========================================================================
// ICartSettingsService.cs - Sepet Ayarları Servisi Interface'i
// ==========================================================================
// Minimum sepet tutarı yönetimi için servis sözleşmesi.
// Public: Müşteri tarafı doğrulama.  Admin: Ayar güncelleme.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Sepet ayarları için servis interface'i.
    /// Minimum sepet tutarı sorgulama ve admin güncelleme desteği.
    /// </summary>
    public interface ICartSettingsService
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // PUBLIC - MÜŞTERİ TARAFI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Aktif sepet ayarlarını getirir.
        /// Sepet ve checkout sayfaları için kullanılır.
        /// </summary>
        Task<CartSettingsDto> GetActiveSettingsAsync();

        /// <summary>
        /// Sepet toplamının minimum tutarı karşılayıp karşılamadığını doğrular.
        /// </summary>
        /// <param name="cartTotal">Sepet ara toplamı (TL)</param>
        /// <returns>Geçerliyse true, minimum altındaysa false</returns>
        Task<bool> ValidateMinimumCartAmountAsync(decimal cartTotal);

        // ═══════════════════════════════════════════════════════════════════════════════
        // ADMIN - AYAR YÖNETİMİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Admin paneli için ayarları getirir (cache'siz, taze veri).
        /// </summary>
        Task<CartSettingsDto> GetSettingsForAdminAsync();

        /// <summary>
        /// Sepet ayarlarını günceller.
        /// Sadece Admin yetkisi ile çağrılmalı.
        /// </summary>
        /// <param name="updateDto">Güncellenecek alanlar (null olanlar değişmez)</param>
        /// <param name="updatedByUserId">Güncellemeyi yapan admin ID'si</param>
        /// <param name="updatedByUserName">Güncellemeyi yapan admin adı</param>
        /// <returns>Başarılı ise true</returns>
        Task<bool> UpdateSettingsAsync(CartSettingsUpdateDto updateDto, int updatedByUserId, string updatedByUserName);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DTO'LAR (Data Transfer Objects)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sepet ayarları okuma DTO'su.
    /// API response ve frontend için kullanılır.
    /// </summary>
    public class CartSettingsDto
    {
        public int Id { get; set; }
        public decimal MinimumCartAmount { get; set; }
        public bool IsMinimumCartAmountActive { get; set; }
        public string MinimumCartAmountMessage { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    /// <summary>
    /// Sepet ayarları güncelleme DTO'su.
    /// Admin panelinden gelen update request için.
    /// Tüm alanlar opsiyonel - sadece gönderilen alanlar güncellenir.
    /// </summary>
    public class CartSettingsUpdateDto
    {
        /// <summary>
        /// Yeni minimum sepet tutarı (null ise değişmez)
        /// </summary>
        public decimal? MinimumCartAmount { get; set; }

        /// <summary>
        /// Minimum tutar aktiflik durumu (null ise değişmez)
        /// </summary>
        public bool? IsMinimumCartAmountActive { get; set; }

        /// <summary>
        /// Müşteriye gösterilecek mesaj (null ise değişmez)
        /// </summary>
        public string? MinimumCartAmountMessage { get; set; }
    }
}
