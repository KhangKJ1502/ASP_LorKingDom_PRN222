using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AspLorKingDomContext _ctx;
        public WishlistRepository(AspLorKingDomContext ctx) { _ctx = ctx; }

        public async Task<bool> ExistsAsync(int accountId, int productId) =>
            await _ctx.Wishlists.AnyAsync(w => w.AccountId == accountId && w.ProductId == productId);

        public async Task AddAsync(int accountId, int productId)
        {
            await _ctx.Wishlists.AddAsync(new Wishlist
            {
                AccountId = accountId,
                ProductId = productId
            });
            await _ctx.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(int accountId, int productId)
        {
            var rows = await _ctx.Wishlists
                .Where(w => w.AccountId == accountId && w.ProductId == productId)
                .ExecuteDeleteAsync();
            return rows;
        }

        public async Task<int> DeleteAllAsync(int accountId) =>
            await _ctx.Wishlists.Where(w => w.AccountId == accountId).ExecuteDeleteAsync();

        public async Task<List<Wishlist>> GetWithProductsAsync(int accountId, string? keyword)
        {
            IQueryable<Wishlist> q = _ctx.Wishlists
                .AsNoTracking()
                .Where(w => w.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = $"%{keyword.Trim()}%";
                q = q.Where(w =>
                    EF.Functions.Like(w.Product.ProductName, k) ||
                    (w.Product.Sku != null && EF.Functions.Like(w.Product.Sku, k)));
            }

            q = q
                .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages);

            return await q
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }


        public async Task<List<int>> GetProductIdsAsync(int accountId) =>
            await _ctx.Wishlists.AsNoTracking()
                .Where(w => w.AccountId == accountId)
                .Select(w => w.ProductId)
                .ToListAsync();

        public async Task<int> CountAsync(int accountId) =>
            await _ctx.Wishlists.AsNoTracking()
                .CountAsync(w => w.AccountId == accountId);
    }
}
