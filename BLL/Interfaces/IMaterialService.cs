using System.Collections.Generic;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IMaterialService
    {
        Task<List<MaterialDto>> GetAllAsync(string? keyword = null);
        Task<List<MaterialDto>> GetActiveAsync();
        Task<MaterialDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(string name, string? description, bool isDeleted = false);
        Task<bool> UpdateAsync(int id, string name, string? description, bool isDeleted);
    }
}
