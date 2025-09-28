using ECommerce.Entities.Enums;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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

            return orders.Select(o => MapToDto(o));
        }

        public async Task<OrderListDto?> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order != null ? MapToDto(order) : null;
        }

        public async Task<OrderListDto> CreateAsync(OrderCreateDto dto)
        {
            var order = new Order
            {
                UserId = dto.UserId,
                TotalPrice = dto.TotalPrice,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                OrderItems = dto.OrderItems.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(order.Id) ?? throw new Exception("Sipariş oluşturulamadı.");
        }

        public async Task UpdateAsync(int id, OrderUpdateDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return;

            // DTO'dan string -> enum
            if (Enum.TryParse<OrderStatus>(dto.Status, out var statusEnum))
            {
                order.Status = statusEnum;
            }

            order.TotalPrice = dto.TotalPrice;
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

            if (Enum.TryParse<OrderStatus>(newStatus, out var statusEnum))
            {
                order.Status = statusEnum;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<int> GetOrderCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetTodayOrderCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders.CountAsync(o => o.OrderDate.Date == today);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalPrice);
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

            return orders.Select(o => MapToDto(o));
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && Enum.TryParse<OrderStatus>(status, out var statusEnum))
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

            return orders.Select(o => MapToDto(o));
        }

        public async Task<OrderListDto> GetOrderByIdAsync(int id)
        {
            var result = await GetByIdAsync(id);
            return result ?? throw new Exception("Sipariş bulunamadı.");
        }

        // --------------------------
        // Helper method: Order -> DTO
        private OrderListDto MapToDto(Order order)
        {
            return new OrderListDto
            {
                Id = order.Id,
                UserId = order.UserId,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                OrderDate = order.OrderDate,
                TotalItems = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0
            };
        }
        //stok rezervasyonu + ödeme + sipariş kaydı
        public async Task<OrderListDto> CheckoutAsync(OrderCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); // transaction başlat

            try
            {
                // 1. Stok kontrol ve düşme
                foreach (var item in dto.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Ürün bulunamadı: {item.ProductId}");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Yetersiz stok: {product.Name}");

                    product.StockQuantity -= item.Quantity; // stok düş
                }

                // 2. Siparişi oluştur
                var order = new Order
                {
                    UserId = dto.UserId,
                    TotalPrice = dto.TotalPrice,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow,
                    OrderItems = dto.OrderItems.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                _context.Orders.Add(order);

                // 3. Ödeme işlemi (örnek olarak bir ödeme servisi çağrısı)
                var paymentSuccess = true; // ödeme servisi çağrılır, sonucu true/false döner
                if (!paymentSuccess)
                    throw new Exception("Ödeme başarısız");

                await _context.SaveChangesAsync();

                // 4. Transaction commit
                await transaction.CommitAsync();

                return await GetByIdAsync(order.Id) ?? throw new Exception("Sipariş oluşturulamadı.");
            }
            catch
            {
                await transaction.RollbackAsync(); // hata olursa geri al
                throw;
            }
        }

    }
}
