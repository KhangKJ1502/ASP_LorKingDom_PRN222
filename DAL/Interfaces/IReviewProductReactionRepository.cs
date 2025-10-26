using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IReviewProductReactionRepository
    {
        Task<List<ReviewProductReaction>> GetByReviewIdAsync(int reviewProductId);
        Task<ReviewProductReaction?> GetByUserAndReviewAsync(int reviewProductId, int accountId);
        Task<int> CreateAsync(ReviewProductReaction reaction);
        Task<bool> UpdateAsync(ReviewProductReaction reaction);
        Task<bool> DeleteAsync(int reactionId);
    }
}