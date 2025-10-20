using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;   

namespace DAL.Repositories
{
    public class ProductImageRepository : IProductImageRepository
    {
        private readonly AspLorKingDomContext _ctx;

        public ProductImageRepository(AspLorKingDomContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
        {
            return await _ctx.ProductImages
                .Where(x => x.ProductId == productId)                 // ProductId
                .OrderByDescending(x => x.IsMain)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProductImage?> GetMainAsync(int productId)
        {
            return await _ctx.ProductImages
                .Where(x => x.ProductId == productId && x.IsMain)     // ProductId
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountSecondaryAsync(int productId)
        {
            return await _ctx.ProductImages
                .Where(x => x.ProductId == productId && !x.IsMain)    // ProductId
                .CountAsync();                                        // cần using Microsoft.EntityFrameworkCore
        }

        public async Task UnsetMainAsync(int productId)
        {
            var currents = await _ctx.ProductImages
                .Where(x => x.ProductId == productId && x.IsMain)     // ProductId
                .ToListAsync();

            if (currents.Count == 0) return;

            foreach (var img in currents)
                img.IsMain = false;

            await _ctx.SaveChangesAsync();
        }

        public async Task AddAsync(ProductImage entity)
        {
            await _ctx.ProductImages.AddAsync(entity);
            await _ctx.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ProductImage> entities)
        {
            await _ctx.ProductImages.AddRangeAsync(entities);
            await _ctx.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                await action();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
