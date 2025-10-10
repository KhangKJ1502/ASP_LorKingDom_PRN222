using DAL.Models;

namespace DAL.Interfaces
{
    public interface ISuperCategoryRepository
    {
        Task<List<SuperCategory>> GetAllAsync(string? keyword);
        Task<List<SuperCategory>> GetActiveAsync();
        Task<List<SuperCategory>> SearchAsync(string keyword, bool includeDeleted = false);
        Task<SuperCategory?> GetByIdAsync(int id);
        Task AddAsync(SuperCategory entity);
        Task UpdateAsync(SuperCategory entity);
        Task EditIsDeletedAsync(int id, bool isDeleted);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
