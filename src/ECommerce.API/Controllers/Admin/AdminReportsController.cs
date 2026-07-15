using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Core.Constants;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Entities.Enums;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using System.Text.RegularExpressions;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = Roles.AllStaff)]
    public class AdminReportsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly InventorySettings _inventorySettings;

        public AdminReportsController(ECommerceDbContext context, IOptions<InventorySettings> inventoryOptions)
        {
            _context = context;
            _inventorySettings = inventoryOptions.Value;
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport([FromQuery] string period = "daily")
        {
            var (fromUtc, toUtcExclusive) = OrderReportHelper.GetPeriodRangeUtc(period);
            var turkeyToday = OrderCancelPolicy.GetTurkeyNow().Date;
            var turkeyStart = period.Equals("weekly", StringComparison.OrdinalIgnoreCase)
                ? turkeyToday.AddDays(-6)
                : period.Equals("monthly", StringComparison.OrdinalIgnoreCase)
                    ? turkeyToday.AddDays(-29)
                    : turkeyToday;

            var ordersInPeriod = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.OrderDate >= fromUtc && o.OrderDate < toUtcExclusive)
                .ToListAsync();

            // Siparişler sayfasıyla uyum: dönemdeki TÜM siparişler sayılır.
            var ordersCount = ordersInPeriod.Count;

            var saleOrders = ordersInPeriod
                .Where(o => OrderReportHelper.IsCountableSaleOrder(o.Status, o.PaymentStatus))
                .ToList();

            var revenue = saleOrders.Sum(OrderReportHelper.GetSaleAmount);
            var itemsSold = saleOrders.Sum(o =>
                (o.OrderItems ?? Enumerable.Empty<OrderItem>()).Sum(i => i.Quantity));

            var productIds = saleOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItem>())
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            var productNameMap = productIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name);

            var productStats = saleOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItem>())
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    productId = g.Key,
                    productName = productNameMap.TryGetValue(g.Key, out var name) ? name : $"Ürün #{g.Key}",
                    quantity = g.Sum(x => x.Quantity),
                    revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                    orderCount = g.Select(x => x.OrderId).Distinct().Count()
                })
                .ToList();

            var topProducts = productStats
                .OrderByDescending(x => x.quantity)
                .ThenByDescending(x => x.revenue)
                .Take(10)
                .ToList();

            var leastProducts = productStats
                .OrderBy(x => x.quantity)
                .ThenBy(x => x.revenue)
                .Take(10)
                .ToList();

            var soldProductIds = productStats.Select(x => x.productId).ToHashSet();
            var activeProductCount = await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.IsActive);
            var zeroSalesProductCount = await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.IsActive && !soldProductIds.Contains(p.Id));

            // İade edilen ürünler — dönem içinde iade olmuş siparişlerden
            var refundedOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o =>
                    (o.Status == OrderStatus.Refunded || o.Status == OrderStatus.PartialRefund)
                    && (
                        (o.RefundedAt != null && o.RefundedAt >= fromUtc && o.RefundedAt < toUtcExclusive)
                        || (o.RefundedAt == null && o.OrderDate >= fromUtc && o.OrderDate < toUtcExclusive)
                    ))
                .ToListAsync();

            var refundRequestRows = await _context.RefundRequests
                .AsNoTracking()
                .Where(r =>
                    r.Status == RefundRequestStatus.Refunded
                    && (
                        (r.RefundedAt != null && r.RefundedAt >= fromUtc && r.RefundedAt < toUtcExclusive)
                        || (r.RefundedAt == null && r.ProcessedAt != null && r.ProcessedAt >= fromUtc && r.ProcessedAt < toUtcExclusive)
                        || (r.RefundedAt == null && r.ProcessedAt == null && r.RequestedAt >= fromUtc && r.RequestedAt < toUtcExclusive)
                    ))
                .Select(r => new { r.OrderId, r.RefundAmount })
                .ToListAsync();

            var refundRequestOrderIds = refundRequestRows
                .Select(r => r.OrderId)
                .Distinct()
                .ToList();

            if (refundRequestOrderIds.Count > 0)
            {
                var missingOrderIds = refundRequestOrderIds
                    .Where(id => refundedOrders.All(o => o.Id != id))
                    .ToList();
                if (missingOrderIds.Count > 0)
                {
                    var extraOrders = await _context.Orders
                        .AsNoTracking()
                        .Include(o => o.OrderItems)
                        .Where(o => missingOrderIds.Contains(o.Id))
                        .ToListAsync();
                    refundedOrders.AddRange(extraOrders);
                }
            }

            // Stok iade logları — kısmi iade satır detayı için
            var refundInventoryLogs = await _context.InventoryLogs
                .AsNoTracking()
                .Where(l =>
                    l.CreatedAt >= fromUtc
                    && l.CreatedAt < toUtcExclusive
                    && l.Action == LocalInventoryPolicy.LogActionRefund)
                .ToListAsync();

            var refundAgg = new Dictionary<int, (decimal Quantity, decimal Amount, HashSet<int> OrderIds)>();

            void AddRefund(int productId, decimal qty, decimal amount, int? orderId)
            {
                if (productId <= 0 || qty <= 0) return;
                if (!refundAgg.TryGetValue(productId, out var current))
                {
                    current = (0m, 0m, new HashSet<int>());
                }

                current.Quantity += qty;
                current.Amount += amount;
                if (orderId.HasValue && orderId.Value > 0)
                {
                    current.OrderIds.Add(orderId.Value);
                }

                refundAgg[productId] = current;
            }

            foreach (var order in refundedOrders)
            {
                var isFullRefund = order.Status == OrderStatus.Refunded;
                foreach (var item in order.OrderItems ?? Enumerable.Empty<OrderItem>())
                {
                    // Kısmi iadede stok logu varsa satırları oradan al; yoksa tüm kalemleri göster
                    if (!isFullRefund && refundInventoryLogs.Count > 0)
                    {
                        continue;
                    }

                    var qty = Math.Abs(item.Quantity);
                    AddRefund(item.ProductId, qty, item.UnitPrice * qty, order.Id);
                }
            }

            foreach (var log in refundInventoryLogs)
            {
                var qty = Math.Abs(log.Quantity);
                int? orderId = null;
                if (int.TryParse(log.ReferenceId, out var parsedOrderId))
                {
                    orderId = parsedOrderId;
                }

                // Aynı siparişte tam iade satırları zaten eklendiyse çift sayımı azalt
                if (orderId.HasValue
                    && refundedOrders.Any(o => o.Id == orderId.Value && o.Status == OrderStatus.Refunded)
                    && refundAgg.ContainsKey(log.ProductId))
                {
                    continue;
                }

                AddRefund(log.ProductId, qty, 0m, orderId);
            }

            var refundProductIds = refundAgg.Keys.ToList();
            var refundNameMap = refundProductIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Products
                    .AsNoTracking()
                    .Where(p => refundProductIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name);

            var refundedProducts = refundAgg
                .Select(kvp => new
                {
                    productId = kvp.Key,
                    productName = refundNameMap.TryGetValue(kvp.Key, out var name) ? name : $"Ürün #{kvp.Key}",
                    quantity = (int)Math.Round(kvp.Value.Quantity),
                    amount = Math.Round(kvp.Value.Amount, 2),
                    orderCount = kvp.Value.OrderIds.Count
                })
                .OrderByDescending(x => x.quantity)
                .ThenByDescending(x => x.amount)
                .Take(20)
                .ToList();

            var refundAmountTotal = refundRequestRows.Sum(r => r.RefundAmount);
            if (refundAmountTotal <= 0)
            {
                refundAmountTotal = refundAgg.Values.Sum(v => v.Amount);
            }

            return Ok(new
            {
                from = fromUtc,
                to = toUtcExclusive,
                period,
                periodStart = turkeyStart.ToString("yyyy-MM-dd"),
                periodEnd = turkeyToday.ToString("yyyy-MM-dd"),
                ordersCount,
                netOrdersCount = saleOrders.Count,
                revenue,
                itemsSold,
                soldProductCount = productStats.Count,
                zeroSalesProductCount,
                activeProductCount,
                refundOrdersCount = refundedOrders.Count,
                refundAmountTotal,
                refundedItemCount = refundedProducts.Sum(x => x.quantity),
                topProducts,
                leastProducts,
                refundedProducts
            });
        }

        [HttpGet("stock/low")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            var threshold = Math.Max(1, _inventorySettings.CriticalStockThreshold);

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.StockQuantity <= threshold)
                .Select(p => new { p.Id, p.Name, p.StockQuantity })
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            var outOfStockCount = products.Count(p => p.StockQuantity <= 0);
            var lowStockCount = products.Count - outOfStockCount;

            return Ok(new { threshold, outOfStockCount, lowStockCount, products });
        }

        [HttpGet("inventory/movements")]
        public async Task<IActionResult> GetInventoryMovements([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var (startUtc, endUtcExclusive) = OrderReportHelper.GetDateRangeUtc(from, to);

            var movements = await _context.InventoryLogs
                .AsNoTracking()
                .Include(l => l.Product)
                .Where(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtcExclusive)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.ProductId,
                    ProductName = l.Product != null ? l.Product.Name : "",
                    changeType = l.Action,
                    changeQuantity = l.Quantity,
                    l.OldStock,
                    l.NewStock,
                    l.ReferenceId,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { start = startUtc, end = endUtcExclusive, movements });
        }

        [HttpGet("erp/sync-status")]
        public async Task<IActionResult> GetErpSyncStatus([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var (startUtc, endUtcExclusive) = OrderReportHelper.GetDateRangeUtc(from, to);

            var logs = await _context.MicroSyncLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtcExclusive)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var groups = logs
                .GroupBy(l => new { l.EntityType, l.Direction })
                .Select(g =>
                {
                    var latest = g.OrderByDescending(x => x.CreatedAt).First();
                    var lastSuccess = g.Where(x => x.Status == "Success").OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                    var lastError = g.Where(x => x.Status != "Success" && x.LastError != null).OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                    int? updatedCount = null;
                    if (!string.IsNullOrWhiteSpace(lastSuccess?.Message))
                    {
                        var m = Regex.Match(lastSuccess.Message, "(\\d+)");
                        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                        {
                            updatedCount = n;
                        }
                    }

                    return new
                    {
                        entity = g.Key.EntityType,
                        direction = g.Key.Direction,
                        lastAttemptAt = latest.LastAttemptAt ?? latest.CreatedAt,
                        lastStatus = latest.Status,
                        lastSuccessAt = lastSuccess?.CreatedAt,
                        lastMessage = latest.Message,
                        lastError = lastError?.LastError,
                        updatedCount,
                        totalAttempts = g.Sum(x => x.Attempts),
                        recentCount = g.Count()
                    };
                })
                .OrderBy(x => x.entity)
                .ThenBy(x => x.direction)
                .ToList();

            return Ok(new { start = startUtc, end = endUtcExclusive, groups });
        }
    }
}
