// BLL/Interfaces/IProductService.cs
using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IProductService
    {
        Task<List<SelectItemDto>> GetSelectListAsync(string? keyword = null, int limit = 200);
        Task<bool> ExistsAsync(int productId);
    }
}
