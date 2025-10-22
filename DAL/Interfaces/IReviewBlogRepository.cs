using DAL.Models;

namespace DAL.Interfaces
{
    public interface IReviewBlogRepository
    {
        Task<List<ReviewBlog>> GetAllAsync();
        Task<List<ReviewBlog>> GetByBlogIdAsync(int blogPostId);
        Task<ReviewBlog?> GetByIdAsync(int reviewBlogId);
        Task<int> CreateAsync(ReviewBlog review);
        Task<bool> UpdateAsync(ReviewBlog review);
        Task<bool> DeleteAsync(int reviewBlogId);
        Task<bool> HasUserCommentedAsync(int blogPostId, int accountId);
    }
}
