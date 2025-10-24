using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IReviewProductReplyService
    {
        Task<List<ReviewProductReplyDto>> GetByReviewIdAsync(int reviewProductId);
        Task<ReviewProductReplyDto?> GetByIdAsync(int replyId);
        Task<int> CreateAsync(ReviewProductReplyDto dto);
        Task<bool> DeleteAsync(int replyId);
    }
}