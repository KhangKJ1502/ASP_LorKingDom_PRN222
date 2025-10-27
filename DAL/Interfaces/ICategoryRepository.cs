using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(string? keyword);
        Task<List<Category>> GetActiveAsync();
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category entity);
        Task UpdateAsync(Category entity);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<int> SetIsDeletedBySuperCategoryAsync(int superCategoryId, bool isDeleted);
        Task<(List<Category> Items, int Total)> QueryPagedAsync(string? keyword, int page, int pageSize);
    }
}
