using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IInventoryLogService
    {
        Task WriteAsync(int productId, string action, int quantity, int oldStock, int newStock, string? referenceId);
    }
}
