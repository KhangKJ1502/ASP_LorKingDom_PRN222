using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IReviewProductReactionService
    {
        Task<bool> ReactAsync(int reviewId, int accountId, string reactionType); // "Like" or "Dislike"
        Task<bool> RemoveReactionAsync(int reviewId, int accountId);
        Task<string?> GetUserReactionAsync(int reviewId, int accountId);
    }
}