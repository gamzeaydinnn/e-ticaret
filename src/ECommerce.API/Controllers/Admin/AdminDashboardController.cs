using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using ECommerce.Business.Services.Interfaces;
using ECommerce.API.Authorization;
using ECommerce.Core.DTOs.Admin;
using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using ECommerce.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.AllStaff)]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ECommerceDbContext _dbContext;
        private readonly IAdminCatalogStatsService _adminCatalogStatsService;
        private readonly InventorySettings _inventorySettings;

        public AdminDashboardController(
            IOrderService orderService,
            ECommerceDbContext dbContext,
            IAdminCatalogStatsService adminCatalogStatsService,
            IOptions<InventorySettings> inventoryOptions
        )
        {
            _orderService = orderService;
            _dbContext = dbContext;
            _adminCatalogStatsService = adminCatalogStatsService;
            _inventorySettings = inventoryOptions.Value;
        }

        [HttpGet("overview")]
        [HasPermission(Permissions.Dashboard.View)]
        public async Task<IActionResult> GetDashboardOverview()
        {
            try
            {
                var overview = await BuildDashboardOverviewAsync();
                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Dashboard verileri alınamadı: " + ex.Message
                });
            }
        }

        [HttpGet("stats")]
        [HasPermission(Permissions.Dashboard.View)]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var overview = await BuildDashboardOverviewAsync();
                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Dashboard verileri alınamadı: " + ex.Message
                });
            }
        }

        private async Task<AdminDashboardOverviewDto> BuildDashboardOverviewAsync()
        {
            var turkeyToday = OrderCancelPolicy.GetTurkeyNow().Date;
            var last14DaysStartUtc = OrderReportHelper.TurkeyToUtc(turkeyToday.AddDays(-13));
            var todayStartUtc = OrderReportHelper.TurkeyToUtc(turkeyToday);
            var tomorrowStartUtc = OrderReportHelper.TurkeyToUtc(turkeyToday.AddDays(1));
            var topProductsSinceUtc = OrderReportHelper.TurkeyToUtc(turkeyToday.AddDays(-90));
            var criticalThreshold = Math.Max(1, _inventorySettings.CriticalStockThreshold);

            // Kullanıcı / kurye — doğrudan COUNT
            var totalUsers = await _dbContext.Users.CountAsync();
            var activeCouriers = await _dbContext.Couriers.CountAsync(c =>
                c.IsOnline || c.Status == "active" || c.Status == "busy");

            // Ürün / stok sayıları: Ürünler ile aynı kaynak (web aktif + fiyatlı katalog)
            var productSnapshots = await _adminCatalogStatsService.GetProductSnapshotsAsync(
                HttpContext.RequestAborted);
            var pricedActive = productSnapshots
                .Where(p => p.IsActive && p.Price > 0m)
                .ToList();
            var totalProducts = pricedActive.Count;
            var outOfStockCount = pricedActive.Count(p => p.StockQuantity <= 0);
            var lowStockCount = pricedActive.Count(p =>
                p.StockQuantity > 0 && p.StockQuantity <= criticalThreshold);

            // Sipariş durum dağılımı — pending/delivered/cancelled buradan türetilir
            var orderStatusRaw = await _dbContext.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalOrders = orderStatusRaw.Sum(x => x.Count);
            var pendingOrders = orderStatusRaw
                .Where(x => x.Status is OrderStatus.New or OrderStatus.Pending or OrderStatus.Confirmed
                    or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Assigned
                    or OrderStatus.PickedUp or OrderStatus.OutForDelivery or OrderStatus.InTransit)
                .Sum(x => x.Count);
            var deliveredOrders = orderStatusRaw
                .Where(x => x.Status is OrderStatus.Delivered or OrderStatus.Completed)
                .Sum(x => x.Count);
            var cancelledOrders = orderStatusRaw
                .Where(x => x.Status == OrderStatus.Cancelled)
                .Sum(x => x.Count);
            var refundedOrders = orderStatusRaw
                .Where(x => x.Status is OrderStatus.Refunded or OrderStatus.PartialRefund)
                .Sum(x => x.Count);

            var orderStatusDistribution = orderStatusRaw
                .Select(x => new AdminDashboardStatusCountDto
                {
                    Label = x.Status.ToString(),
                    Count = x.Count
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var todayOrders = await _dbContext.Orders
                .AsNoTracking()
                .CountAsync(o => o.OrderDate >= todayStartUtc && o.OrderDate < tomorrowStartUtc);

            var totalRevenue = await _orderService.GetTotalRevenueAsync();

            var pendingRefundRequests = await _dbContext.RefundRequests
                .AsNoTracking()
                .CountAsync(r => r.Status == RefundRequestStatus.Pending);
            var failedRefunds = await _dbContext.RefundRequests
                .AsNoTracking()
                .CountAsync(r => r.Status == RefundRequestStatus.RefundFailed);
            var totalRefundedAmount = await _dbContext.RefundRequests
                .AsNoTracking()
                .Where(r => r.Status == RefundRequestStatus.Refunded ||
                            r.Status == RefundRequestStatus.AutoCancelled)
                .SumAsync(r => (decimal?)r.RefundAmount) ?? 0;

            // 14 günlük metrik — sadece gerekli kolonlar
            var dailyRaw = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= last14DaysStartUtc)
                .Select(o => new
                {
                    o.OrderDate,
                    o.Status,
                    o.PaymentStatus,
                    o.CapturedAmount,
                    o.FinalAmount,
                    o.FinalPrice,
                    o.TotalPrice
                })
                .ToListAsync();

            var dailyGrouped = dailyRaw
                .GroupBy(o => OrderCancelPolicy.ConvertUtcToTurkey(o.OrderDate).Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Orders = g.Count(o => OrderReportHelper.IsCountableSaleOrder(o.Status, o.PaymentStatus)),
                    Revenue = g
                        .Where(o => OrderReportHelper.IsCountableSaleOrder(o.Status, o.PaymentStatus))
                        .Sum(o =>
                            o.CapturedAmount > 0 ? o.CapturedAmount :
                            o.FinalAmount > 0 ? o.FinalAmount :
                            o.FinalPrice > 0 ? o.FinalPrice :
                            o.TotalPrice)
                })
                .ToList();

            var dailyMap = dailyGrouped.ToDictionary(x => x.Date, x => x);
            var dailyMetrics = new List<AdminDashboardMetricPointDto>();
            for (var i = 13; i >= 0; i--)
            {
                var day = turkeyToday.AddDays(-i);
                if (dailyMap.TryGetValue(day, out var existing))
                {
                    dailyMetrics.Add(new AdminDashboardMetricPointDto
                    {
                        Date = day.ToString("yyyy-MM-dd"),
                        Orders = existing.Orders,
                        Revenue = existing.Revenue
                    });
                }
                else
                {
                    dailyMetrics.Add(new AdminDashboardMetricPointDto
                    {
                        Date = day.ToString("yyyy-MM-dd"),
                        Orders = 0,
                        Revenue = 0m
                    });
                }
            }

            var paymentStatusDistribution = await _dbContext.Orders
                .AsNoTracking()
                .GroupBy(o => o.PaymentStatus)
                .Select(g => new AdminDashboardStatusCountDto
                {
                    Label = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var userRoleDistribution = await _dbContext.Users
                .AsNoTracking()
                .GroupBy(u => u.Role ?? "User")
                .Select(g => new AdminDashboardStatusCountDto
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var recentOrderRows = await _dbContext.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.CustomerName,
                    o.UserId,
                    Amount = o.FinalPrice > 0 ? o.FinalPrice : o.TotalPrice,
                    o.Status,
                    o.OrderDate
                })
                .ToListAsync();

            var recentOrders = recentOrderRows.Select(o => new AdminDashboardRecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = string.IsNullOrWhiteSpace(o.CustomerName)
                    ? (o.UserId.HasValue ? $"Kullanıcı #{o.UserId.Value}" : "Misafir Kullanıcı")
                    : o.CustomerName!,
                Amount = o.Amount,
                Status = o.Status.ToString(),
                Date = o.OrderDate.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            // En çok satanlar — son 90 gün
            List<AdminDashboardTopProductDto> topProducts;
            try
            {
                topProducts = await _dbContext.OrderItems
                    .AsNoTracking()
                    .Where(oi => oi.Order!.OrderDate >= topProductsSinceUtc)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new AdminDashboardTopProductDto
                    {
                        ProductId = g.Key,
                        Name = g.Select(x => x.Product!.Name).FirstOrDefault() ?? "Ürün",
                        Sales = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(6)
                    .ToListAsync();
            }
            catch
            {
                // Join çevirisi başarısız olursa boş liste — dashboard çökmesin
                topProducts = new List<AdminDashboardTopProductDto>();
            }

            return new AdminDashboardOverviewDto
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TodayOrders = todayOrders,
                ActiveCouriers = activeCouriers,
                OutOfStockCount = outOfStockCount,
                LowStockCount = lowStockCount,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                RefundedOrders = refundedOrders,
                PendingRefundRequests = pendingRefundRequests,
                FailedRefunds = failedRefunds,
                TotalRefundedAmount = totalRefundedAmount,
                DailyMetrics = dailyMetrics,
                OrderStatusDistribution = orderStatusDistribution,
                PaymentStatusDistribution = paymentStatusDistribution,
                UserRoleDistribution = userRoleDistribution,
                RecentOrders = recentOrders,
                TopProducts = topProducts
            };
        }
    }
}
