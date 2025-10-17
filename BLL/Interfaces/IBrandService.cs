using System.Collections.Generic;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IBrandService
    {
        Task<List<BrandDto>> GetAllAsync(string? keyword = null);
        Task<List<BrandDto>> GetActiveAsync();
        Task<BrandDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(string name, bool isDeleted = false);
        Task<bool> UpdateAsync(int id, string name, bool isDeleted);
    }
}
