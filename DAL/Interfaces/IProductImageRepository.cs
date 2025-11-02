// DAL/Repositories/IProductImageRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Interfaces
{
    public interface IProductImageRepository
    {
        Task<List<ProductImage>> GetByProductIdAsync(int productId);
        Task<ProductImage?> GetMainAsync(int productId);
        Task<int> CountSecondaryAsync(int productId);
        Task UnsetMainAsync(int productId);
        Task AddAsync(ProductImage entity);
        Task AddRangeAsync(IEnumerable<ProductImage> entities);
        Task UpsertImagesAsync(
       int productId,
       string? mainImageUrl,
       IEnumerable<string> keepSecondaryUrls,
       IEnumerable<string> addSecondaryUrls,
       bool keepMainIfNull = true);
        // Cho phép service bao bọc transaction khi cần
        Task ExecuteInTransactionAsync(Func<Task> action);
        void RemoveRange(IEnumerable<ProductImage> entities);
        Task SaveChangesAsync();
    }
}
