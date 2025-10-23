using DAL.Models;

namespace DAL.Interfaces
{
    public interface IBlogRepository
    {
        Task<List<BlogPost>> GetAllAsync();
        Task<BlogPost?> GetByIdAsync(int id);
        Task<int> CreateAsync(BlogPost blog);
        Task<bool> UpdateAsync(BlogPost blog);
        //Task<bool> DeleteAsync(int id);
        Task<int> GetFeaturedBlogCountAsync();
    }
}
