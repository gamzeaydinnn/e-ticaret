using ECommerce.Entities.Enums;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace ECommerce.Business.Services.Managers
{
    public class OrderManager : IOrderService
    {
        private readonly ECommerceDbContext _context;

        public OrderManager(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null)
        {
            var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();

            if (userId.HasValue)
                query = query.Where(o => o.UserId == userId.Value);

            var orders = await query.ToListAsync();

            return orders.Select(o => new OrderListDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalPrice = o.TotalAmount,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate,
                TotalItems = o.OrderItems.Sum(oi => oi.Quantity)
                
            });
        }

        public async Task<OrderListDto?> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return new OrderListDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalPrice = order.TotalAmount,
                Status = order.Status.ToString(),
                OrderDate = order.OrderDate
            };
        }

        public async Task<OrderListDto> CreateAsync(OrderCreateDto dto)
        {
            var order = new Order
            {
                UserId = dto.UserId,
                TotalAmount = dto.TotalPrice,
                Status = OrderStatus.Pending,
                OrderDate = System.DateTime.UtcNow,
                OrderItems = dto.OrderItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(order.Id) ?? throw new System.Exception("Sipariş oluşturulamadı.");
        }

        public async Task UpdateAsync(int id, OrderUpdateDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;

            // Status string'ten enum'a çevrilmeli
            if (System.Enum.TryParse<OrderStatus>(dto.Status, out var statusEnum))
            {
                order.Status = statusEnum;
            }

            order.TotalAmount = dto.TotalPrice;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChangeOrderStatusAsync(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            if (System.Enum.TryParse<OrderStatus>(newStatus, out var statusEnum))
            {
                order.Status = statusEnum;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        // Admin panel için ek methodlar
        public async Task<int> GetOrderCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetTodayOrderCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .CountAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(int page = 1, int size = 20)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return orders.Select(o => new OrderListDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalPrice = o.TotalAmount,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate
            });
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && System.Enum.TryParse<OrderStatus>(status, out var statusEnum))
            {
                order.Status = statusEnum;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int count = 10)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();

            return orders.Select(o => new OrderListDto
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalPrice = o.TotalAmount,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate
            });
        }

        public async Task<OrderListDto> GetOrderByIdAsync(int id)
        {
            var result = await GetByIdAsync(id);
            return result ?? throw new System.Exception("Sipariş bulunamadı.");
        }
    }
}
