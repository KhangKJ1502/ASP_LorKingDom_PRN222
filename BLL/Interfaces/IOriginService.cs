using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IOriginService
    {
        Task<List<OriginDto>> GetAllAsync(string? keyword = null);
        Task<List<OriginDto>> GetActiveAsync();
        Task<OriginDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(string name, bool isDeleted = false);
        Task<bool> UpdateAsync(int id, string name, bool isDeleted);
    }
}
