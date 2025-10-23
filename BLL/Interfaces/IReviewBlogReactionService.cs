using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IReviewBlogReactionService
    {
        Task<int> AddOrUpdateReactionAsync(int reviewBlogId, int accountId, string reactionType);
        Task<bool> RemoveReactionAsync(int reviewBlogId, int accountId);
    }
}
