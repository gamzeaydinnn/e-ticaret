using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    public class InventoryLogService : IInventoryLogService
    {
        private readonly ECommerceDbContext _context;

        public InventoryLogService(ECommerceDbContext context)
        {
            _context = context;
        }

        public Task WriteAsync(int productId, string action, int quantity, int oldStock, int newStock, string? referenceId)
        {
            var log = new InventoryLog
            {
                ProductId = productId,
                Action = action,
                Quantity = quantity,
                OldStock = oldStock,
                NewStock = newStock,
                ReferenceId = referenceId
            };

            _context.InventoryLogs.Add(log);
            return _context.SaveChangesAsync();
        }
    }
}
