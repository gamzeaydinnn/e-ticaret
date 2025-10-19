using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IOrderService
using ECommerce.Entities.Concrete;            // Order, OrderItem
using ECommerce.Core.Interfaces;              // IOrderRepository
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Enums;              // Order DTO


namespace ECommerce.Business.Services.Managers
{
    public class OrderManager : IOrderService
    {
        private readonly ECommerceDbContext _context;
        private readonly IInventoryService _inventoryService;

        public OrderManager(ECommerceDbContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
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
            // Checkout mantığına benzer: fiyatları yeniden hesapla ve OrderNumber üret
            decimal total = 0m;
            var items = new List<OrderItem>();
            foreach (var i in dto.OrderItems)
            {
                if (i.Quantity <= 0) throw new Exception("Geçersiz miktar");
                var product = await _context.Products.FindAsync(i.ProductId)
                    ?? throw new Exception($"Ürün bulunamadı: {i.ProductId}");
                var unitPrice = product.SpecialPrice ?? product.Price;
                total += unitPrice * i.Quantity;
                items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = i.Quantity,
                    UnitPrice = unitPrice
                });
            }

            var order = new Order
            {
                UserId = dto.UserId,
                OrderNumber = GenerateOrderNumber(),
                TotalPrice = total,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = string.IsNullOrWhiteSpace(dto.ShippingAddress) ? "-" : dto.ShippingAddress,
                ShippingCity = string.IsNullOrWhiteSpace(dto.ShippingCity) ? "-" : dto.ShippingCity,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = dto.CustomerEmail,
                DeliveryNotes = dto.DeliveryNotes,
                OrderItems = items
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
                UserId = order.UserId ?? 0,
                OrderNumber = order.OrderNumber,
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
                // 1) Stok + fiyat ön-kontrol, toplam hesapla
                decimal total = 0m;
                var items = new List<OrderItem>();
                foreach (var item in dto.OrderItems)
                {
                    if (item.Quantity <= 0)
                        throw new Exception("Geçersiz miktar");

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Ürün bulunamadı: {item.ProductId}");
                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Yetersiz stok: {product.Name}");

                    var unitPrice = product.SpecialPrice ?? product.Price;
                    total += unitPrice * item.Quantity;
                    items.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                // 2) Siparişi oluştur
                var order = new Order
                {
                    UserId = dto.UserId,
                    OrderNumber = GenerateOrderNumber(),
                    TotalPrice = total,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow,
                    ShippingAddress = dto.ShippingAddress,
                    ShippingCity = dto.ShippingCity,
                    CustomerName = dto.CustomerName,
                    CustomerPhone = dto.CustomerPhone,
                    CustomerEmail = dto.CustomerEmail,
                    DeliveryNotes = dto.DeliveryNotes,
                    OrderItems = items
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 3) Stok düşümü
                foreach (var item in order.OrderItems)
                {
                    var ok = await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        item.Quantity,
                        InventoryChangeType.Sale,
                        note: $"Online Order #{order.OrderNumber}",
                        performedByUserId: order.UserId
                    );
                    if (!ok) throw new Exception("Stok düşümü başarısız");
                }

                // 4) Transaction commit
                await transaction.CommitAsync();

                return await GetByIdAsync(order.Id) ?? throw new Exception("Sipariş oluşturulamadı.");
            }
            catch
            {
                await transaction.RollbackAsync(); // hata olursa geri al
                throw;
            }
        }

        private static string GenerateOrderNumber()
        {
            // Basit, benzersiz sipariş numarası (örnek): ORD-YYYYMMDD-HHMMSS-XXXX
            var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var rnd = Random.Shared.Next(1000, 9999);
            return $"ORD-{ts}-{rnd}";
        }

    }
}
