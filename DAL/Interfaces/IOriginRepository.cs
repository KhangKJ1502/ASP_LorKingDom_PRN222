using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IOriginRepository
    {
        Task<List<Origin>> GetAllAsync(string? keyword);
        Task<List<Origin>> GetActiveAsync();
        Task<Origin?> GetByIdAsync(int id);
        Task AddAsync(Origin entity);
        Task UpdateAsync(Origin entity);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
