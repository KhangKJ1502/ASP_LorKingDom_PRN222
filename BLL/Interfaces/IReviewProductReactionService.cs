using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IReviewProductReactionService
    {
        Task<int> AddOrUpdateReactionAsync(int reviewProductId, int accountId, string reactionType);
        Task<bool> RemoveReactionAsync(int reviewProductId, int accountId);
    }
}