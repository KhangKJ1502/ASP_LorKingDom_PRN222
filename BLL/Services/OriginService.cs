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
    public class OriginService : IOriginService
    {
        private readonly IOriginRepository _repo;
        private readonly IProductRepository _productRepo;
        public OriginService(IOriginRepository repo, IProductRepository productRepo)
        {
            _repo = repo;
            _productRepo = productRepo;
        }

        public async Task<List<OriginDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<OriginDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<OriginDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(string name, bool isDeleted = false)
        {
            var dto = new OriginDto { Name = name, IsDeleted = isDeleted };
            OriginValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên xuất xứ đã tồn tại.");

            var entity = new Origin
            {
                OriginName = name.Trim(),
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);
            return entity.OriginId;
        }

        public async Task<bool> UpdateAsync(int id, string name, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new OriginDto { Id = id, Name = name, IsDeleted = isDeleted };
            OriginValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name, id))
                throw new InvalidOperationException("Tên xuất xứ đã tồn tại.");

            e.OriginName = name.Trim();
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);
            if (isDeleted)
            {
                await _productRepo.SetIsDeletedByOriginAsync(id, true);
            }
            return true;
        }

        private static OriginDto Map(Origin x) => new()
        {
            Id = x.OriginId,
            Name = x.OriginName,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
    }
}
