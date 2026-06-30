using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ECommerce.Data.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public OrderRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.UserId == userId && o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.IsActive);
        }

        public override async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.IsActive)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetCourierPendingWeightOrdersAsync(int courierId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o =>
                    o.IsActive &&
                    o.CourierId == courierId &&
                    o.HasWeightBasedItems &&
                    o.Status != OrderStatus.Cancelled &&
                    o.Status != OrderStatus.Refunded &&
                    o.Status != OrderStatus.Delivered)
                .Where(o => o.OrderItems.Any(i =>
                    !i.IsWeighed &&
                    (i.IsWeightBased || (i.Product != null && i.Product.IsWeightBased))))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
