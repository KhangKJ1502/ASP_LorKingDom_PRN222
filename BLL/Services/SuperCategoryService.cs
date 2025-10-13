using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using BLL.Validators;
namespace BLL.Services
{
    public class SuperCategoryService : ISuperCategoryService
    {
        private readonly ISuperCategoryRepository _repo;

        public SuperCategoryService(ISuperCategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SuperCategoryDto>> GetAllAsync(string? keyword = null)
        {
            // Nếu muốn chỉ search khi có keyword, có thể dùng _repo.SearchAsync(...)
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<SuperCategoryDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<SuperCategoryDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(string name, bool isDeleted = false)
        {
            var dto = new SuperCategoryDto { Name = name, IsDeleted = isDeleted };

            SuperCategoryValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên đã tồn tại.");

            var entity = new SuperCategory
            {
                SuperCategoryName = name.Trim(),
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);      // repo tự SaveChangesAsync bên trong
            return entity.SuperCategoryId;
        }

        public async Task<bool> UpdateAsync(int id, string name, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new SuperCategoryDto { Id = id, Name = name, IsDeleted = isDeleted };
            SuperCategoryValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name, id))
                throw new InvalidOperationException("Tên đã tồn tại.");

            e.SuperCategoryName = name;
            e.IsDeleted = isDeleted;

            // Cần có phương thức UpdateAsync ở Repository (commit bên trong)
            await _repo.UpdateAsync(e);
            return true;
        }

     

        private static SuperCategoryDto Map(SuperCategory x) => new()
        {
            Id = x.SuperCategoryId,
            Name = x.SuperCategoryName,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
    }
}
