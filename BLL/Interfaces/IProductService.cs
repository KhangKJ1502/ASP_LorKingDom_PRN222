using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.IServices
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync(string? keyword = null);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductDto dto);     // dto.Id phải = 0
        Task<bool> UpdateAsync(ProductDto dto);    // dto.Id > 0
    }
}
