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
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _repo;
        private readonly IProductRepository _productRepo;

        public BrandService(IBrandRepository repo, IProductRepository productRepo)
        {
            _repo = repo;
            _productRepo = productRepo;
        }

        public async Task<List<BrandDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<BrandDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<BrandDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(string name, bool isDeleted = false)
        {
            var dto = new BrandDto { Name = name, IsDeleted = isDeleted };
            BrandValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name))
                throw new InvalidOperationException("Tên thương hiệu đã tồn tại.");

            var entity = new Brand
            {
                BrandName = name.Trim(),
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);
            return entity.BrandId;
        }

        public async Task<bool> UpdateAsync(int id, string name, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new BrandDto { Id = id, Name = name, IsDeleted = isDeleted };
            BrandValidator.Validate(dto);

            if (await _repo.ExistsByNameAsync(name, id))
                throw new InvalidOperationException("Tên thương hiệu đã tồn tại.");

            e.BrandName = name.Trim();
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);
            if (isDeleted)
            {
                await _productRepo.SetIsDeletedByBrandAsync(id, true);
            }

            return true;
        }

        private static BrandDto Map(Brand x) => new()
        {
            Id = x.BrandId,
            Name = x.BrandName,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
    }
}
