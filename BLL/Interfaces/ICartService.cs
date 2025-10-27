using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface ICartService
    {
        Task<CartDto?> GetByAccountIdAsync(int accountId);
        Task AddToCartAsync(int accountId, int productId, int quantity);
        Task UpdateCartItemQuantityAsync(int cartItemId, int newQuantity);
        Task RemoveCartItemAsync(int cartItemId);
        Task ClearCartAsync(int accountId);
    }
}
