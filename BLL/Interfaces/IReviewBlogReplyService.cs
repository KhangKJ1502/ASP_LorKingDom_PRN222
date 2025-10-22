using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IReviewBlogReplyService
    {
        Task<List<ReviewBlogReplyDto>> GetByReviewIdAsync(int reviewBlogId);
        Task<ReviewBlogReplyDto?> GetByIdAsync(int replyId);
        Task<int> CreateAsync(ReviewBlogReplyDto dto);
        Task<bool> DeleteAsync(int replyId);
    }
}
