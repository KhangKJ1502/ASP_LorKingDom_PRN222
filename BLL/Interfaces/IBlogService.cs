using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IBlogService
    {
        Task<List<BlogPostDto>> GetAllAsync(string? keyword = null);
        Task<BlogPostDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(BlogPostDto dto, int[] categoryIds);
        Task<bool> UpdateAsync(int id, BlogPostDto dto, int[] categoryIds);
        //Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteAsync(int id);
        Task<int> GetFeaturedBlogCountAsync();
        Task<bool> CanAddFeaturedBlogAsync(int? excludeBlogId = null);
    }
}
