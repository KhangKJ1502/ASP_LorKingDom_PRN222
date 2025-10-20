using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync(string? keyword);
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product entity);
        Task UpdateAsync(Product entity);
        Task<bool> ExistsBySkuAsync(string sku);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    }
}
