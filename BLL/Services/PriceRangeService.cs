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
    public class PriceRangeService : IPriceRangeService
    {
        private readonly IPriceRangeRepository _repo;
        private readonly IProductRepository _productRepo;
        public PriceRangeService(IPriceRangeRepository repo, IProductRepository productRepo)
        {
            _repo = repo;
            _productRepo = productRepo;
        }

        public async Task<List<PriceRangeDto>> GetAllAsync(string? keyword = null)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<List<PriceRangeDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(Map).ToList();
        }

        public async Task<PriceRangeDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null ? null : Map(e);
        }

        public async Task<int> CreateAsync(decimal min, decimal max, bool isDeleted = false)
        {
            var dto = new PriceRangeDto { PriceRangeMin = min, PriceRangeMax = max, IsDeleted = isDeleted };
            PriceRangeValidator.Validate(dto);

            if (await _repo.ExistsAsync(min, max))
                throw new InvalidOperationException("Khoảng giá này đã tồn tại.");

            var entity = new PriceRange
            {
                PriceRangeMin = min,
                PriceRangeMax = max,
                IsDeleted = isDeleted
            };

            await _repo.AddAsync(entity);
            return entity.PriceRangeId;
        }

        public async Task<bool> UpdateAsync(int id, decimal min, decimal max, bool isDeleted)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return false;

            var dto = new PriceRangeDto { Id = id, PriceRangeMin = min, PriceRangeMax = max, IsDeleted = isDeleted };
            PriceRangeValidator.Validate(dto);

            if (await _repo.ExistsAsync(min, max, id))
                throw new InvalidOperationException("Khoảng giá này đã tồn tại.");

            e.PriceRangeMin = min;
            e.PriceRangeMax = max;
            e.IsDeleted = isDeleted;

            await _repo.UpdateAsync(e);
            if (isDeleted)
            {
                await _productRepo.SetIsDeletedByPriceRangeAsync(id, true);
            }
            return true;
        }

        private static PriceRangeDto Map(PriceRange x) => new()
        {
            Id = x.PriceRangeId,
            PriceRangeMin = x.PriceRangeMin,
            PriceRangeMax = x.PriceRangeMax,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt
        };
    }
}
