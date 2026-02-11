// ==========================================================================
// CartSettingsManager.cs - Sepet Ayarları Servisi İmplementasyonu
// ==========================================================================
// Minimum sepet tutarı yönetimi.
// DbContext üzerinden sepet ayarlarını okur ve günceller.
// Memory cache ile performans optimizasyonu sağlar.
// Singleton pattern: Tabloda tek aktif kayıt beklenir.
// ==========================================================================

using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Sepet ayarları yönetim servisi.
    /// Veritabanından minimum sepet tutarı okuma ve admin güncelleme işlemlerini yönetir.
    /// </summary>
    public class CartSettingsManager : ICartSettingsService
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // BAĞIMLILIKLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        private readonly ECommerceDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CartSettingsManager> _logger;

        // Cache key - tekil ayar kaydı için tek key yeterli
        private const string CACHE_KEY = "CartSettings_Active";

        // Cache süresi - sık güncellenmeyeceği için 30 dakika yeterli
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        // ═══════════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════════════

        public CartSettingsManager(
            ECommerceDbContext context,
            IMemoryCache cache,
            ILogger<CartSettingsManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PUBLIC - MÜŞTERİ TARAFI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<CartSettingsDto> GetActiveSettingsAsync()
        {
            // Cache kontrolü - müşteri sepeti için performans kritik
            if (_cache.TryGetValue(CACHE_KEY, out CartSettingsDto? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var setting = await _context.CartSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IsActive);

                // Kayıt yoksa varsayılan değerler döndür (minimum tutar pasif)
                var dto = setting != null
                    ? MapToDto(setting)
                    : GetDefaultSettings();

                // Cache'e ekle
                _cache.Set(CACHE_KEY, dto, CacheDuration);

                _logger.LogDebug("Sepet ayarları veritabanından yüklendi. MinAmount: {Amount}, Active: {Active}",
                    dto.MinimumCartAmount, dto.IsMinimumCartAmountActive);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet ayarları yüklenirken hata oluştu");
                // Hata durumunda güvenli varsayılan döndür (minimum tutar pasif)
                return GetDefaultSettings();
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateMinimumCartAmountAsync(decimal cartTotal)
        {
            var settings = await GetActiveSettingsAsync();

            // Minimum tutar kontrolü pasifse her zaman geçerli
            if (!settings.IsMinimumCartAmountActive)
                return true;

            return cartTotal >= settings.MinimumCartAmount;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ADMIN - AYAR YÖNETİMİ
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<CartSettingsDto> GetSettingsForAdminAsync()
        {
            // Admin paneli için cache kullanma - her zaman taze veri
            try
            {
                var setting = await _context.CartSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IsActive);

                if (setting == null)
                {
                    _logger.LogWarning("Veritabanında aktif sepet ayarı bulunamadı, varsayılan döndürülüyor");
                    return GetDefaultSettings();
                }

                return MapToDto(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin sepet ayarları getirilirken hata oluştu");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateSettingsAsync(
            CartSettingsUpdateDto updateDto,
            int updatedByUserId,
            string updatedByUserName)
        {
            if (updateDto == null)
            {
                _logger.LogWarning("UpdateSettingsAsync: Null updateDto parametresi");
                return false;
            }

            try
            {
                // İlk aktif kaydı bul veya oluştur
                var setting = await _context.CartSettings.FirstOrDefaultAsync(s => s.IsActive);

                if (setting == null)
                {
                    // Kayıt yoksa yeni oluştur (seed data çalışmamış olabilir)
                    setting = new CartSetting
                    {
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CartSettings.Add(setting);
                    _logger.LogInformation("Yeni sepet ayarı kaydı oluşturuluyor");
                }

                // Partial update - sadece null olmayan alanları güncelle
                if (updateDto.MinimumCartAmount.HasValue)
                {
                    // Negatif tutar kontrolü
                    if (updateDto.MinimumCartAmount.Value < 0)
                    {
                        _logger.LogWarning("Negatif minimum sepet tutarı reddedildi: {Amount}", updateDto.MinimumCartAmount.Value);
                        return false;
                    }
                    setting.MinimumCartAmount = updateDto.MinimumCartAmount.Value;
                }

                if (updateDto.IsMinimumCartAmountActive.HasValue)
                    setting.IsMinimumCartAmountActive = updateDto.IsMinimumCartAmountActive.Value;

                if (!string.IsNullOrWhiteSpace(updateDto.MinimumCartAmountMessage))
                    setting.MinimumCartAmountMessage = updateDto.MinimumCartAmountMessage.Trim();

                // Audit bilgileri
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = updatedByUserId;
                setting.UpdatedByUserName = updatedByUserName;

                await _context.SaveChangesAsync();

                // Cache'i temizle - güncel veri için
                InvalidateCache();

                _logger.LogInformation(
                    "Sepet ayarları güncellendi. MinAmount: {Amount}, Active: {Active}, Güncelleyen: {UserName}",
                    setting.MinimumCartAmount, setting.IsMinimumCartAmountActive, updatedByUserName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet ayarları güncellenirken hata oluştu");
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Entity'yi DTO'ya dönüştürür.
        /// </summary>
        private static CartSettingsDto MapToDto(CartSetting entity)
        {
            return new CartSettingsDto
            {
                Id = entity.Id,
                MinimumCartAmount = entity.MinimumCartAmount,
                IsMinimumCartAmountActive = entity.IsMinimumCartAmountActive,
                MinimumCartAmountMessage = entity.MinimumCartAmountMessage,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt,
                UpdatedByUserName = entity.UpdatedByUserName
            };
        }

        /// <summary>
        /// Veritabanında kayıt yoksa kullanılacak varsayılan ayarlar.
        /// Minimum tutar pasif olarak döndürülür (güvenli varsayılan).
        /// </summary>
        private static CartSettingsDto GetDefaultSettings()
        {
            return new CartSettingsDto
            {
                Id = 0,
                MinimumCartAmount = 0,
                IsMinimumCartAmountActive = false,
                MinimumCartAmountMessage = "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.",
                IsActive = true,
                UpdatedAt = null,
                UpdatedByUserName = null
            };
        }

        /// <summary>
        /// Sepet ayarları cache'ini temizler.
        /// Güncelleme sonrası çağrılmalı.
        /// </summary>
        private void InvalidateCache()
        {
            _cache.Remove(CACHE_KEY);
            _logger.LogDebug("Sepet ayarları cache'i temizlendi");
        }
    }
}
