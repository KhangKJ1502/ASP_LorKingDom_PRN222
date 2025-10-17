// BLL/Interfaces/IPromotionService.cs
using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<List<PromotionDto>> GetAllAsync(string? keyword);
        Task<PromotionDto?> GetByIdAsync(int id);

        Task<PromotionDto> CreateAsync(PromotionCreateDto dto);
        Task<bool> UpdateAsync(PromotionUpdateDto dto);

        Task<bool> SoftDeleteAsync(int id);
        Task<bool> RestoreAsync(int id);

        Task<bool> ToggleStatusAsync(int id);
        Task<bool> SetStatusAsync(int id, string status);

        Task<List<PromotionDto>> GetActiveAsync();
        Task<List<PromotionDto>> GetActiveByProductAsync(int productId);

        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasOverlapAsync(int productId, DateTime start, DateTime end, int? excludeId = null);
    }
}
