using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Logs;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAdminLogService
    {
        Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryParameters parameters);
        Task<PagedResult<ErrorLogDto>> GetErrorLogsAsync(ErrorLogQueryParameters parameters);
        Task<PagedResult<SystemLogDto>> GetSystemLogsAsync(SystemLogQueryParameters parameters);
    }
}
