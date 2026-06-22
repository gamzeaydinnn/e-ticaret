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
using System.Data.Common;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Sepet ayarları yönetim servisi.
    /// Veritabanından minimum sepet tutarı okuma ve admin güncelleme işlemlerini yönetir.
    /// </summary>
    public class CartSettingsManager : ICartSettingsService
    {
        private const string DefaultMinimumCartAmountMessage = "Sipariş verebilmek için sepet tutarınız en az {amount} TL olmalıdır.";
        private const string DefaultGuestFirstOrderShippingMessage = "Hesap oluştur, ilk alışverişinde kargo bedava!";
        private const int MaxMessageLength = 500;
        private const int MaxUpdatedByUserNameLength = 200;

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
                if (IsMissingGuestMessageColumnException(ex))
                {
                    _logger.LogWarning(ex, "CartSettings tablosunda GuestFirstOrderShippingMessage kolonu bulunamadı. Legacy fallback kullanılacak.");
                    return await GetLegacySettingsAsync();
                }

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
                if (IsMissingGuestMessageColumnException(ex))
                {
                    _logger.LogWarning(ex, "Admin CartSettings sorgusunda GuestFirstOrderShippingMessage kolonu eksik. Legacy fallback kullanılacak.");
                    return await GetLegacySettingsAsync();
                }

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

                // NEDEN: Admin mesajı boş gönderirse sistem varsayılanına dönülmeli.
                // null ise alan hiç değiştirilmez; boş string ise reset anlamına gelir.
                if (updateDto.MinimumCartAmountMessage != null)
                {
                    setting.MinimumCartAmountMessage = string.IsNullOrWhiteSpace(updateDto.MinimumCartAmountMessage)
                        ? DefaultMinimumCartAmountMessage
                        : Truncate(updateDto.MinimumCartAmountMessage.Trim(), MaxMessageLength);
                }

                // NEDEN: Admin alanı bilinçli olarak boş bırakırsa banner tamamen gizlenebilmelidir.
                // Bu yüzden burada null "değiştirme", boş string ise "temizle" anlamına gelir.
                if (updateDto.GuestFirstOrderShippingMessage != null)
                    setting.GuestFirstOrderShippingMessage = Truncate(updateDto.GuestFirstOrderShippingMessage.Trim(), MaxMessageLength);

                // Audit bilgileri
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = updatedByUserId;
                setting.UpdatedByUserName = Truncate(updatedByUserName, MaxUpdatedByUserNameLength);

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
                if (IsMissingGuestMessageColumnException(ex))
                {
                    _logger.LogWarning(ex, "CartSettings legacy şemada güncelleniyor. GuestFirstOrderShippingMessage kolonu atlanacak.");
                    return await UpdateLegacySettingsAsync(updateDto, updatedByUserId, updatedByUserName);
                }

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
                GuestFirstOrderShippingMessage = entity.GuestFirstOrderShippingMessage,
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
                MinimumCartAmountMessage = DefaultMinimumCartAmountMessage,
                GuestFirstOrderShippingMessage = DefaultGuestFirstOrderShippingMessage,
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

        private static bool IsMissingGuestMessageColumnException(Exception ex)
        {
            return ex.ToString().Contains("GuestFirstOrderShippingMessage", StringComparison.OrdinalIgnoreCase)
                && ex.ToString().Contains("Invalid column name", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<CartSettingsDto> GetLegacySettingsAsync()
        {
            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            try
            {
                return await ReadLegacySettingsAsync(connection);
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static async Task<CartSettingsDto> ReadLegacySettingsAsync(DbConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TOP 1
                    Id,
                    MinimumCartAmount,
                    IsMinimumCartAmountActive,
                    MinimumCartAmountMessage,
                    IsActive,
                    UpdatedAt,
                    UpdatedByUserName
                FROM CartSettings
                WHERE IsActive = 1
                ORDER BY Id";

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return GetDefaultSettings();
            }

            return new CartSettingsDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                MinimumCartAmount = reader.GetDecimal(reader.GetOrdinal("MinimumCartAmount")),
                IsMinimumCartAmountActive = reader.GetBoolean(reader.GetOrdinal("IsMinimumCartAmountActive")),
                MinimumCartAmountMessage = reader["MinimumCartAmountMessage"]?.ToString() ?? DefaultMinimumCartAmountMessage,
                GuestFirstOrderShippingMessage = DefaultGuestFirstOrderShippingMessage,
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                UpdatedByUserName = reader["UpdatedByUserName"]?.ToString()
            };
        }

        private async Task<bool> UpdateLegacySettingsAsync(
            CartSettingsUpdateDto updateDto,
            int updatedByUserId,
            string updatedByUserName)
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT TOP 1 Id FROM CartSettings WHERE IsActive = 1 ORDER BY Id";
            var existingId = await selectCommand.ExecuteScalarAsync();

            if (existingId == null || existingId == DBNull.Value)
            {
                await using var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO CartSettings
                    (MinimumCartAmount, IsMinimumCartAmountActive, MinimumCartAmountMessage, UpdatedByUserId, UpdatedByUserName, IsActive, CreatedAt, UpdatedAt)
                    VALUES
                    (@MinimumCartAmount, @IsMinimumCartAmountActive, @MinimumCartAmountMessage, @UpdatedByUserId, @UpdatedByUserName, 1, @CreatedAt, @UpdatedAt)";

                AddParameter(insertCommand, "@MinimumCartAmount", updateDto.MinimumCartAmount ?? 0m);
                AddParameter(insertCommand, "@IsMinimumCartAmountActive", updateDto.IsMinimumCartAmountActive ?? false);
                AddParameter(
                    insertCommand,
                    "@MinimumCartAmountMessage",
                    updateDto.MinimumCartAmountMessage != null && !string.IsNullOrWhiteSpace(updateDto.MinimumCartAmountMessage)
                        ? Truncate(updateDto.MinimumCartAmountMessage.Trim(), MaxMessageLength)
                        : DefaultMinimumCartAmountMessage);
                AddParameter(insertCommand, "@UpdatedByUserId", updatedByUserId);
                AddParameter(insertCommand, "@UpdatedByUserName", Truncate(updatedByUserName, MaxUpdatedByUserNameLength));
                AddParameter(insertCommand, "@CreatedAt", DateTime.UtcNow);
                AddParameter(insertCommand, "@UpdatedAt", DateTime.UtcNow);

                await insertCommand.ExecuteNonQueryAsync();
            }
            else
            {
                await using var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = @"
                    UPDATE CartSettings
                    SET
                        MinimumCartAmount = @MinimumCartAmount,
                        IsMinimumCartAmountActive = @IsMinimumCartAmountActive,
                        MinimumCartAmountMessage = @MinimumCartAmountMessage,
                        UpdatedByUserId = @UpdatedByUserId,
                        UpdatedByUserName = @UpdatedByUserName,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id";

                var current = await ReadLegacySettingsAsync(connection);
                AddParameter(updateCommand, "@Id", Convert.ToInt32(existingId));
                AddParameter(updateCommand, "@MinimumCartAmount", updateDto.MinimumCartAmount ?? current.MinimumCartAmount);
                AddParameter(updateCommand, "@IsMinimumCartAmountActive", updateDto.IsMinimumCartAmountActive ?? current.IsMinimumCartAmountActive);
                AddParameter(
                    updateCommand,
                    "@MinimumCartAmountMessage",
                    updateDto.MinimumCartAmountMessage == null
                        ? current.MinimumCartAmountMessage
                        : string.IsNullOrWhiteSpace(updateDto.MinimumCartAmountMessage)
                            ? DefaultMinimumCartAmountMessage
                            : Truncate(updateDto.MinimumCartAmountMessage.Trim(), MaxMessageLength));
                AddParameter(updateCommand, "@UpdatedByUserId", updatedByUserId);
                AddParameter(updateCommand, "@UpdatedByUserName", Truncate(updatedByUserName, MaxUpdatedByUserNameLength));
                AddParameter(updateCommand, "@UpdatedAt", DateTime.UtcNow);

                await updateCommand.ExecuteNonQueryAsync();
            }

            InvalidateCache();
            return true;
        }

        private static void AddParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
