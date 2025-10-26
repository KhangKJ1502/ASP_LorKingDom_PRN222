using DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IReviewProductRepository
    {
        Task<List<ReviewProduct>> GetAllAsync();
        Task<List<ReviewProduct>> GetByProductIdAsync(int productId);
        Task<ReviewProduct?> GetByIdAsync(int reviewProductId);
        Task<int> CreateAsync(ReviewProduct review);
        Task<bool> UpdateAsync(ReviewProduct review);
        Task<bool> DeleteAsync(int reviewProductId);
        Task<bool> HasUserReviewedAsync(int productId, int accountId);
    }
}