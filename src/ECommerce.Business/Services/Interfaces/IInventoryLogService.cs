using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IInventoryLogService
    {
        Task WriteAsync(int productId, string action, decimal quantity, decimal oldStock, decimal newStock, string? referenceId);
    }
}
