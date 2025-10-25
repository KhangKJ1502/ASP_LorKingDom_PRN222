using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ReviewProductReplyRepository : IReviewProductReplyRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewProductReplyRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewProductReply>> GetByReviewIdAsync(int reviewProductId)
        {
            return await _context.ReviewProductReplies
                .Where(r => r.ReviewProductId == reviewProductId)
                .Include(r => r.Account)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReviewProductReply?> GetByIdAsync(int replyProductId)
        {
            return await _context.ReviewProductReplies
                .Include(r => r.Account)
                .FirstOrDefaultAsync(r => r.ReplyProductId == replyProductId);
        }

        public async Task<int> CreateAsync(ReviewProductReply reply)
        {
            await _context.ReviewProductReplies.AddAsync(reply);
            await _context.SaveChangesAsync();
            return reply.ReplyProductId;
        }

        public async Task<bool> UpdateAsync(ReviewProductReply reply)
        {
            _context.ReviewProductReplies.Update(reply);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int replyProductId)
        {
            var reply = await _context.ReviewProductReplies.FindAsync(replyProductId);
            if (reply == null) return false;

            _context.ReviewProductReplies.Remove(reply);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}