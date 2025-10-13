using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
namespace BLL.Interfaces
{
    public interface IPriceRangeService
    {
        Task<List<PriceRangeDto>> GetAllAsync(string? keyword = null);
        Task<List<PriceRangeDto>> GetActiveAsync();
        Task<PriceRangeDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(decimal min, decimal max, bool isDeleted = false);
        Task<bool> UpdateAsync(int id, decimal min, decimal max, bool isDeleted);
    }
}
