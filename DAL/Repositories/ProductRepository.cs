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
                q = q.Where(p => p.ProductName.Contains(keyword) || p.Sku.Contains(keyword));

            return await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
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
            var query = _ctx.Products.Where(p => p.ProductName == name);
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
                    // Bạn có thể đồng thời cho về trạng thái Discontinued để rõ nghĩa
                    p.ProductStatus = "Discontinued";
                }
            }

            await _ctx.SaveChangesAsync();
            return items.Count;
        }
    }
}
