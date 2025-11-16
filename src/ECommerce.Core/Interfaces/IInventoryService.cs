using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Order;
using System.Collections.Generic;

namespace ECommerce.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<bool> IncreaseStockAsync(int productId, int quantity);
        Task<bool> DecreaseStockAsync(int productId, int quantity);
        Task<int> GetStockLevelAsync(int productId);

        // Gelişmiş stok hareketi (log + sebep + bildirim)
        Task<bool> DecreaseStockAsync(int productId, int quantity, InventoryChangeType changeType, string? note = null, int? performedByUserId = null);
        Task<bool> IncreaseStockAsync(int productId, int quantity, InventoryChangeType changeType, string? note = null, int? performedByUserId = null);

        // Sepet/checkout öncesi merkezi stok doğrulaması
        Task<(bool Success, string? ErrorMessage)> ValidateStockForOrderAsync(IEnumerable<OrderItemDto> items);
    }
}
//stok artır/azalt.
