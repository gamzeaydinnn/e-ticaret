using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task WriteAsync(int adminUserId, string action, string entityType, string entityId, object? oldValue, object? newValue);
    }
}
