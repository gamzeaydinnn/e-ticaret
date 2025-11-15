using System;
using System.Collections.Generic;
using System.Text.Json;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    public class LoggerService : ILogService
    {
        private readonly ILogger<LoggerService> _logger;
        private readonly ECommerceDbContext _dbContext;

        public LoggerService(ILogger<LoggerService> logger, ECommerceDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public void Info(string message, IDictionary<string, object>? context = null)
        {
            if (context == null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                _logger.LogInformation("{Message} {@Context}", message, context);
            }
        }

        public void Warn(string message, IDictionary<string, object>? context = null)
        {
            if (context == null)
            {
                _logger.LogWarning(message);
            }
            else
            {
                _logger.LogWarning("{Message} {@Context}", message, context);
            }
        }

        public void Error(Exception exception, string message, IDictionary<string, object>? context = null)
        {
            if (context == null)
            {
                _logger.LogError(exception, message);
            }
            else
            {
                _logger.LogError(exception, "{Message} {@Context}", message, context);
            }
        }

        public void Audit(string action, string entityName, int? entityId, object? oldValues, object? newValues, string? performedBy = null)
        {
            try
            {
                var audit = new AuditLogs
                {
                    Action = action ?? string.Empty,
                    EntityName = entityName ?? string.Empty,
                    EntityId = entityId,
                    OldValues = SerializeSafely(oldValues),
                    NewValues = SerializeSafely(newValues),
                    PerformedBy = performedBy,
                };

                _dbContext.AuditLogs.Add(audit);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // Audit yazarken hata olsa bile ana akışı bozmamak için sadece logla
                _logger.LogError(ex, "Audit log yazılırken hata oluştu");
            }
        }

        private static string? SerializeSafely(object? value)
        {
            if (value == null) return null;
            try
            {
                return JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }
    }
}

