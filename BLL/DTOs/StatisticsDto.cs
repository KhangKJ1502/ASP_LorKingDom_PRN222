using System;
using System.Collections.Generic;

namespace BLL.DTOs
{
    /// <summary>
    /// DTO cho Dashboard Statistics (Venue Statistics)
    /// </summary>
    public class DashboardStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int PendingOrders { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisMonth { get; set; }
        public int NewCustomersThisMonth { get; set; }

        // Top selling products
        public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();

        // Revenue by month (last 12 months)
        public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();

        // Order status breakdown
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();

        // Revenue trend (last 7 days)
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new();
    }

    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// DTO cho Product Statistics
    /// </summary>
    public class ProductStatisticsDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int DiscontinuedProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }

        // Sales by category
        public List<CategorySalesDto> SalesByCategory { get; set; } = new();

        // Top performing products
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Worst performing products
        public List<TopSellingProductDto> WorstProducts { get; set; } = new();

        // Stock alerts
        public List<LowStockProductDto> LowStockAlerts { get; set; } = new();

        // Sales by brand
        public List<BrandSalesDto> SalesByBrand { get; set; } = new();
    }

    public class CategorySalesDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class BrandSalesDto
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public int CurrentStock { get; set; }
        public int RecommendedStock { get; set; }
        public string? MainImageUrl { get; set; }
    }
}
