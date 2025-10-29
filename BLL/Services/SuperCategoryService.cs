using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;
using BLL.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class SuperCategoryService : ISuperCategoryService
    {
        private readonly ISuperCategoryRepository _repo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IProductRepository _productRepo;

        public SuperCategoryService(ISuperCategoryRepository repo, ICategoryRepository categoryRepo, IProductRepository productRepo)
        {
            _repo = repo;
            _categoryRepo = categoryRepo;
            _productRepo = productRepo;
        }

        public async Task<List<SuperCategoryDto>> GetAllAsync(string? keyword = null)
        {
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

            await _repo.AddAsync(entity);
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

            e.SuperCategoryName = name.Trim();
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);

            if (isDeleted)
            {
                await _categoryRepo.SetIsDeletedBySuperCategoryAsync(id, true);
                await _productRepo.SetIsDeletedBySuperCategoryAsync(id, true);

            }

            return true;
        }

        private static SuperCategoryDto Map(SuperCategory x) => new()
        {
            Id = x.SuperCategoryId,
            Name = x.SuperCategoryName,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
        public async Task<PagedResult<SuperCategoryDto>> GetPagedAsync(string? keyword, int page, int pageSize)
        {
            var (items, total) = await _repo.QueryPagedAsync(keyword, page, pageSize);
            return new PagedResult<SuperCategoryDto>
            {
                Items = items.Select(Map).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

    }
}
