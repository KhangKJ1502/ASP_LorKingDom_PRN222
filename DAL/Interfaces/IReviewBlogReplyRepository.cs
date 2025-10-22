using DAL.Models;

namespace DAL.Interfaces
{
    public interface IReviewBlogReplyRepository
    {
        Task<List<ReviewBlogReply>> GetByReviewIdAsync(int reviewBlogId);
        Task<ReviewBlogReply?> GetByIdAsync(int replyBlogId);
        Task<int> CreateAsync(ReviewBlogReply reply);
        Task<bool> UpdateAsync(ReviewBlogReply reply);
        Task<bool> DeleteAsync(int replyBlogId);
    }
}
