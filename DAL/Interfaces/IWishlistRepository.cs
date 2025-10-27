using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Interfaces
{
    public interface IWishlistRepository
    {
        Task<bool> ExistsAsync(int accountId, int productId);
        Task AddAsync(int accountId, int productId);
        Task<int> DeleteAsync(int accountId, int productId);
        Task<int> DeleteAllAsync(int accountId);
        Task<List<Wishlist>> GetWithProductsAsync(int accountId, string? keyword);
        Task<List<int>> GetProductIdsAsync(int accountId);
        Task<int> CountAsync(int accountId);
    }
}
