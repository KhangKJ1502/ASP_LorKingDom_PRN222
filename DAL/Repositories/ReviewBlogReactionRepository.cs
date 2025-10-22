using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ReviewBlogReactionRepository : IReviewBlogReactionRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewBlogReactionRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewBlogReaction>> GetByReviewIdAsync(int reviewBlogId)
        {
            return await _context.ReviewBlogReactions
                .Where(r => r.ReviewBlogId == reviewBlogId)
                .Include(r => r.Account)
                .ToListAsync();
        }

        public async Task<ReviewBlogReaction?> GetByUserAndReviewAsync(int reviewBlogId, int accountId)
        {
            return await _context.ReviewBlogReactions
                .FirstOrDefaultAsync(r => r.ReviewBlogId == reviewBlogId && r.AccountId == accountId);
        }

        public async Task<int> CreateAsync(ReviewBlogReaction reaction)
        {
            await _context.ReviewBlogReactions.AddAsync(reaction);
            await _context.SaveChangesAsync();
            return reaction.ReactionBlogId;
        }

        public async Task<bool> UpdateAsync(ReviewBlogReaction reaction)
        {
            _context.ReviewBlogReactions.Update(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int reactionId)
        {
            var reaction = await _context.ReviewBlogReactions.FindAsync(reactionId);
            if (reaction == null) return false;

            _context.ReviewBlogReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
