// DAL/Repositories/ProductRepository.cs
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ProductRepository : IProductRepository
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
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = $"%{keyword.Trim()}%";
                q = q.Where(p =>
                    EF.Functions.Like(p.ProductName, k) ||
                    (p.Sku != null && EF.Functions.Like(p.Sku, k))   // ✅ null-safe + Like
                );
            }

            return await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Material)
                .Include(p => p.Age)
                .Include(p => p.Sex)
                .Include(p => p.PriceRange)
                .Include(p => p.Origin)
                .Include(p => p.ProductImages)
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
            // ✅ case-insensitive đơn giản (phụ thuộc collation; chuẩn hơn là normalized column/collation CI)
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
                    p.ProductStatus = "Discontinued";                 // ✅ rõ nghĩa khi OFF
                }
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }

        // ✅ NEW: cascade Category → Product
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
            // Nếu dùng EF Core 7+/8 có thể dùng ExecuteUpdateAsync; dưới đây là cách tương thích rộng:
            var items = await _ctx.Products
                .Where(p => p.MaterialId == materialId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued"; // rõ nghĩa khi OFF
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
        public async Task<int> SetIsDeletedByOriginAsync(int originId, bool isDeleted)
        {
            // Nếu bạn dùng EF Core 7+/8 có thể chuyển sang ExecuteUpdateAsync để tối ưu.
            var items = await _ctx.Products
                .Where(p => p.OriginId == originId)
                .ToListAsync();

            foreach (var p in items)
            {
                p.IsDeleted = isDeleted;
                if (isDeleted)
                    p.ProductStatus = "Discontinued"; // rõ nghĩa khi OFF
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }

        public async Task<(List<Product> Items, int Total)> QueryStorefrontPagedAsync(string? keyword, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 16;

            var q = _ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages) // để lấy MainImageUrl
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
                q = q.Where(p => p.ProductName.Contains(keyword) || p.Sku.Contains(keyword));
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
            // Dựa trên quan hệ: Product.Category.SuperCategoryId == superCategoryId
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
                .Include(p => p.ProductImages) // để có ảnh chính
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

    }

}
