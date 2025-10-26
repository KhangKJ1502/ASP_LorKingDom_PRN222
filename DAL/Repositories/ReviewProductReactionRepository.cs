using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ReviewProductReactionRepository : IReviewProductReactionRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewProductReactionRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewProductReaction>> GetByReviewIdAsync(int reviewProductId)
        {
            return await _context.ReviewProductReactions
                .Where(r => r.ReviewProductId == reviewProductId)
                .Include(r => r.Account)
                .ToListAsync();
        }

        public async Task<ReviewProductReaction?> GetByUserAndReviewAsync(int reviewProductId, int accountId)
        {
            return await _context.ReviewProductReactions
                .FirstOrDefaultAsync(r => r.ReviewProductId == reviewProductId && r.AccountId == accountId);
        }

        public async Task<int> CreateAsync(ReviewProductReaction reaction)
        {
            await _context.ReviewProductReactions.AddAsync(reaction);
            await _context.SaveChangesAsync();
            return reaction.ReactionProductId;
        }

        public async Task<bool> UpdateAsync(ReviewProductReaction reaction)
        {
            _context.ReviewProductReactions.Update(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int reactionId)
        {
            var reaction = await _context.ReviewProductReactions.FindAsync(reactionId);
            if (reaction == null) return false;

            _context.ReviewProductReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}