using System;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    public class AuditLogService : IAuditLogService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly ECommerceDbContext _dbContext;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ECommerceDbContext dbContext, ILogger<AuditLogService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task WriteAsync(int adminUserId, string action, string entityType, string entityId, object? oldValue, object? newValue)
        {
            try
            {
                var entry = new AuditLogs
                {
                    UserId = adminUserId,
                    Action = action ?? string.Empty,
                    EntityName = entityType ?? string.Empty,
                    EntityId = TryParseEntityId(entityId),
                    OldValues = Serialize(oldValue),
                    NewValues = Serialize(newValue),
                    PerformedBy = adminUserId > 0 ? adminUserId.ToString() : null,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.AuditLogs.Add(entry);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log could not be written for {Action} on {EntityType} ({EntityId})", action, entityType, entityId);
            }
        }

        private static int TryParseEntityId(string? entityId)
        {
            if (!string.IsNullOrWhiteSpace(entityId) && int.TryParse(entityId, out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static string? Serialize(object? value)
        {
            if (value == null) return null;

            try
            {
                return JsonSerializer.Serialize(value, SerializerOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
