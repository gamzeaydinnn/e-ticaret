using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Admin
{
    public class AdminDashboardOverviewDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int ActiveCouriers { get; set; }
        // STOK İSTATİSTİKLERİ
        // OutOfStockCount: stoğu tamamen biten (StockQuantity <= 0) aktif ürün sayısı.
        // LowStockCount: kritik eşik altındaki (0 < stok <= eşik) aktif ürün sayısı.
        // NEDEN ayrı tutuluyor: "Stokta yok" ile "kritik/düşük stok" operasyonel olarak
        //   farklı aksiyon gerektirir; tek bir listede karışmamalı.
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        // İade istatistikleri
        public int CancelledOrders { get; set; }
        public int RefundedOrders { get; set; }
        public int PendingRefundRequests { get; set; }
        public int FailedRefunds { get; set; }
        public decimal TotalRefundedAmount { get; set; }
        public List<AdminDashboardMetricPointDto> DailyMetrics { get; set; } = new();
        public List<AdminDashboardStatusCountDto> OrderStatusDistribution { get; set; } = new();
        public List<AdminDashboardStatusCountDto> PaymentStatusDistribution { get; set; } = new();
        public List<AdminDashboardStatusCountDto> UserRoleDistribution { get; set; } = new();
        public List<AdminDashboardRecentOrderDto> RecentOrders { get; set; } = new();
        public List<AdminDashboardTopProductDto> TopProducts { get; set; } = new();
    }

    public class AdminDashboardMetricPointDto
    {
        public string Date { get; set; } = string.Empty;
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminDashboardStatusCountDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AdminDashboardRecentOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public class AdminDashboardTopProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sales { get; set; }
        public decimal Revenue { get; set; }
    }
}
