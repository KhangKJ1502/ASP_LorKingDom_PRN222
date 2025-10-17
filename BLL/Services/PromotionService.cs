// BLL/Services/PromotionService.cs
using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _repo;
        private readonly PromotionValidator _validator;

        public PromotionService(IPromotionRepository repo, PromotionValidator validator)
        {
            _repo = repo;
            _validator = validator;
        }

        public async Task<List<PromotionDto>> GetAllAsync(string? keyword)
        {
            var list = await _repo.GetAllAsync(keyword);
            return list.Select(Map).ToList();
        }

        public async Task<PromotionDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<PromotionDto> CreateAsync(PromotionCreateDto dto)
        {
            await _validator.ThrowIfInvalidCreateAsync(dto);

            var entity = new Promotion
            {
                ProductId = dto.ProductId,
                Name = dto.Name,
                Description = dto.Description,
                DiscountPercent = dto.DiscountPercent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            var created = await _repo.AddAsync(entity);
            var full = await _repo.GetByIdAsync(created.PromotionId);
            return Map(full ?? created);
        }

        public async Task<bool> UpdateAsync(PromotionUpdateDto dto)
        {
            await _validator.ThrowIfInvalidUpdateAsync(dto);

            var toUpdate = new Promotion
            {
                PromotionId = dto.PromotionId,
                ProductId = dto.ProductId,
                Name = dto.Name,
                Description = dto.Description,
                DiscountPercent = dto.DiscountPercent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status
            };

            return await _repo.UpdateAsync(toUpdate);
        }

        public Task<bool> SoftDeleteAsync(int id) => _repo.DeleteAsync(id);
        public Task<bool> RestoreAsync(int id) => _repo.RestoreAsync(id);

        public Task<bool> ToggleStatusAsync(int id) => _repo.ToggleStatusAsync(id);
        public Task<bool> SetStatusAsync(int id, string status) => _repo.SetStatusAsync(id, status);

        public async Task<List<PromotionDto>> GetActiveAsync()
        {
            var list = await _repo.GetActivePromotionsAsync();
            return list.Select(Map).ToList();
        }

        public async Task<List<PromotionDto>> GetActiveByProductAsync(int productId)
        {
            var list = await _repo.GetActiveByProductAsync(productId);
            return list.Select(Map).ToList();
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
            => _repo.ExistsByNameAsync(name, excludeId);

        public Task<bool> HasOverlapAsync(int productId, DateTime start, DateTime end, int? excludeId = null)
            => _repo.HasOverlapAsync(productId, start, end, excludeId);

        private static PromotionDto Map(Promotion x) => new()
        {
            PromotionId = x.PromotionId,
            ProductId = x.ProductId,
            ProductName = x.Product?.ProductName,
            Name = x.Name,
            Description = x.Description,
            DiscountPercent = x.DiscountPercent,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            Status = x.Status,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
