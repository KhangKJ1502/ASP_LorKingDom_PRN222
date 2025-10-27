using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync(string? keyword = null);
        Task<List<CategoryDto>> GetActiveAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(int superCategoryId, string name, bool isDeleted = false);
        Task<bool> UpdateAsync(int id, int superCategoryId, string name, bool isDeleted);
        Task<PagedResult<CategoryDto>> GetPagedAsync(string? keyword, int page, int pageSize);
    }
}
