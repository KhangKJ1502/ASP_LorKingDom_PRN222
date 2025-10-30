using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IStatisticsRepository _repo;

        public StatisticsService(IStatisticsRepository repo)
        {
            _repo = repo;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            var dto = new DashboardStatisticsDto
            {
                TotalRevenue = await _repo.GetTotalRevenueAsync(),
                TotalOrders = await _repo.GetTotalOrdersAsync(),
                TotalCustomers = await _repo.GetTotalCustomersAsync(),
                TotalProducts = await _repo.GetTotalProductsAsync(),
                LowStockProducts = await _repo.GetLowStockProductCountAsync(),
                PendingOrders = await _repo.GetPendingOrderCountAsync(),
                RevenueToday = await _repo.GetRevenueTodayAsync(),
                RevenueThisMonth = await _repo.GetRevenueThisMonthAsync(),
                RevenueThisYear = await _repo.GetRevenueThisYearAsync(),
                OrdersToday = await _repo.GetOrdersTodayAsync(),
                OrdersThisMonth = await _repo.GetOrdersThisMonthAsync(),
                NewCustomersThisMonth = await _repo.GetNewCustomersThisMonthAsync()
            };

            // Top selling products
            var topProducts = await _repo.GetTopSellingProductsAsync(5);
            dto.TopSellingProducts = topProducts.Select(p => new TopSellingProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                MainImageUrl = p.ImageUrl,
                TotalSold = p.TotalSold,
                TotalRevenue = p.Revenue
            }).ToList();

            // Monthly revenues (last 12 months)
            var monthlyData = await _repo.GetMonthlyRevenuesAsync(12);
            dto.MonthlyRevenues = monthlyData.Select(m => new MonthlyRevenueDto
            {
                Year = m.Year,
                Month = m.Month,
                MonthName = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Revenue = m.Revenue,
                OrderCount = m.OrderCount
            }).ToList();

            // Orders by status
            dto.OrdersByStatus = await _repo.GetOrdersByStatusAsync();

            // Daily revenues (last 7 days)
            var dailyData = await _repo.GetDailyRevenuesAsync(7);
            dto.DailyRevenues = dailyData.Select(d => new DailyRevenueDto
            {
                Date = d.Date,
                DateLabel = d.Date.ToString("dd/MM"),
                Revenue = d.Revenue,
                OrderCount = d.OrderCount
            }).ToList();

            return dto;
        }

        public async Task<ProductStatisticsDto> GetProductStatisticsAsync()
        {
            var dto = new ProductStatisticsDto
            {
                TotalProducts = await _repo.GetTotalProductsAsync(),
                ActiveProducts = await _repo.GetActiveProductCountAsync(),
                OutOfStockProducts = await _repo.GetOutOfStockProductCountAsync(),
                DiscontinuedProducts = await _repo.GetDiscontinuedProductCountAsync(),
                LowStockProducts = await _repo.GetLowStockProductCountAsync(),
                TotalInventoryValue = await _repo.GetTotalInventoryValueAsync()
            };

            // Sales by category
            var categoryData = await _repo.GetSalesByCategoryAsync();
            dto.SalesByCategory = categoryData.Select(c => new CategorySalesDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ProductCount = c.ProductCount,
                TotalSold = c.TotalSold,
                TotalRevenue = c.Revenue
            }).ToList();

            // Top products
            var topProducts = await _repo.GetTopProductsAsync(10);
            dto.TopProducts = topProducts.Select(p => new TopSellingProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                MainImageUrl = p.ImageUrl,
                TotalSold = p.TotalSold,
                TotalRevenue = p.Revenue
            }).ToList();

            // Worst products
            var worstProducts = await _repo.GetWorstProductsAsync(10);
            dto.WorstProducts = worstProducts.Select(p => new TopSellingProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                MainImageUrl = p.ImageUrl,
                TotalSold = p.TotalSold,
                TotalRevenue = p.Revenue
            }).ToList();

            // Low stock alerts
            var lowStockData = await _repo.GetLowStockProductsAsync(10);
            dto.LowStockAlerts = lowStockData.Select(p => new LowStockProductDto
            {
                ProductId = p.ProductId,
                Sku = p.Sku,
                ProductName = p.ProductName,
                CategoryName = p.Category,
                BrandName = p.Brand,
                CurrentStock = p.Stock,
                RecommendedStock = 20, // Default recommended stock
                MainImageUrl = p.ImageUrl
            }).ToList();

            // Sales by brand
            var brandData = await _repo.GetSalesByBrandAsync();
            dto.SalesByBrand = brandData.Select(b => new BrandSalesDto
            {
                BrandId = b.BrandId,
                BrandName = b.BrandName,
                ProductCount = b.ProductCount,
                TotalSold = b.TotalSold,
                TotalRevenue = b.Revenue
            }).ToList();

            return dto;
        }
    }
}
