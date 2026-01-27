using System;
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
using System.Text.RegularExpressions;
//Dönem içi sipariş sayısı, ciro, satılan adet ve top 5 ürün (OrderDate ve Delivered/Completed).
namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = Roles.AllStaff)] // CustomerSupport ve Logistics da raporlara erişebilsin
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
            DateTime from = DateTime.UtcNow.Date;
            if (period.Equals("weekly", StringComparison.OrdinalIgnoreCase))
                from = DateTime.UtcNow.Date.AddDays(-7);
            else if (period.Equals("monthly", StringComparison.OrdinalIgnoreCase))
                from = DateTime.UtcNow.Date.AddDays(-30);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.OrderDate >= from && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
                .ToListAsync();

            var ordersCount = orders.Count;
            var revenue = orders.Sum(o => o.TotalPrice);
            var itemsSold = orders.Sum(o => o.OrderItems.Sum(i => i.Quantity));
            var topProducts = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            return Ok(new
            {
                from,
                to = DateTime.UtcNow,
                ordersCount,
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
                .Where(p => p.IsActive && p.StockQuantity <= threshold)
                .Select(p => new { p.Id, p.Name, p.StockQuantity })
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            return Ok(new { threshold, products });
        }

        [HttpGet("inventory/movements")]
        public async Task<IActionResult> GetInventoryMovements([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = from ?? DateTime.UtcNow.Date.AddDays(-7);
            var end = to ?? DateTime.UtcNow;

            var movements = await _context.InventoryLogs
                .Include(l => l.Product)
                .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.ProductId,
                    ProductName = l.Product != null ? l.Product.Name : "",
                    l.Action,
                    l.Quantity,
                    l.OldStock,
                    l.NewStock,
                    l.ReferenceId,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { start, end, movements });
        }

        [HttpGet("erp/sync-status")]
        public async Task<IActionResult> GetErpSyncStatus([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = from ?? DateTime.UtcNow.Date.AddDays(-7);
            var end = to ?? DateTime.UtcNow;

            var logs = await _context.MicroSyncLogs
                .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
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

            return Ok(new { start, end, groups });
        }
    }
}
