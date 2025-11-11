using DAL.Models;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IReviewProductImageRepository
    {
        Task<int> CreateAsync(ReviewProductImage image);
        Task<bool> DeleteAsync(int imageId);
    }
}