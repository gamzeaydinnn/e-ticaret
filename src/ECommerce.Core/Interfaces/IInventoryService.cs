using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Inventory;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.DTOs.Cart;

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

        // Stok rezervasyon yönetimi
        Task<bool> ReserveStockAsync(Guid clientOrderId, IEnumerable<CartItemDto> items);
        Task ReleaseReservationAsync(Guid clientOrderId);
        Task CommitReservationAsync(Guid clientOrderId);

        /// <summary>
        /// Checkout commit ile düşülen satılabilir stoku geri yükler.
        /// Master: Product.StockQuantity; varyant varsa ProductVariant.Stock.
        /// </summary>
        Task RestoreOrderStockAsync(
            IEnumerable<OrderStockRestoreLineDto> lines,
            string logAction,
            string reference);
    }
}
//stok artır/azalt.
