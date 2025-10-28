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
        Task<int> SetIsDeletedByBrandAsync(int brandId, bool isDeleted);
        Task<int> SetIsDeletedByCategoryAsync(int categoryId, bool isDeleted);
        Task<int> SetIsDeletedByMaterialAsync(int materialId, bool isDeleted);
        Task<int> SetIsDeletedByOriginAsync(int originId, bool isDeleted);
        Task<int> SetIsDeletedBySuperCategoryAsync(int superCategoryId, bool isDeleted);
        Task<(List<Product> Items, int Total)> QueryStorefrontPagedAsync(string? keyword, int page, int pageSize);
        Task<(List<Product> Items, int Total)> QueryAdminPagedAsync(string? keyword, int page, int pageSize);

    }
}
