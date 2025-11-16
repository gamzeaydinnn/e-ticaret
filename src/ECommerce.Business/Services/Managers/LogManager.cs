using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Logs;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Business.Services.Managers
{
    public class LogManager : IAdminLogService
    {
        private readonly ECommerceDbContext _dbContext;

        public LogManager(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryParameters parameters)
        {
            var query = _dbContext.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(parameters.EntityType))
            {
                var entityType = parameters.EntityType.Trim();
                query = query.Where(l => l.EntityName == entityType);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Action))
            {
                var action = parameters.Action.Trim();
                query = query.Where(l => l.Action == action);
            }

            if (parameters.StartDate.HasValue)
            {
                var start = parameters.StartDate.Value;
                query = query.Where(l => l.CreatedAt >= start);
            }

            if (parameters.EndDate.HasValue)
            {
                var end = parameters.EndDate.Value;
                query = query.Where(l => l.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var search = parameters.Search.Trim();
                query = query.Where(l =>
                    EF.Functions.Like(l.Action, $"%{search}%") ||
                    EF.Functions.Like(l.EntityName, $"%{search}%") ||
                    (l.OldValues != null && EF.Functions.Like(l.OldValues, $"%{search}%")) ||
                    (l.NewValues != null && EF.Functions.Like(l.NewValues, $"%{search}%")));
            }

            var skip = NormalizeSkip(parameters.Skip);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(parameters.Take)
                .Select(l => new AuditLogDto
                {
                    Id = l.Id,
                    AdminUserId = l.UserId,
                    Action = l.Action,
                    EntityType = l.EntityName,
                    EntityId = l.EntityId.HasValue ? l.EntityId.Value.ToString() : null,
                    OldValues = l.OldValues,
                    NewValues = l.NewValues,
                    PerformedBy = l.PerformedBy,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AuditLogDto>(items, total, skip, parameters.Take);
        }

        public async Task<PagedResult<ErrorLogDto>> GetErrorLogsAsync(ErrorLogQueryParameters parameters)
        {
            var query = _dbContext.ErrorLogs.AsNoTracking();

            if (parameters.StartDate.HasValue)
            {
                var start = parameters.StartDate.Value;
                query = query.Where(l => l.CreatedAt >= start);
            }

            if (parameters.EndDate.HasValue)
            {
                var end = parameters.EndDate.Value;
                query = query.Where(l => l.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Path))
            {
                var path = parameters.Path.Trim();
                query = query.Where(l => l.Path != null && EF.Functions.Like(l.Path, $"%{path}%"));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Method))
            {
                var method = parameters.Method.Trim();
                query = query.Where(l => l.Method != null && l.Method == method);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var term = parameters.Search.Trim();
                query = query.Where(l =>
                    EF.Functions.Like(l.Message, $"%{term}%") ||
                    (l.StackTrace != null && EF.Functions.Like(l.StackTrace, $"%{term}%")));
            }

            var skip = NormalizeSkip(parameters.Skip);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(parameters.Take)
                .Select(l => new ErrorLogDto
                {
                    Id = l.Id,
                    Message = l.Message,
                    StackTrace = l.StackTrace,
                    Path = l.Path,
                    Method = l.Method,
                    UserId = l.UserId,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<ErrorLogDto>(items, total, skip, parameters.Take);
        }

        public async Task<PagedResult<SystemLogDto>> GetSystemLogsAsync(SystemLogQueryParameters parameters)
        {
            var query = _dbContext.MicroSyncLogs.AsNoTracking();

            if (parameters.StartDate.HasValue)
            {
                var start = parameters.StartDate.Value;
                query = query.Where(l => l.CreatedAt >= start);
            }

            if (parameters.EndDate.HasValue)
            {
                var end = parameters.EndDate.Value;
                query = query.Where(l => l.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(parameters.EntityType))
            {
                var entityType = parameters.EntityType.Trim();
                query = query.Where(l => l.EntityType == entityType);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Status))
            {
                var status = parameters.Status.Trim();
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Direction))
            {
                var direction = parameters.Direction.Trim();
                query = query.Where(l => l.Direction == direction);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var term = parameters.Search.Trim();
                query = query.Where(l =>
                    EF.Functions.Like(l.Message, $"%{term}%") ||
                    (l.LastError != null && EF.Functions.Like(l.LastError, $"%{term}%")));
            }

            var skip = NormalizeSkip(parameters.Skip);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(parameters.Take)
                .Select(l => new SystemLogDto
                {
                    Id = l.Id,
                    EntityType = l.EntityType,
                    ExternalId = l.ExternalId,
                    InternalId = l.InternalId,
                    Direction = l.Direction,
                    Status = l.Status,
                    Attempts = l.Attempts,
                    LastError = l.LastError,
                    LastAttemptAt = l.LastAttemptAt,
                    CreatedAt = l.CreatedAt,
                    Message = l.Message
                })
                .ToListAsync();

            return new PagedResult<SystemLogDto>(items, total, skip, parameters.Take);
        }

        private static int NormalizeSkip(int skip) => skip < 0 ? 0 : skip;
    }
}
