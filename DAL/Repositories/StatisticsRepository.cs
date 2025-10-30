using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly AspLorKingDomContext _ctx;

        public StatisticsRepository(AspLorKingDomContext ctx)
        {
            _ctx = ctx;
        }

        // ===== DASHBOARD STATISTICS =====

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<int> GetTotalOrdersAsync()
        {
            return await _ctx.Orders
                .Where(o => !o.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetTotalCustomersAsync()
        {
            var customerRole = await _ctx.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Customer");

            if (customerRole == null) return 0;

            return await _ctx.Accounts
                .Where(a => a.RoleId == customerRole.RoleId && !a.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetTotalProductsAsync()
        {
            return await _ctx.Products
                .Where(p => !p.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetLowStockProductCountAsync(int threshold = 10)
        {
            return await _ctx.Products
                .Where(p => !p.IsDeleted && p.Quantity <= threshold && p.Quantity > 0)
                .CountAsync();
        }

        public async Task<int> GetPendingOrderCountAsync()
        {
            var pendingStatus = await _ctx.StatusOrders
                .FirstOrDefaultAsync(s => s.StatusName == "Pending");

            if (pendingStatus == null) return 0;

            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.StatusId == pendingStatus.StatusId)
                .CountAsync();
        }

        public async Task<decimal> GetRevenueTodayAsync()
        {
            var today = DateTime.Today;
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null &&
                           o.PaymentCompletedAt.Value.Date == today)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<decimal> GetRevenueThisMonthAsync()
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null &&
                           o.PaymentCompletedAt.Value >= firstDay)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<decimal> GetRevenueThisYearAsync()
        {
            var year = DateTime.Now.Year;
            var firstDay = new DateTime(year, 1, 1);
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null &&
                           o.PaymentCompletedAt.Value >= firstDay)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<int> GetOrdersTodayAsync()
        {
            var today = DateTime.Today;
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.OrderDate.Date == today)
                .CountAsync();
        }

        public async Task<int> GetOrdersThisMonthAsync()
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            return await _ctx.Orders
                .Where(o => !o.IsDeleted && o.OrderDate >= firstDay)
                .CountAsync();
        }

        public async Task<int> GetNewCustomersThisMonthAsync()
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);

            var customerRole = await _ctx.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Customer");

            if (customerRole == null) return 0;

            return await _ctx.Accounts
                .Where(a => a.RoleId == customerRole.RoleId && !a.IsDeleted &&
                           a.CreatedAt >= firstDay)
                .CountAsync();
        }

        public async Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetTopSellingProductsAsync(int top = 5)
        {
            var result = await _ctx.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(od => !od.Order.IsDeleted && od.Order.PaymentCompletedAt != null)
                .GroupBy(od => new
                {
                    od.ProductId,
                    od.Product.ProductName,
                    MainImage = od.Product.ProductImages.FirstOrDefault(pi => pi.IsMain).ImageUrl
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.MainImage,
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(top)
                .ToListAsync();

            return result.Select(r => (r.ProductId, r.ProductName, (string?)r.MainImage, r.TotalSold, r.Revenue)).ToList();
        }

        public async Task<List<(int Year, int Month, decimal Revenue, int OrderCount)>> GetMonthlyRevenuesAsync(int months = 12)
        {
            var startDate = DateTime.Now.AddMonths(-months).Date;

            var result = await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null &&
                           o.PaymentCompletedAt.Value >= startDate)
                .GroupBy(o => new
                {
                    Year = o.PaymentCompletedAt!.Value.Year,
                    Month = o.PaymentCompletedAt!.Value.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            return result.Select(r => (r.Year, r.Month, r.Revenue, r.OrderCount)).ToList();
        }

        public async Task<Dictionary<string, int>> GetOrdersByStatusAsync()
        {
            var result = await _ctx.Orders
                .Include(o => o.Status)
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.Status.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return result;
        }

        public async Task<List<(DateTime Date, decimal Revenue, int OrderCount)>> GetDailyRevenuesAsync(int days = 7)
        {
            var startDate = DateTime.Today.AddDays(-days);

            var result = await _ctx.Orders
                .Where(o => !o.IsDeleted && o.PaymentCompletedAt != null &&
                           o.PaymentCompletedAt.Value.Date >= startDate)
                .GroupBy(o => o.PaymentCompletedAt!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return result.Select(r => (r.Date, r.Revenue, r.OrderCount)).ToList();
        }

        // ===== PRODUCT STATISTICS =====

        public async Task<int> GetActiveProductCountAsync()
        {
            return await _ctx.Products
                .Where(p => !p.IsDeleted && p.ProductStatus == "Available")
                .CountAsync();
        }

        public async Task<int> GetOutOfStockProductCountAsync()
        {
            return await _ctx.Products
                .Where(p => !p.IsDeleted && p.Quantity == 0)
                .CountAsync();
        }

        public async Task<int> GetDiscontinuedProductCountAsync()
        {
            return await _ctx.Products
                .Where(p => p.ProductStatus == "Discontinued")
                .CountAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _ctx.Products
                .Where(p => !p.IsDeleted && p.Quantity > 0)
                .SumAsync(p => p.Price * p.Quantity);
        }

        public async Task<List<(int CategoryId, string CategoryName, int ProductCount, int TotalSold, decimal Revenue)>> GetSalesByCategoryAsync()
        {
            var result = await _ctx.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => !od.Order.IsDeleted && od.Order.PaymentCompletedAt != null && od.Product.CategoryId != null)
                .GroupBy(od => new
                {
                    CategoryId = od.Product.CategoryId!.Value,
                    CategoryName = od.Product.Category!.CategoryName
                })
                .Select(g => new
                {
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    ProductCount = g.Select(od => od.ProductId).Distinct().Count(),
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return result.Select(r => (r.CategoryId, r.CategoryName, r.ProductCount, r.TotalSold, r.Revenue)).ToList();
        }

        public async Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetTopProductsAsync(int top = 10)
        {
            return await GetTopSellingProductsAsync(top);
        }

        public async Task<List<(int ProductId, string ProductName, string? ImageUrl, int TotalSold, decimal Revenue)>> GetWorstProductsAsync(int top = 10)
        {
            var result = await _ctx.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(od => !od.Order.IsDeleted && od.Order.PaymentCompletedAt != null)
                .GroupBy(od => new
                {
                    od.ProductId,
                    od.Product.ProductName,
                    MainImage = od.Product.ProductImages.FirstOrDefault(pi => pi.IsMain).ImageUrl
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.MainImage,
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderBy(x => x.TotalSold) // Ascending for worst
                .Take(top)
                .ToListAsync();

            return result.Select(r => (r.ProductId, r.ProductName, (string?)r.MainImage, r.TotalSold, r.Revenue)).ToList();
        }

        public async Task<List<(int ProductId, string Sku, string ProductName, string? Category, string? Brand, int Stock, string? ImageUrl)>> GetLowStockProductsAsync(int threshold = 10)
        {
            var result = await _ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted && p.Quantity <= threshold && p.Quantity > 0)
                .OrderBy(p => p.Quantity)
                .Select(p => new
                {
                    p.ProductId,
                    p.Sku,
                    p.ProductName,
                    Category = p.Category != null ? p.Category.CategoryName : null,
                    Brand = p.Brand != null ? p.Brand.BrandName : null,
                    p.Quantity,
                    MainImage = p.ProductImages.FirstOrDefault(pi => pi.IsMain) != null 
                        ? p.ProductImages.FirstOrDefault(pi => pi.IsMain)!.ImageUrl 
                        : null
                })
                .ToListAsync();

            return result.Select(r => (r.ProductId, r.Sku, r.ProductName, r.Category, r.Brand, r.Quantity, r.MainImage)).ToList();
        }

        public async Task<List<(int BrandId, string BrandName, int ProductCount, int TotalSold, decimal Revenue)>> GetSalesByBrandAsync()
        {
            var result = await _ctx.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                    .ThenInclude(p => p.Brand)
                .Where(od => !od.Order.IsDeleted && od.Order.PaymentCompletedAt != null && od.Product.BrandId != null)
                .GroupBy(od => new
                {
                    BrandId = od.Product.BrandId!.Value,
                    BrandName = od.Product.Brand!.BrandName
                })
                .Select(g => new
                {
                    g.Key.BrandId,
                    g.Key.BrandName,
                    ProductCount = g.Select(od => od.ProductId).Distinct().Count(),
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return result.Select(r => (r.BrandId, r.BrandName, r.ProductCount, r.TotalSold, r.Revenue)).ToList();
        }
    }
}
