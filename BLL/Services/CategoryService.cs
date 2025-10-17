using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly ISuperCategoryRepository _superRepo;

        public CategoryService(ICategoryRepository repo, ISuperCategoryRepository superRepo)
        {
            _repo = repo;
            _superRepo = superRepo;
        }

        public async Task<List<CategoryDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<CategoryDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(int superCategoryId, string name, bool isDeleted = false)
        {
            var dto = new CategoryDto { Name = name, SuperCategoryId = superCategoryId, IsDeleted = isDeleted };
            CategoryValidator.Validate(dto);

           

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên danh mục đã tồn tại.");

            var entity = new Category
            {
                SuperCategoryId = superCategoryId,
                CategoryName = name.Trim(),
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);
            return entity.CategoryId;
        }

        public async Task<bool> UpdateAsync(int id, int superCategoryId, string name, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new CategoryDto { Id = id, SuperCategoryId = superCategoryId, Name = name, IsDeleted = isDeleted };
            CategoryValidator.Validate(dto);


            if (await _repo.ExistsByNameAsync(name, id))
                throw new InvalidOperationException("Tên danh mục đã tồn tại.");

            e.SuperCategoryId = superCategoryId;
            e.CategoryName = name.Trim();
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);
            return true;
        }

        private static CategoryDto Map(Category x) => new()
        {
            Id = x.CategoryId,
            SuperCategoryId = x.SuperCategoryId,
            Name = x.CategoryName,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            SuperCategoryName = x.SuperCategory?.SuperCategoryName
        };
    }
}
