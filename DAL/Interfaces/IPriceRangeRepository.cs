using DAL.Models;

namespace DAL.Interfaces
{
    public interface IPriceRangeRepository
    {
        Task<List<PriceRange>> GetAllAsync(string? keyword);
        Task<List<PriceRange>> GetActiveAsync();
        Task<PriceRange?> GetByIdAsync(int id);
        Task AddAsync(PriceRange entity);
        Task UpdateAsync(PriceRange entity);
        Task<bool> ExistsAsync(decimal min, decimal max, int? excludeId = null);
    }
}
