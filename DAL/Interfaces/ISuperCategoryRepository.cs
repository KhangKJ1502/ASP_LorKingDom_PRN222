using DAL.Models;

namespace DAL.Interfaces
{
    public interface ISuperCategoryRepository
    {
        Task<List<SuperCategory>> GetAllAsync(string? keyword);
        Task<List<SuperCategory>> GetActiveAsync();
        Task<SuperCategory?> GetByIdAsync(int id);
        Task AddAsync(SuperCategory entity);
        Task UpdateAsync(SuperCategory entity);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<(List<SuperCategory> Items, int Total)> QueryPagedAsync(string? keyword, int page, int pageSize);
    }
}
