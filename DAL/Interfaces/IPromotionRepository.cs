// DAL/Interfaces/IPromotionRepository.cs
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IPromotionRepository
    {
        Task<List<Promotion>> GetAllAsync(string? keyword);
        Task<Promotion?> GetByIdAsync(int id);
        Task<Promotion> AddAsync(Promotion promotion);
        Task<bool> UpdateAsync(Promotion promotion);
        Task<bool> DeleteAsync(int id);     // xóa mềm
        Task<bool> RestoreAsync(int id);

        Task<bool> SetStatusAsync(int id, string status); // "Active"/"Inactive"
        Task<bool> ToggleStatusAsync(int id);

        Task<List<Promotion>> GetActivePromotionsAsync();
        Task<List<Promotion>> GetActiveByProductAsync(int productId);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasOverlapAsync(int productId, DateTime start, DateTime end, int? excludeId = null);
    }
}
