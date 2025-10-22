using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class ReviewBlogReactionService : IReviewBlogReactionService
    {
        private readonly IReviewBlogReactionRepository _repo;

        public ReviewBlogReactionService(IReviewBlogReactionRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> AddOrUpdateReactionAsync(int reviewBlogId, int accountId, string reactionType)
        {
            if (reactionType != "like" && reactionType != "dislike")
                throw new ArgumentException("Reaction type phải là 'like' hoặc 'dislike'");

            var existing = await _repo.GetByUserAndReviewAsync(reviewBlogId, accountId);

            if (existing == null)
            {
                // Create new reaction
                var reaction = new ReviewBlogReaction
                {
                    ReviewBlogId = reviewBlogId,
                    AccountId = accountId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.Now
                };
                return await _repo.CreateAsync(reaction);
            }
            else if (existing.ReactionType == reactionType)
            {
                // Remove reaction if same type
                await _repo.DeleteAsync(existing.ReactionBlogId);
                return existing.ReactionBlogId;
            }
            else
            {
                // Update reaction type
                existing.ReactionType = reactionType;
                existing.CreatedAt = DateTime.Now;
                await _repo.UpdateAsync(existing);
                return existing.ReactionBlogId;
            }
        }

        public async Task<bool> RemoveReactionAsync(int reviewBlogId, int accountId)
        {
            var existing = await _repo.GetByUserAndReviewAsync(reviewBlogId, accountId);
            if (existing == null) return false;

            return await _repo.DeleteAsync(existing.ReactionBlogId);
        }
    }
}
