using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ReviewProductRepository : IReviewProductRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewProductRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewProduct>> GetAllAsync()
        {
            return await _context.ReviewProducts
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Product)
                .Include(r => r.ReviewProductReactions)
                .Include(r => r.ReviewProductReplies)
                    .ThenInclude(rr => rr.Account)
                .Include(r => r.ReviewProductImages)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReviewProduct>> GetByProductIdAsync(int productId)
        {
            return await _context.ReviewProducts
                .Where(r => r.ProductId == productId)
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Product)
                .Include(r => r.ReviewProductReactions)
                .Include(r => r.ReviewProductReplies)
                    .ThenInclude(rr => rr.Account)
                .Include(r => r.ReviewProductImages)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReviewProduct?> GetByIdAsync(int reviewProductId)
        {
            return await _context.ReviewProducts
                .Include(r => r.Account)
                    .ThenInclude(a => a.Role)
                .Include(r => r.Product)
                .Include(r => r.ReviewProductReactions)
                    .ThenInclude(ra => ra.Account)
                .Include(r => r.ReviewProductReplies)
                    .ThenInclude(rr => rr.Account)
                .Include(r => r.ReviewProductImages)
                .FirstOrDefaultAsync(r => r.ReviewProductId == reviewProductId);
        }

        public async Task<int> CreateAsync(ReviewProduct review)
        {
            await _context.ReviewProducts.AddAsync(review);
            await _context.SaveChangesAsync();
            return review.ReviewProductId;
        }

        public async Task<bool> UpdateAsync(ReviewProduct review)
        {
            _context.ReviewProducts.Update(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int reviewProductId)
        {
            var review = await _context.ReviewProducts.FindAsync(reviewProductId);
            if (review == null) return false;

            _context.ReviewProducts.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserReviewedAsync(int productId, int accountId)
        {
            return await _context.ReviewProducts
                .AnyAsync(r => r.ProductId == productId && r.AccountId == accountId);
        }
    }
}