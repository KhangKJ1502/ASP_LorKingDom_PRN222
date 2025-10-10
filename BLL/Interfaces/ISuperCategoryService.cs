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
        // View all + Search (truyền keyword; null => lấy tất cả)
        Task<List<SuperCategoryDto>> GetAllAsync(string? keyword = null);

        // Lấy danh sách chưa bị xóa mềm
        Task<List<SuperCategoryDto>> GetActiveAsync();

        // Lấy theo Id
        Task<SuperCategoryDto?> GetByIdAsync(int id);

        // Thêm
        Task<int> CreateAsync(string name, bool isDeleted = false);

        // Sửa tên + trạng thái (edit)
        Task<bool> UpdateAsync(int id, string name, bool isDeleted);

        // Xóa mềm (set IsDeleted=true)
        Task<bool> SoftDeleteAsync(int id);
    }
}
