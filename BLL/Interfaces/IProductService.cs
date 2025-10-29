// BLL/Interfaces/IProductService.cs
using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync(string? keyword = null);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductDto dto);     // dto.Id phải = 0
        Task<bool> UpdateAsync(ProductDto dto);    // dto.Id > 0
        Task<PagedResult<ProductDto>> GetStorefrontPagedAsync(string? keyword, int page, int pageSize);
        Task<PagedResult<ProductDto>> GetAdminPagedAsync(string? keyword, int page, int pageSize);

    }
}
