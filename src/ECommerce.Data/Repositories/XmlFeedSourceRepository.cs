// XmlFeedSourceRepository: XML feed kaynakları için repository implementasyonu.
// Feed tanımları, senkronizasyon durumu ve zamanlama bilgilerini yönetir.
// Background job'lar için senkronizasyon zamanlaması desteği sağlar.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// XML feed kaynakları için repository implementasyonu.
    /// Tedarikçi XML entegrasyonlarının veritabanı işlemleri.
    /// </summary>
    public class XmlFeedSourceRepository : BaseRepository<XmlFeedSource>, IXmlFeedSourceRepository
    {
        public XmlFeedSourceRepository(ECommerceDbContext context) : base(context)
        {
        }

        #region Sorgular

        /// <inheritdoc/>
        public async Task<XmlFeedSource?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(s => s.Name == name.Trim());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<XmlFeedSource>> GetActiveSourcesAsync()
        {
            return await _dbSet
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<XmlFeedSource>> GetAutoSyncEnabledSourcesAsync()
        {
            return await _dbSet
                .Where(s => s.IsActive && s.AutoSyncEnabled)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<XmlFeedSource>> GetSourcesDueForSyncAsync()
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(s => s.IsActive && 
                           s.AutoSyncEnabled && 
                           s.NextSyncAt != null && 
                           s.NextSyncAt <= now)
                .OrderBy(s => s.NextSyncAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<XmlFeedSource>> GetBySupplierNameAsync(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                return Enumerable.Empty<XmlFeedSource>();

            return await _dbSet
                .Where(s => s.SupplierName != null && 
                           s.SupplierName.Contains(supplierName))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        #endregion

        #region Senkronizasyon Durumu

        /// <inheritdoc/>
        public async Task UpdateSyncSuccessAsync(int sourceId, int createdCount, int updatedCount, int failedCount)
        {
            var source = await _dbSet.FindAsync(sourceId);
            if (source == null)
                return;

            var now = DateTime.UtcNow;

            source.LastSyncAt = now;
            source.LastSyncSuccess = true;
            source.LastSyncError = null; // Başarılı olduğunda hatayı temizle
            source.LastSyncCreatedCount = createdCount;
            source.LastSyncUpdatedCount = updatedCount;
            source.LastSyncFailedCount = failedCount;
            source.TotalSyncCount++;
            source.UpdatedAt = now;

            // Bir sonraki senkronizasyonu planla
            if (source.AutoSyncEnabled && source.SyncIntervalMinutes.HasValue && source.SyncIntervalMinutes > 0)
            {
                source.NextSyncAt = now.AddMinutes(source.SyncIntervalMinutes.Value);
            }
            else
            {
                source.NextSyncAt = null;
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateSyncFailureAsync(int sourceId, string errorMessage)
        {
            var source = await _dbSet.FindAsync(sourceId);
            if (source == null)
                return;

            var now = DateTime.UtcNow;

            source.LastSyncAt = now;
            source.LastSyncSuccess = false;
            source.LastSyncError = TruncateErrorMessage(errorMessage, 2000);
            source.TotalSyncCount++;
            source.UpdatedAt = now;

            // Hata durumunda da bir sonraki senkronizasyonu planla
            // (retry mekanizması için)
            if (source.AutoSyncEnabled && source.SyncIntervalMinutes.HasValue && source.SyncIntervalMinutes > 0)
            {
                // Hata durumunda biraz daha geç dene (en az 15 dk)
                var retryInterval = Math.Max(source.SyncIntervalMinutes.Value, 15);
                source.NextSyncAt = now.AddMinutes(retryInterval);
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task ScheduleNextSyncAsync(int sourceId)
        {
            var source = await _dbSet.FindAsync(sourceId);
            if (source == null)
                return;

            if (source.AutoSyncEnabled && source.SyncIntervalMinutes.HasValue && source.SyncIntervalMinutes > 0)
            {
                source.NextSyncAt = DateTime.UtcNow.AddMinutes(source.SyncIntervalMinutes.Value);
                source.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Hata mesajını maksimum uzunluğa kısaltır.
        /// </summary>
        private string TruncateErrorMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            if (message.Length <= maxLength)
                return message;

            return message.Substring(0, maxLength - 3) + "...";
        }

        #endregion

        #region Benzersizlik Kontrolleri

        /// <inheritdoc/>
        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var query = _dbSet.Where(s => s.Name == name.Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> UrlExistsAsync(string url, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var query = _dbSet.Where(s => s.Url == url.Trim() && s.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        #endregion

        #region İstatistikler

        /// <inheritdoc/>
        public async Task<int> GetActiveSourceCountAsync()
        {
            return await _dbSet.CountAsync(s => s.IsActive);
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalSyncCountAsync()
        {
            return await _dbSet.SumAsync(s => s.TotalSyncCount);
        }

        /// <inheritdoc/>
        public async Task<int> GetRecentFailureCountAsync()
        {
            var since = DateTime.UtcNow.AddHours(-24);

            return await _dbSet
                .CountAsync(s => s.LastSyncAt >= since && s.LastSyncSuccess == false);
        }

        #endregion
    }
}
