// ==========================================================================
// ShippingManager.cs - Kargo Servisi İmplementasyonu
// ==========================================================================
// Araç tipi bazlı dinamik kargo fiyatlandırması.
// DbContext üzerinden kargo ayarlarını okur ve günceller.
// Memory cache ile performans optimizasyonu sağlar.
// ==========================================================================

using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kargo ücreti yönetim servisi.
    /// Veritabanından dinamik fiyat okuma ve admin güncelleme işlemlerini yönetir.
    /// </summary>
    public class ShippingManager : IShippingService
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // BAĞIMLILIKLAR
        // ═══════════════════════════════════════════════════════════════════════════════
        
        private readonly ECommerceDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ShippingManager> _logger;

        // Cache key'leri - tutarlılık için sabit tanımlanıyor
        private const string CACHE_KEY_ALL_SETTINGS = "ShippingSettings_All";
        private const string CACHE_KEY_ACTIVE_SETTINGS = "ShippingSettings_Active";
        private const string CACHE_KEY_PRICE_PREFIX = "ShippingPrice_";
        
        // Cache süresi - sık güncellenmeyeceği için 30 dakika yeterli
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        // ═══════════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════════════

        public ShippingManager(
            ECommerceDbContext context,
            IMemoryCache cache,
            ILogger<ShippingManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // SORGULAMA METODLARI
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<IEnumerable<ShippingSettingDto>> GetAllSettingsAsync()
        {
            // Cache kontrolü - admin paneli için bile cache kullan
            if (_cache.TryGetValue(CACHE_KEY_ALL_SETTINGS, out IEnumerable<ShippingSettingDto>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var settings = await _context.ShippingSettings
                    .AsNoTracking()
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => MapToDto(s))
                    .ToListAsync();

                // Cache'e ekle
                _cache.Set(CACHE_KEY_ALL_SETTINGS, settings, CacheDuration);

                _logger.LogDebug("Tüm kargo ayarları veritabanından yüklendi. Toplam: {Count}", settings.Count);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarları yüklenirken hata oluştu");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ShippingSettingDto>> GetActiveSettingsAsync()
        {
            // Cache kontrolü - müşteri sepeti için performans kritik
            if (_cache.TryGetValue(CACHE_KEY_ACTIVE_SETTINGS, out IEnumerable<ShippingSettingDto>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var settings = await _context.ShippingSettings
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => MapToDto(s))
                    .ToListAsync();

                // Cache'e ekle
                _cache.Set(CACHE_KEY_ACTIVE_SETTINGS, settings, CacheDuration);

                _logger.LogDebug("Aktif kargo ayarları yüklendi. Toplam: {Count}", settings.Count);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif kargo ayarları yüklenirken hata oluştu");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<decimal?> GetPriceByVehicleTypeAsync(string vehicleType)
        {
            if (string.IsNullOrWhiteSpace(vehicleType))
            {
                _logger.LogWarning("GetPriceByVehicleTypeAsync: Boş vehicleType parametresi");
                return null;
            }

            // Normalize et (küçük harf, trim)
            var normalizedType = vehicleType.Trim().ToLowerInvariant();
            var cacheKey = $"{CACHE_KEY_PRICE_PREFIX}{normalizedType}";

            // Cache kontrolü
            if (_cache.TryGetValue(cacheKey, out decimal cachedPrice))
            {
                return cachedPrice;
            }

            try
            {
                var setting = await _context.ShippingSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.VehicleType.ToLower() == normalizedType && s.IsActive);

                if (setting == null)
                {
                    _logger.LogWarning("Araç tipi için kargo ayarı bulunamadı: {VehicleType}", vehicleType);
                    return null;
                }

                // Cache'e ekle
                _cache.Set(cacheKey, setting.Price, CacheDuration);

                return setting.Price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo fiyatı sorgulanırken hata: {VehicleType}", vehicleType);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ShippingSettingDto?> GetByIdAsync(int id)
        {
            try
            {
                var setting = await _context.ShippingSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                return setting != null ? MapToDto(setting) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarı ID ile sorgulanırken hata: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ShippingSettingDto?> GetByVehicleTypeAsync(string vehicleType)
        {
            if (string.IsNullOrWhiteSpace(vehicleType))
                return null;

            var normalizedType = vehicleType.Trim().ToLowerInvariant();

            try
            {
                var setting = await _context.ShippingSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.VehicleType.ToLower() == normalizedType);

                return setting != null ? MapToDto(setting) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarı vehicleType ile sorgulanırken hata: {VehicleType}", vehicleType);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // GÜNCELLEME METODLARI (ADMIN)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<bool> UpdateSettingAsync(
            int id, 
            ShippingSettingUpdateDto updateDto, 
            int updatedByUserId, 
            string updatedByUserName)
        {
            if (updateDto == null)
            {
                _logger.LogWarning("UpdateSettingAsync: Null updateDto parametresi");
                return false;
            }

            try
            {
                var setting = await _context.ShippingSettings.FindAsync(id);
                if (setting == null)
                {
                    _logger.LogWarning("Güncellenecek kargo ayarı bulunamadı: {Id}", id);
                    return false;
                }

                // Partial update - sadece null olmayan alanları güncelle
                if (updateDto.Price.HasValue)
                {
                    // Negatif fiyat kontrolü
                    if (updateDto.Price.Value < 0)
                    {
                        _logger.LogWarning("Negatif kargo ücreti reddedildi: {Price}", updateDto.Price.Value);
                        return false;
                    }
                    setting.Price = updateDto.Price.Value;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.DisplayName))
                    setting.DisplayName = updateDto.DisplayName.Trim();

                if (!string.IsNullOrWhiteSpace(updateDto.EstimatedDeliveryTime))
                    setting.EstimatedDeliveryTime = updateDto.EstimatedDeliveryTime.Trim();

                if (updateDto.Description != null)
                    setting.Description = updateDto.Description.Trim();

                if (updateDto.SortOrder.HasValue)
                    setting.SortOrder = updateDto.SortOrder.Value;

                if (updateDto.MaxWeight.HasValue)
                    setting.MaxWeight = updateDto.MaxWeight.Value;

                if (updateDto.IsActive.HasValue)
                    setting.IsActive = updateDto.IsActive.Value;

                // Audit bilgileri
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = updatedByUserId;
                setting.UpdatedByUserName = updatedByUserName;

                await _context.SaveChangesAsync();

                // Cache'i temizle - güncel veri için
                InvalidateCache();

                _logger.LogInformation(
                    "Kargo ayarı güncellendi. Id: {Id}, VehicleType: {VehicleType}, Yeni Fiyat: {Price}, Güncelleyen: {UserName}",
                    id, setting.VehicleType, setting.Price, updatedByUserName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarı güncellenirken hata: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ToggleActiveAsync(int id, bool isActive, int updatedByUserId, string updatedByUserName)
        {
            try
            {
                var setting = await _context.ShippingSettings.FindAsync(id);
                if (setting == null)
                {
                    _logger.LogWarning("Aktiflik değiştirilecek kargo ayarı bulunamadı: {Id}", id);
                    return false;
                }

                setting.IsActive = isActive;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = updatedByUserId;
                setting.UpdatedByUserName = updatedByUserName;

                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateCache();

                _logger.LogInformation(
                    "Kargo ayarı aktiflik değiştirildi. Id: {Id}, IsActive: {IsActive}, Güncelleyen: {UserName}",
                    id, isActive, updatedByUserName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarı aktiflik değiştirilirken hata: {Id}", id);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ESKİ METODLAR (Geriye Dönük Uyumluluk)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <inheritdoc />
        public async Task<decimal> CalculateShippingCostAsync(int orderId)
        {
            // Eski metod - Order'dan shipping method'u okuyup fiyat döndür
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("CalculateShippingCostAsync: Sipariş bulunamadı: {OrderId}", orderId);
                    return 40m; // Varsayılan motosiklet fiyatı
                }

                var price = await GetPriceByVehicleTypeAsync(order.ShippingMethod);
                return price ?? 40m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş kargo ücreti hesaplanırken hata: {OrderId}", orderId);
                return 40m; // Hata durumunda varsayılan
            }
        }

        /// <inheritdoc />
        public async Task<string> GetEstimatedDeliveryAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return "30-45 dakika";

                var setting = await GetByVehicleTypeAsync(order.ShippingMethod);
                return setting?.EstimatedDeliveryTime ?? "30-45 dakika";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmini teslimat süresi sorgulanırken hata: {OrderId}", orderId);
                return "30-45 dakika";
            }
        }

        /// <inheritdoc />
        public async Task<bool> ShipOrderAsync(int orderId)
        {
            // Bu metod OrderStateMachine tarafından yönetiliyor
            // Geriye dönük uyumluluk için true döndür
            await Task.CompletedTask;
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Entity'yi DTO'ya dönüştürür.
        /// </summary>
        private static ShippingSettingDto MapToDto(ShippingSetting entity)
        {
            return new ShippingSettingDto
            {
                Id = entity.Id,
                VehicleType = entity.VehicleType,
                DisplayName = entity.DisplayName,
                Price = entity.Price,
                EstimatedDeliveryTime = entity.EstimatedDeliveryTime,
                Description = entity.Description,
                SortOrder = entity.SortOrder,
                MaxWeight = entity.MaxWeight,
                MaxVolume = entity.MaxVolume,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt,
                UpdatedByUserName = entity.UpdatedByUserName
            };
        }

        /// <summary>
        /// Tüm kargo cache'lerini temizler.
        /// Güncelleme sonrası çağrılmalı.
        /// </summary>
        private void InvalidateCache()
        {
            _cache.Remove(CACHE_KEY_ALL_SETTINGS);
            _cache.Remove(CACHE_KEY_ACTIVE_SETTINGS);
            // Fiyat cache'leri için wildcard silme desteklenmediğinden
            // motorcycle ve car için manuel sil
            _cache.Remove($"{CACHE_KEY_PRICE_PREFIX}motorcycle");
            _cache.Remove($"{CACHE_KEY_PRICE_PREFIX}car");
            
            _logger.LogDebug("Kargo ayarları cache'i temizlendi");
        }
    }
}
