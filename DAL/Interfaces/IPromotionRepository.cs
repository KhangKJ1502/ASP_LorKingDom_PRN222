// DAL/Interfaces/IPromotionRepository.cs
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IPromotionRepository
    {
        Task<(IList<Promotion> Items, int Total)> SearchPagedAsync(string? keyword, int page, int pageSize);
        Task<List<Promotion>> GetAllAsync(string? keyword);
        Task<Promotion?> GetByIdAsync(int id);
        Task<Promotion> AddAsync(Promotion promotion);
        Task<bool> UpdateAsync(Promotion promotion);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
        Task<bool> SetStatusAsync(int id, string status);   
        Task<List<Promotion>> GetActivePromotionsAsync();
        Task<List<Promotion>> GetActiveByProductAsync(int productId);
        Task<List<Promotion>> GetActiveInTimeRangeAsync(); // Mới: lấy Active trong khoảng thời gian hợp lệ
        Task<bool> ExistsByNameAsync(string code, int? excludeId = null);
        Task<bool> HasOverlapAsync(int productId, DateTime start, DateTime end, int? excludeId = null);
    }
}
