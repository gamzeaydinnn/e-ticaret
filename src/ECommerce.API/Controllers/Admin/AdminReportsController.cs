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

            var topProducts = saleOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItem>())
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    productId = g.Key,
                    productName = productNameMap.TryGetValue(g.Key, out var name) ? name : $"Ürün #{g.Key}",
                    quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.quantity)
                .Take(5)
                .ToList();

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
                topProducts
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
