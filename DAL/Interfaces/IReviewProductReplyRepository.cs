using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IReviewProductReplyRepository
    {
        Task<List<ReviewProductReply>> GetByReviewIdAsync(int reviewProductId);
        Task<ReviewProductReply?> GetByIdAsync(int replyProductId);
        Task<int> CreateAsync(ReviewProductReply reply);
        Task<bool> UpdateAsync(ReviewProductReply reply);
        Task<bool> DeleteAsync(int replyProductId);
    }
}