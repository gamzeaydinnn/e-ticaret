using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.API.Authorization;
using ECommerce.Core.DTOs.Admin;
using ECommerce.Data.Context;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
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
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly ICategoryService _categoryService;
        private readonly ICartService _cartService;
        private readonly IFavoriteService _favoriteService;
        private readonly ICourierService _courierService;
        private readonly IPaymentService _paymentService;
        private readonly ECommerceDbContext _dbContext;

        public AdminDashboardController(
            IUserService userService,
            IProductService productService,
            IOrderService orderService,
            ICategoryService categoryService,
            ICartService cartService,
            IFavoriteService favoriteService,
            ICourierService courierService,
            IPaymentService paymentService,
            ECommerceDbContext dbContext
        )
        {
            _userService = userService;
            _productService = productService;
            _orderService = orderService;
            _categoryService = categoryService;
            _cartService = cartService;
            _favoriteService = favoriteService;
            _courierService = courierService;
            _paymentService = paymentService;
            _dbContext = dbContext;
        }

        [HttpGet("stats")]
        [HasPermission(Permissions.Dashboard.View)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var overview = await BuildDashboardOverviewAsync();
            return Ok(overview);
        }

        [HttpGet("overview")]
        [HasPermission(Permissions.Dashboard.View)]
        public async Task<IActionResult> GetDashboardOverview()
        {
            var overview = await BuildDashboardOverviewAsync();
            return Ok(overview);
        }

        private async Task<AdminDashboardOverviewDto> BuildDashboardOverviewAsync()
        {
            var utcNow = DateTime.UtcNow;
            var todayStart = utcNow.Date;
            var last14DaysStart = todayStart.AddDays(-13);

            var totalUsers = await _userService.GetUserCountAsync();
            var totalProducts = await _productService.GetProductCountAsync();
            var totalOrders = await _orderService.GetOrderCountAsync();
            var totalRevenue = await _orderService.GetTotalRevenueAsync();
            var todayOrders = await _orderService.GetTodayOrderCountAsync();

            var activeCouriers = await _dbContext.Couriers.CountAsync(c =>
                c.IsOnline || c.Status == "active" || c.Status == "busy");

            var pendingOrders = await _dbContext.Orders.CountAsync(o =>
                o.Status == OrderStatus.New ||
                o.Status == OrderStatus.Pending ||
                o.Status == OrderStatus.Confirmed ||
                o.Status == OrderStatus.Preparing ||
                o.Status == OrderStatus.Ready ||
                o.Status == OrderStatus.Assigned ||
                o.Status == OrderStatus.PickedUp ||
                o.Status == OrderStatus.OutForDelivery ||
                o.Status == OrderStatus.InTransit);

            var deliveredOrders = await _dbContext.Orders.CountAsync(o =>
                o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed);

            var dailyRaw = await _dbContext.Orders
                .Where(o => o.OrderDate >= last14DaysStart)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Orders = g.Count(),
                    Revenue = g.Where(o =>
                            o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed)
                        .Sum(o => o.FinalPrice > 0 ? o.FinalPrice : o.TotalPrice)
                })
                .ToListAsync();

            var dailyMap = dailyRaw.ToDictionary(x => x.Date, x => x);
            var dailyMetrics = new List<AdminDashboardMetricPointDto>();
            for (var i = 13; i >= 0; i--)
            {
                var day = todayStart.AddDays(-i);
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

            var orderStatusRaw = await _dbContext.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            var orderStatusDistribution = orderStatusRaw
                .Select(x => new AdminDashboardStatusCountDto
                {
                    Label = x.Status.ToString(),
                    Count = x.Count
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var paymentStatusRaw = await _dbContext.Orders
                .GroupBy(o => o.PaymentStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
            var paymentStatusDistribution = paymentStatusRaw
                .Select(x => new AdminDashboardStatusCountDto
                {
                    Label = x.Status.ToString(),
                    Count = x.Count
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var userRoleDistribution = await _dbContext.Users
                .GroupBy(u => u.Role ?? "User")
                .Select(g => new AdminDashboardStatusCountDto
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var recentOrderRows = await _dbContext.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .Select(o => new
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    UserId = o.UserId,
                    Amount = o.FinalPrice > 0 ? o.FinalPrice : o.TotalPrice,
                    Status = o.Status,
                    OrderDate = o.OrderDate
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

            var topProducts = await _dbContext.OrderItems
                .Join(
                    _dbContext.Products,
                    oi => oi.ProductId,
                    p => p.Id,
                    (oi, p) => new { oi.ProductId, ProductName = p.Name, oi.Quantity, oi.UnitPrice }
                )
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new AdminDashboardTopProductDto
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.ProductName,
                    Sales = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(x => x.Sales)
                .Take(6)
                .ToListAsync();

            var stats = new
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalCategories = await _categoryService.GetCategoryCountAsync(),
                TotalCarts = await _cartService.GetCartCountAsync(),
                TotalFavorites = 0, // Geçici olarak 0 - userId parametresi gerekli
                TotalCouriers = await _courierService.GetCourierCountAsync(),
                TotalPayments = await _paymentService.GetPaymentCountAsync(),
                TodayOrders = todayOrders,
                Revenue = totalRevenue
            };
            
            return new AdminDashboardOverviewDto
            {
                TotalUsers = stats.TotalUsers,
                TotalProducts = stats.TotalProducts,
                TotalOrders = stats.TotalOrders,
                TotalRevenue = stats.Revenue,
                TodayOrders = stats.TodayOrders,
                ActiveCouriers = activeCouriers,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
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
