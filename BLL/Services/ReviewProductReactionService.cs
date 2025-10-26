using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ReviewProductReactionService : IReviewProductReactionService
    {
        private readonly IReviewProductReactionRepository _repo;

        public ReviewProductReactionService(IReviewProductReactionRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> AddOrUpdateReactionAsync(int reviewProductId, int accountId, string reactionType)
        {
            if (reactionType != "Like" && reactionType != "Dislike")
                throw new ArgumentException("Reaction type must be 'Like' or 'Dislike'");

            var existing = await _repo.GetByUserAndReviewAsync(reviewProductId, accountId);

            if (existing == null)
            {
                var reaction = new ReviewProductReaction
                {
                    ReviewProductId = reviewProductId,
                    AccountId = accountId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                };
                return await _repo.CreateAsync(reaction);
            }
            else if (existing.ReactionType == reactionType)
            {
                await _repo.DeleteAsync(existing.ReactionProductId);
                return existing.ReactionProductId;
            }
            else
            {
                existing.ReactionType = reactionType;
                existing.CreatedAt = DateTime.Now;
                await _repo.UpdateAsync(existing);
                return existing.ReactionProductId;
            }
        }

        public async Task<bool> RemoveReactionAsync(int reviewProductId, int accountId)
        {
            var existing = await _repo.GetByUserAndReviewAsync(reviewProductId, accountId);
            if (existing == null) return false;

            return await _repo.DeleteAsync(existing.ReactionProductId);
        }
    }
}