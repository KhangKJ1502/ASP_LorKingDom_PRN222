using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Interfaces
{
    public interface IMaterialRepository
    {
        Task<List<Material>> GetAllAsync(string? keyword);
        Task<List<Material>> GetActiveAsync();
        Task<Material?> GetByIdAsync(int id);
        Task AddAsync(Material entity);
        Task UpdateAsync(Material entity);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
