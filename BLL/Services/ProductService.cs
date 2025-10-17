// BLL/Services/ProductService.cs
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo) => _repo = repo;

        public async Task<List<SelectItemDto>> GetSelectListAsync(string? keyword = null, int limit = 200)
        {
            var rows = await _repo.GetBasicListAsync(keyword, limit);
            return rows.Select(r => new SelectItemDto(r.Id.ToString(), r.Name)).ToList();
        }

        public Task<bool> ExistsAsync(int productId) => _repo.ExistsAsync(productId);
    }
}
