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
                .Where(x => x.ProductId == productId)                
                .OrderByDescending(x => x.IsMain)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProductImage?> GetMainAsync(int productId)
        {
            return await _ctx.ProductImages
                .Where(x => x.ProductId == productId && x.IsMain)     
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountSecondaryAsync(int productId)
        {
            return await _ctx.ProductImages
                .Where(x => x.ProductId == productId && !x.IsMain)   
                .CountAsync();                                       
        }

        public async Task UnsetMainAsync(int productId)
        {
            var currents = await _ctx.ProductImages
                .Where(x => x.ProductId == productId && x.IsMain)     
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
        public void RemoveRange(IEnumerable<ProductImage> entities)
        {
            _ctx.ProductImages.RemoveRange(entities);
        }

        public async Task SaveChangesAsync()
        {
            await _ctx.SaveChangesAsync();
        }

        public async Task UpsertImagesAsync(
     int productId,
     string? mainImageUrl,
     IEnumerable<string> keepSecondaryUrls,
     IEnumerable<string> addSecondaryUrls,
     bool keepMainIfNull = true)
        {
            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                // 1) Main image
                if (!keepMainIfNull || !string.IsNullOrWhiteSpace(mainImageUrl))
                {
                    // clear current main
                    var mains = await _ctx.ProductImages
                        .Where(x => x.ProductId == productId && x.IsMain)
                        .ToListAsync();
                    foreach (var m in mains) m.IsMain = false;

                    if (!string.IsNullOrWhiteSpace(mainImageUrl))
                    {
                        await _ctx.ProductImages.AddAsync(new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = mainImageUrl!,
                            IsMain = true
                        });
                    }
                    await _ctx.SaveChangesAsync();
                }

                var keepSet = new HashSet<string>((keepSecondaryUrls ?? Enumerable.Empty<string>())
                                                  .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                                                  StringComparer.OrdinalIgnoreCase);

                var secondaries = await _ctx.ProductImages
                    .Where(x => x.ProductId == productId && !x.IsMain)
                    .ToListAsync();

                var toDelete = secondaries.Where(s => !keepSet.Contains(s.ImageUrl)).ToList();
                if (toDelete.Count > 0)
                {
                    _ctx.ProductImages.RemoveRange(toDelete);
                    await _ctx.SaveChangesAsync();
                }

                var existingUrls = await _ctx.ProductImages
                    .Where(x => x.ProductId == productId && !x.IsMain)
                    .Select(x => x.ImageUrl)
                    .ToListAsync();

                var canAdd = Math.Max(0, 6 - existingUrls.Count);
                var toAdd = (addSecondaryUrls ?? Enumerable.Empty<string>())
                    .Select(s => (s ?? string.Empty).Trim())
                    .Where(s => s.Length > 0 && !existingUrls.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .Take(canAdd)
                    .Select(url => new ProductImage
                    {
                        ProductId = productId,
                        ImageUrl = url,
                        IsMain = false
                    })
                    .ToList();

                if (toAdd.Count > 0)
                {
                    await _ctx.ProductImages.AddRangeAsync(toAdd);
                    await _ctx.SaveChangesAsync();
                }

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
