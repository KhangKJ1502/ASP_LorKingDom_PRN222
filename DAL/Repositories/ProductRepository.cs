using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public partial class ProductRepository : IProductRepository
    {
        private readonly AspLorKingDomContext _ctx;

        public ProductRepository(AspLorKingDomContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<List<Product>> GetAllAsync(string? keyword)
        {
            var q = _ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Promotion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = $"%{keyword.Trim()}%";
                q = q.Where(p =>
                    EF.Functions.Like(p.ProductName, k) ||
                    (p.Sku != null && EF.Functions.Like(p.Sku, k))
                );
            }

            return await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Material)
                .Include(p => p.Age)
                .Include(p => p.Sex)
                .Include(p => p.Origin)
                .Include(p => p.PriceRange)
                .Include(p => p.ProductImages)
                .Include(p => p.Promotion)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }


        public async Task AddAsync(Product entity)
        {
            await _ctx.Products.AddAsync(entity);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product entity)
        {
            _ctx.Products.Update(entity);
            await _ctx.SaveChangesAsync();
        }

        public async Task<bool> ExistsBySkuAsync(string sku)
        {
            return await _ctx.Products.AnyAsync(p => p.Sku == sku);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var up = name.ToUpper();
            var query = _ctx.Products.Where(p => p.ProductName.ToUpper() == up);
            if (excludeId.HasValue)
                query = query.Where(p => p.ProductId != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<int> SetIsDeletedByBrandAsync(int brandId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.BrandId == brandId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                {
                    p.ProductStatus = "Discontinued";
                }
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<int> SetIsDeletedByCategoryAsync(int categoryId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued";
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<int> SetIsDeletedByMaterialAsync(int materialId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.MaterialId == materialId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued";
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<int> SetIsDeletedByOriginAsync(int originId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.OriginId == originId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued";
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<int> SetIsDeletedByPriceRangeAsync(int priceRangeId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.PriceRangeId == priceRangeId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued";
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }

        public async Task<(List<Product> Items, int Total)> QueryStorefrontPagedAsync(
      string? keyword, int page, int pageSize, int? priceRangeId = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var q = _ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Promotion)
                .Where(p =>
                    p.IsDeleted == false &&
                    p.Quantity > 0 &&
                    p.ProductStatus == "Available" &&
                    p.CategoryId != null &&
                    p.MaterialId != null &&
                    p.AgeId != null &&
                    p.SexId != null &&
                    p.PriceRangeId != null &&
                    p.BrandId != null &&
                    p.OriginId != null
                );

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(p => p.ProductName.Contains(k) || (p.Sku != null && p.Sku.Contains(k)));
            }

            if (priceRangeId.HasValue)
            {
                q = q.Where(p => p.PriceRangeId == priceRangeId.Value);
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> SetIsDeletedBySuperCategoryAsync(int superCategoryId, bool isDeleted)
        {
            var items = await _ctx.Products
                .Where(p => p.Category != null && p.Category.SuperCategoryId == superCategoryId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued";
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<(List<Product> Items, int Total)> QueryAdminPagedAsync(string? keyword, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var q = _ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Promotion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(p =>
                    p.ProductName.Contains(k) ||
                    (p.Sku != null && p.Sku.Contains(k)) ||
                    (p.Brand != null && p.Brand.BrandName.Contains(k)) ||
                    (p.Category != null && p.Category.CategoryName.Contains(k))
                );
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<List<int>> GetProductIdsByPromotionAsync(int promotionId)
        {
            return _ctx.Products
                .Where(p => p.PromotionId == promotionId)
                .Select(p => p.ProductId)
                .ToListAsync();
        }

        public async Task<int> SetPromotionForProductsAsync(int? promotionId, int[] productIds)
        {
            if (productIds == null || productIds.Length == 0) return 0;

            var q = _ctx.Products.Where(p => productIds.Contains(p.ProductId));

            var affected = await q.ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PromotionId, p => promotionId)
                .SetProperty(p => p.UpdatedAt, p => DateTime.Now));

            return affected;
        }
    }

}

