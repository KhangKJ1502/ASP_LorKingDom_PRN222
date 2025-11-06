using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IReviewProductService
    {
        Task<List<ReviewProductDto>> GetAllReviewsAsync(bool includeDeleted = false);
        Task<ReviewProductDto?> GetByIdAsync(int reviewProductId);
        Task<int> CreateAsync(ReviewProductDto dto);
        Task<bool> UpdateAsync(int id, ReviewProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
        Task<bool> CanReviewAsync(int productId, int accountId);

        // customer review product
        Task<List<ReviewProductDto>> GetReviewsByAccountAsync(int accountId);
        Task<bool> HasUserReviewedAsync(int productId, int accountId);
        Task<ReviewProductDto?> GetReviewByProductAndAccountAsync(int productId, int accountId);
        Task<List<ReviewProductDto>> GetReviewsByProductIdAsync(int productId);
        Task<List<ReviewProductDto>> GetReviewsByProductIdAsync(int productId, int? accountId);
    }
}