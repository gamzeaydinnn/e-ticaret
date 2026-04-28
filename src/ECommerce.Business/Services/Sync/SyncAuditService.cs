using System;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Sync denetim kaydı servisi — kritik sync olaylarını kalıcı olarak saklar.
    /// 
    /// NEDEN: In-memory log'lar container restart'ında kaybolur. Bu servis çakışma
    /// kararları, circuit breaker geçişleri ve alert geçmişini DB'ye yazar.
    /// Admin panelden sorgulanabilir audit trail sağlar.
    /// </summary>
    public interface ISyncAuditService
    {
        Task LogConflictAsync(string entityId, string conflictType, string strategy,
            string message, object? details = null, string? correlationId = null);

        Task LogCircuitBreakerEventAsync(string source, string state,
            string message, object? details = null, string? correlationId = null);

        Task LogAlertAsync(string source, string message, string severity = "Warning",
            object? details = null, string? correlationId = null);

        Task LogDeadLetterAsync(string entityId, string source,
            string message, object? details = null, string? correlationId = null);

        Task LogRetryEventAsync(string entityId, string source,
            string message, int attempt, bool succeeded, string? correlationId = null);

        Task LogManualActionAsync(string entityId, string source,
            string message, string? userId = null, string? correlationId = null);
    }

    public class SyncAuditService : ISyncAuditService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<SyncAuditService> _logger;

        // JSON serialize ayarları — Details alanı için
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public SyncAuditService(
            ECommerceDbContext context,
            ILogger<SyncAuditService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogConflictAsync(string entityId, string conflictType, string strategy,
            string message, object? details = null, string? correlationId = null)
        {
            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "Conflict",
                Severity = "Warning",
                Source = $"SyncConflict/{conflictType}",
                EntityId = entityId,
                Message = $"[{strategy}] {message}",
                Details = SerializeDetails(details ?? new { ConflictType = conflictType, Strategy = strategy }),
                CorrelationId = correlationId
            });
        }

        public async Task LogCircuitBreakerEventAsync(string source, string state,
            string message, object? details = null, string? correlationId = null)
        {
            var severity = state == "Open" ? "Error" : state == "HalfOpen" ? "Warning" : "Info";

            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "CircuitBreaker",
                Severity = severity,
                Source = source,
                Message = $"[CB:{state}] {message}",
                Details = SerializeDetails(details ?? new { State = state }),
                CorrelationId = correlationId
            });
        }

        public async Task LogAlertAsync(string source, string message, string severity = "Warning",
            object? details = null, string? correlationId = null)
        {
            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "Alert",
                Severity = severity,
                Source = source,
                Message = message,
                Details = SerializeDetails(details),
                CorrelationId = correlationId
            });
        }

        public async Task LogDeadLetterAsync(string entityId, string source,
            string message, object? details = null, string? correlationId = null)
        {
            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "DeadLetter",
                Severity = "Error",
                Source = source,
                EntityId = entityId,
                Message = message,
                Details = SerializeDetails(details),
                CorrelationId = correlationId
            });
        }

        public async Task LogRetryEventAsync(string entityId, string source,
            string message, int attempt, bool succeeded, string? correlationId = null)
        {
            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "Retry",
                Severity = succeeded ? "Info" : "Warning",
                Source = source,
                EntityId = entityId,
                Message = $"[Attempt#{attempt}] {(succeeded ? "✓" : "✗")} {message}",
                Details = SerializeDetails(new { Attempt = attempt, Succeeded = succeeded }),
                CorrelationId = correlationId
            });
        }

        public async Task LogManualActionAsync(string entityId, string source,
            string message, string? userId = null, string? correlationId = null)
        {
            await WriteAuditAsync(new SyncAuditLog
            {
                EventType = "ManualAction",
                Severity = "Info",
                Source = source,
                EntityId = entityId,
                Message = message,
                Details = SerializeDetails(userId != null ? new { UserId = userId } : null),
                CorrelationId = correlationId
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // Private
        // ════════════════════════════════════════════════════════════════════

        private async Task WriteAuditAsync(SyncAuditLog log)
        {
            try
            {
                log.CreatedAt = DateTime.UtcNow;
                _context.SyncAuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit log yazımı asıl işlemi engellemeMEli — fire-and-forget log
                _logger.LogWarning(ex,
                    "[SyncAudit] Audit log yazımı başarısız. EventType: {EventType}, Entity: {Entity}",
                    log.EventType, log.EntityId);
            }
        }

        private static string? SerializeDetails(object? details)
        {
            if (details == null) return null;
            try
            {
                return JsonSerializer.Serialize(details, JsonOptions);
            }
            catch
            {
                return details.ToString();
            }
        }
    }
}
