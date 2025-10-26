
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Order;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Managers
{
    public class OrderManager : IOrderService
    {
        private readonly ECommerceDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ECommerce.Business.Services.Interfaces.INotificationService? _notificationService;

        public OrderManager(ECommerceDbContext context, IInventoryService inventoryService, ECommerce.Business.Services.Interfaces.INotificationService? notificationService = null)
        {
            _context = context;
            _inventoryService = inventoryService;
            _notificationService = notificationService;
        }

        // Siparişin tam detayını getir (fatura için)
        public async Task<OrderDetailDto?> GetDetailByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return null;
            return new OrderDetailDto
            {
                Id = order.Id,
                UserId = order.UserId ?? 0,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
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
                // Shipping will be computed server-side (do not trust client-provided cost)
                ShippingMethod = NormalizeShippingMethod(dto.ShippingMethod),
                ShippingCost = ComputeShippingCost(NormalizeShippingMethod(dto.ShippingMethod)),
                TotalPrice = total + ComputeShippingCost(NormalizeShippingMethod(dto.ShippingMethod)),
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

        public async Task<OrderListDto> CheckoutAsync(OrderCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                // Compute shipping server-side (whitelist + fixed costs)
                var shippingMethod = NormalizeShippingMethod(dto.ShippingMethod);
                var shippingCost = ComputeShippingCost(shippingMethod);
                total += shippingCost;

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
                    OrderItems = items,
                    ShippingMethod = shippingMethod,
                    ShippingCost = shippingCost
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
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
                await transaction.CommitAsync();

                // Fire-and-forget notification (do not block checkout)
                if (_notificationService != null)
                {
                    _ = _notificationService.SendOrderConfirmationAsync(order.Id);
                }

                return await GetByIdAsync(order.Id) ?? throw new Exception("Sipariş oluşturulamadı.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != userId)
                return false;
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                return false;
            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
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

        public async Task<OrderListDto> GetOrderByIdAsync(int id)
        {
            var result = await GetByIdAsync(id);
            return result ?? throw new Exception("Sipariş bulunamadı.");
        }

        public async Task UpdateOrderStatusAsync(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && Enum.TryParse<OrderStatus>(status, out var statusEnum))
            {
                var previous = order.Status;
                order.Status = statusEnum;
                await _context.SaveChangesAsync();

                // If status transitioned to Delivered, notify customer (if available)
                if (previous != OrderStatus.Delivered && statusEnum == OrderStatus.Delivered && _notificationService != null)
                {
                    // If you have a tracking number in real flow, pass it; here we pass empty string
                    _ = _notificationService.SendShipmentNotificationAsync(order.Id, trackingNumber: string.Empty);
                }
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
                TotalItems = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
                ShippingMethod = order.ShippingMethod,
                ShippingCost = order.ShippingCost,
                ShippingAddress = order.ShippingAddress,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                DeliveryNotes = order.DeliveryNotes ?? string.Empty,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
        }

        // Normalize incoming shipping method and map to a known key
        private static string NormalizeShippingMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method)) return "car";
            var m = method.Trim().ToLowerInvariant();
            // accept some common variants (english/turkish)
            if (m == "motokurye" || m == "motorcycle" || m == "motor") return "motorcycle";
            if (m == "araç" || m == "arac" || m == "car") return "car";
            // default fallback
            return "car";
        }

        // Fixed shipping cost mapping for allowed shipping methods
        private static decimal ComputeShippingCost(string method)
        {
            var m = method?.Trim().ToLowerInvariant() ?? "car";
            return m == "motorcycle" ? 15m : 30m;
        }

        private static string GenerateOrderNumber()
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var rnd = Random.Shared.Next(1000, 9999);
            return $"ORD-{ts}-{rnd}";
        }
    }
}
