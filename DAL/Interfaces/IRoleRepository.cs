using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(int roleId);
        Task<bool> ExistsAsync(int roleId);
        Task<List<Role>> GetAllAsync();
    }
}
