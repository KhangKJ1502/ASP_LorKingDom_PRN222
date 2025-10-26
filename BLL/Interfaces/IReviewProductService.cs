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
    }
}