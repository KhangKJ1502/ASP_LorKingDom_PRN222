using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly IMaterialRepository _repo;
        private readonly IProductRepository _productRepo;

        public MaterialService(IMaterialRepository repo, IProductRepository productRepo)
        {
            _repo = repo;
            _productRepo = productRepo;
        }

        public async Task<List<MaterialDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<MaterialDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<MaterialDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(string name, string? description, bool isDeleted = false)
        {
            var dto = new MaterialDto { Name = name, Description = description, IsDeleted = isDeleted };
            MaterialValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên chất liệu đã tồn tại.");

            var entity = new Material
            {
                MaterialName = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim(),
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);
            return entity.MaterialId;
        }

        public async Task<bool> UpdateAsync(int id, string name, string? description, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new MaterialDto { Id = id, Name = name, Description = description, IsDeleted = isDeleted };
            MaterialValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name, id))
                throw new InvalidOperationException("Tên chất liệu đã tồn tại.");

            e.MaterialName = name.Trim();
            e.Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim();
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);
            if (isDeleted)
            {
                await _productRepo.SetIsDeletedByMaterialAsync(id, true);
                // Khi bật lại Material: KHÔNG tự bật Product con (giữ quy ước như Brand)
            }
            return true;
        }

        private static MaterialDto Map(Material x) => new()
        {
            Id = x.MaterialId,
            Name = x.MaterialName,
            Description = x.Description,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
    }
}
