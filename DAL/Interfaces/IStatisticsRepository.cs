using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IStatisticsRepository
    {
        // Dashboard Statistics
        Task<decimal> GetTotalRevenueAsync();
        Task<int> GetTotalOrdersAsync();
        Task<int> GetTotalCustomersAsync();
        Task<int> GetTotalProductsAsync();
        Task<int> GetLowStockProductCountAsync(int threshold = 10);
        Task<int> GetPendingOrderCountAsync();
        Task<decimal> GetRevenueTodayAsync();
        Task<decimal> GetRevenueThisMonthAsync();
        Task<decimal> GetRevenueThisYearAsync();
        Task<int> GetOrdersTodayAsync();
        Task<int> GetOrdersThisMonthAsync();
        Task<int> GetNewCustomersThisMonthAsync();
        Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetTopSellingProductsAsync(int top = 5);
        Task<List<(int Year, int Month, decimal Revenue, int OrderCount)>> GetMonthlyRevenuesAsync(int months = 12);
        Task<Dictionary<string, int>> GetOrdersByStatusAsync();
        Task<List<(DateTime Date, decimal Revenue, int OrderCount)>> GetDailyRevenuesAsync(int days = 7);

        // Product Statistics
        Task<int> GetActiveProductCountAsync();
        Task<int> GetOutOfStockProductCountAsync();
        Task<int> GetDiscontinuedProductCountAsync();
        Task<decimal> GetTotalInventoryValueAsync();
        Task<List<(int CategoryId, string CategoryName, int ProductCount, int TotalSold, decimal Revenue)>> GetSalesByCategoryAsync();
        Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetTopProductsAsync(int top = 10);
        Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetWorstProductsAsync(int top = 10);
        Task<List<(int ProductId, string Sku, string ProductName, string? Category, string? Brand, int Stock, string? ImageUrl)>> GetLowStockProductsAsync(int threshold = 10);
        Task<List<(int BrandId, string BrandName, int ProductCount, int TotalSold, decimal Revenue)>> GetSalesByBrandAsync();
    }
}
