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
                OrderDate = o.OrderDate
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
    }
}
