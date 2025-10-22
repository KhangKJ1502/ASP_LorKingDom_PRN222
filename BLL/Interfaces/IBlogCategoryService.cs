using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IBlogCategoryService
    {
        Task<List<BlogCategoryDto>> GetAllAsync();
        Task<BlogCategoryDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(BlogCategoryDto dto);
        Task<bool> UpdateAsync(int id, BlogCategoryDto dto);
        Task<bool> SoftDeleteAsync(int id);
    }
}
