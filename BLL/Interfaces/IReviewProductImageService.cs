using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IReviewProductImageService
    {
        Task<int> CreateAsync(ReviewProductImageDto dto);
        Task<bool> DeleteAsync(int imageId);
    }
}