using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ReviewBlogRepository : IReviewBlogRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewBlogRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewBlog>> GetAllAsync()
        {
            return await _context.ReviewBlogs
                .Include(r => r.Account)
                .Include(r => r.BlogPost)
                .Include(r => r.ReviewBlogReactions)
                .Include(r => r.ReviewBlogReplies)
                    .ThenInclude(rr => rr.Account)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ReviewBlog>> GetByBlogIdAsync(int blogPostId)
        {
            return await _context.ReviewBlogs
                .Where(r => r.BlogPostId == blogPostId)
                .Include(r => r.Account)
                .Include(r => r.ReviewBlogReactions)
                .Include(r => r.ReviewBlogReplies)
                    .ThenInclude(rr => rr.Account)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReviewBlog?> GetByIdAsync(int reviewBlogId)
        {
            return await _context.ReviewBlogs
                .Include(r => r.Account)
                .Include(r => r.ReviewBlogReactions)
                    .ThenInclude(ra => ra.Account)
                .Include(r => r.ReviewBlogReplies)
                    .ThenInclude(rr => rr.Account)
                .FirstOrDefaultAsync(r => r.ReviewBlogId == reviewBlogId);
        }

        public async Task<int> CreateAsync(ReviewBlog review)
        {
            await _context.ReviewBlogs.AddAsync(review);
            await _context.SaveChangesAsync();
            return review.ReviewBlogId;
        }

        public async Task<bool> UpdateAsync(ReviewBlog review)
        {
            _context.ReviewBlogs.Update(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int reviewBlogId)
        {
            var review = await _context.ReviewBlogs.FindAsync(reviewBlogId);
            if (review == null) return false;

            _context.ReviewBlogs.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserCommentedAsync(int blogPostId, int accountId)
        {
            return await _context.ReviewBlogs
                .AnyAsync(r => r.BlogPostId == blogPostId && r.AccountId == accountId);
        }
    }
}
