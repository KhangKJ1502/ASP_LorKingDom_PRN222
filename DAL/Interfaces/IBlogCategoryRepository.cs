using DAL.Models;

namespace DAL.Interfaces
{
    public interface IBlogCategoryRepository
    {
        Task<List<BlogCategory>> GetAllAsync();
        Task<BlogCategory?> GetByIdAsync(int id);
        Task<int> CreateAsync(BlogCategory category);
        Task<bool> UpdateAsync(BlogCategory category);
    }
}
