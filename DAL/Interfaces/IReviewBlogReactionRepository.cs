using DAL.Models;

namespace DAL.Interfaces
{
    public interface IReviewBlogReactionRepository
    {
        Task<List<ReviewBlogReaction>> GetByReviewIdAsync(int reviewBlogId);
        Task<ReviewBlogReaction?> GetByUserAndReviewAsync(int reviewBlogId, int accountId);
        Task<int> CreateAsync(ReviewBlogReaction reaction);
        Task<bool> UpdateAsync(ReviewBlogReaction reaction);
        Task<bool> DeleteAsync(int reactionId);
    }
}
