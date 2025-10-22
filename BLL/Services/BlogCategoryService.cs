using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class BlogCategoryService : IBlogCategoryService
    {
        private readonly IBlogCategoryRepository _repo;

        public BlogCategoryService(IBlogCategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<BlogCategoryDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Where(c => !c.IsDeleted).Select(Map).ToList();
        }

        public async Task<BlogCategoryDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null || entity.IsDeleted ? null : Map(entity);
        }

        public async Task<int> CreateAsync(BlogCategoryDto dto)
        {
            var entity = new BlogCategory
            {
                BlogCategoryName = dto.BlogCategoryName,
                Description = dto.Description,
                IsDeleted = false,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = null
            };

            await _repo.CreateAsync(entity);
            return entity.BlogCategoryId;
        }

        public async Task<bool> UpdateAsync(int id, BlogCategoryDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.BlogCategoryName = dto.BlogCategoryName;
            entity.Description = dto.Description;
            entity.IsDeleted = dto.IsDeleted;
            entity.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.Now;
            await _repo.UpdateAsync(entity);
            return true;
        }

        private static BlogCategoryDto Map(BlogCategory x) => new()
        {
            BlogCategoryId = x.BlogCategoryId,
            BlogCategoryName = x.BlogCategoryName,
            Description = x.Description,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
