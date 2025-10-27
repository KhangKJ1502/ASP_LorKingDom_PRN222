using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface IWishlistService
    {
        Task<(bool added, int count)> ToggleAsync(int accountId, int productId);
        Task<int> RemoveAsync(int accountId, int productId);
        Task<int> ClearAsync(int accountId);
        Task<List<WishlistItemDto>> ListAsync(int accountId, string? keyword);
        Task<List<int>> GetProductIdsAsync(int accountId);
        Task<int> CountAsync(int accountId);
    }
}
