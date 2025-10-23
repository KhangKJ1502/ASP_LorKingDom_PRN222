using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ReviewBlogReplyRepository : IReviewBlogReplyRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewBlogReplyRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewBlogReply>> GetByReviewIdAsync(int reviewBlogId)
        {
            return await _context.ReviewBlogReplies
                .Where(r => r.ReviewBlogId == reviewBlogId)
                .Include(r => r.Account)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReviewBlogReply?> GetByIdAsync(int replyBlogId)
        {
            return await _context.ReviewBlogReplies
                .Include(r => r.Account)
                .FirstOrDefaultAsync(r => r.ReplyBlogId == replyBlogId);
        }

        public async Task<int> CreateAsync(ReviewBlogReply reply)
        {
            await _context.ReviewBlogReplies.AddAsync(reply);
            await _context.SaveChangesAsync();
            return reply.ReplyBlogId;
        }

        public async Task<bool> UpdateAsync(ReviewBlogReply reply)
        {
            _context.ReviewBlogReplies.Update(reply);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int replyBlogId)
        {
            var reply = await _context.ReviewBlogReplies.FindAsync(replyBlogId);
            if (reply == null) return false;

            _context.ReviewBlogReplies.Remove(reply);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
