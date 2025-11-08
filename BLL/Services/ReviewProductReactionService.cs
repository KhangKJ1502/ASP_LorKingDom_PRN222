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

        public async Task<bool> ReactAsync(int reviewId, int accountId, string reactionType)
        {
            if (!new[] { "Like", "Dislike" }.Contains(reactionType))
                throw new ArgumentException("Reaction must be Like or Dislike");

            var existing = await _repo.GetByUserAndReviewAsync(reviewId, accountId);

            if (existing == null)
            {
                var reaction = new ReviewProductReaction
                {
                    ReviewProductId = reviewId,
                    AccountId = accountId,
                    ReactionType = reactionType,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };
                return await _repo.CreateAsync(reaction) > 0;
            }
            else
            {
                if (existing.ReactionType == reactionType)
                {
                    // Cùng loại → xóa mềm (IsDeleted = true)
                    existing.IsDeleted = true;
                    return await _repo.UpdateAsync(existing);
                }
                else
                {
                    // Khác loại → chuyển Like ↔ Dislike
                    existing.ReactionType = reactionType;
                    // Không có UpdatedAt → bỏ qua
                    return await _repo.UpdateAsync(existing);
                }
            }
        }

        public async Task<bool> RemoveReactionAsync(int reviewId, int accountId)
        {
            var reaction = await _repo.GetByUserAndReviewAsync(reviewId, accountId);
            if (reaction == null || reaction.IsDeleted) return false;

            reaction.IsDeleted = true;
            return await _repo.UpdateAsync(reaction);
        }

        public async Task<string?> GetUserReactionAsync(int reviewId, int accountId)
        {
            var reaction = await _repo.GetByUserAndReviewAsync(reviewId, accountId);
            return reaction != null && !reaction.IsDeleted ? reaction.ReactionType : null;
        }
    }
}