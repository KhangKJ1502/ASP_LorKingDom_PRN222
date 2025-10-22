using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IReviewBlogService
    {
        Task<List<ReviewBlogDto>> GetByBlogIdAsync(int blogPostId, int? currentUserId = null);
        Task<List<ReviewBlogDto>> GetAllReviewsAsync();
        Task<ReviewBlogDto?> GetByIdAsync(int reviewBlogId, int? currentUserId = null);
        Task<int> CreateAsync(ReviewBlogDto dto);
        Task<bool> UpdateAsync(int id, ReviewBlogDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> CanCommentAsync(int blogPostId, int accountId);
    }
}
