using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// MikroSyncState ve MicroSyncLog repository implementasyonu.
    /// 
    /// NEDEN: Senkronizasyon durumlarını ve loglarını veritabanında yönetir.
    /// Delta sync için LastSyncTime takibi, hata yönetimi için log kayıtları.
    /// </summary>
    public class MikroSyncRepository : IMikroSyncRepository
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<MikroSyncRepository> _logger;

        public MikroSyncRepository(
            ECommerceDbContext context,
            ILogger<MikroSyncRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== SYNC STATE İŞLEMLERİ ====================

        /// <inheritdoc />
        public async Task<MikroSyncState?> GetSyncStateAsync(
            string syncType,
            string direction,
            CancellationToken cancellationToken = default)
        {
            return await _context.MikroSyncStates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.SyncType == syncType && s.Direction == direction,
                    cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MikroSyncState> UpsertSyncStateAsync(
            MikroSyncState state,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.MikroSyncStates
                .FirstOrDefaultAsync(
                    s => s.SyncType == state.SyncType && s.Direction == state.Direction,
                    cancellationToken);

            if (existing == null)
            {
                // Yeni kayıt
                state.CreatedAt = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
                _context.MikroSyncStates.Add(state);
                
                _logger.LogInformation(
                    "[MikroSyncRepository] Yeni sync state oluşturuldu. Tip: {Type}, Yön: {Direction}",
                    state.SyncType, state.Direction);
            }
            else
            {
                // Mevcut kaydı güncelle
                existing.LastSyncTime = state.LastSyncTime;
                existing.LastSyncCount = state.LastSyncCount;
                existing.LastSyncDurationMs = state.LastSyncDurationMs;
                existing.LastSyncSuccess = state.LastSyncSuccess;
                existing.LastError = state.LastError;
                existing.ConsecutiveFailures = state.ConsecutiveFailures;
                existing.IsEnabled = state.IsEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
                
                state = existing;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return state;
        }

        /// <inheritdoc />
        public async Task UpdateSyncSuccessAsync(
            string syncType,
            string direction,
            int processedCount,
            long durationMs,
            CancellationToken cancellationToken = default)
        {
            var state = await GetSyncStateAsync(syncType, direction, cancellationToken) 
                ?? new MikroSyncState { SyncType = syncType, Direction = direction };

            state.LastSyncTime = DateTime.UtcNow;
            state.LastSyncCount = processedCount;
            state.LastSyncDurationMs = durationMs;
            state.LastSyncSuccess = true;
            state.LastError = null;
            state.ConsecutiveFailures = 0; // Başarılı olunca sıfırla

            await UpsertSyncStateAsync(state, cancellationToken);

            _logger.LogInformation(
                "[MikroSyncRepository] Sync başarılı. Tip: {Type}, Yön: {Direction}, " +
                "Kayıt: {Count}, Süre: {Duration}ms",
                syncType, direction, processedCount, durationMs);
        }

        /// <inheritdoc />
        public async Task UpdateSyncFailureAsync(
            string syncType,
            string direction,
            string errorMessage,
            CancellationToken cancellationToken = default)
        {
            var state = await GetSyncStateAsync(syncType, direction, cancellationToken)
                ?? new MikroSyncState { SyncType = syncType, Direction = direction };

            state.LastSyncSuccess = false;
            state.LastError = errorMessage?.Length > 1000 
                ? errorMessage.Substring(0, 1000) 
                : errorMessage;
            state.ConsecutiveFailures++;
            state.UpdatedAt = DateTime.UtcNow;

            await UpsertSyncStateAsync(state, cancellationToken);

            _logger.LogWarning(
                "[MikroSyncRepository] Sync başarısız. Tip: {Type}, Yön: {Direction}, " +
                "Ardışık hata: {Failures}, Hata: {Error}",
                syncType, direction, state.ConsecutiveFailures, errorMessage);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MikroSyncState>> GetAllSyncStatesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.MikroSyncStates
                .AsNoTracking()
                .OrderBy(s => s.SyncType)
                .ThenBy(s => s.Direction)
                .ToListAsync(cancellationToken);
        }

        // ==================== SYNC LOG İŞLEMLERİ ====================

        /// <inheritdoc />
        public async Task<MicroSyncLog> CreateLogAsync(
            MicroSyncLog log,
            CancellationToken cancellationToken = default)
        {
            log.CreatedAt = DateTime.UtcNow;
            _context.MicroSyncLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
            return log;
        }

        /// <inheritdoc />
        public async Task UpdateLogAsync(
            MicroSyncLog log,
            CancellationToken cancellationToken = default)
        {
            _context.MicroSyncLogs.Update(log);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MicroSyncLog?> GetLastLogAsync(
            string entityType,
            string? externalId = null,
            string? internalId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MicroSyncLogs
                .AsNoTracking()
                .Where(l => l.EntityType == entityType);

            if (!string.IsNullOrEmpty(externalId))
                query = query.Where(l => l.ExternalId == externalId);

            if (!string.IsNullOrEmpty(internalId))
                query = query.Where(l => l.InternalId == internalId);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MicroSyncLog>> GetPendingLogsAsync(
            string? entityType = null,
            int maxAttempts = 3,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MicroSyncLogs
                .AsNoTracking()
                .Where(l => l.Status == "Pending" || l.Status == "Failed")
                .Where(l => l.Attempts < maxAttempts);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);

            return await query
                .OrderBy(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MicroSyncLog>> GetLogsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? entityType = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MicroSyncLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= startDate && l.CreatedAt <= endDate);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(1000) // Performans limiti
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> GetFailedLogCountAsync(
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MicroSyncLogs
                .Where(l => l.Status == "Failed");

            if (since.HasValue)
                query = query.Where(l => l.CreatedAt >= since.Value);

            return await query.CountAsync(cancellationToken);
        }
    }
}
