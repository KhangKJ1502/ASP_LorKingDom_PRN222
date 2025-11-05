using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface ISuperCategoryService
    {
        Task<List<SuperCategoryDto>> GetAllAsync(string? keyword = null);

        Task<List<SuperCategoryDto>> GetActiveAsync();

        Task<SuperCategoryDto?> GetByIdAsync(int id);

        Task<int> CreateAsync(string name, bool isDeleted = false);

        Task<bool> UpdateAsync(int id, string name, bool isDeleted);
        Task<PagedResult<SuperCategoryDto>> GetPagedAsync(string? keyword, int page, int pageSize);
    }
}
