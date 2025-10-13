using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<Brand>> GetAllAsync(string? keyword);
        Task<List<Brand>> GetActiveAsync();
        Task<Brand?> GetByIdAsync(int id);
        Task AddAsync(Brand entity);
        Task UpdateAsync(Brand entity);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
