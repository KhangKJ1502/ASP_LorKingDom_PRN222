using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DAL.Models;

namespace DAL.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByAccountIdAsync(int accountId);
        Task<CartItem?> GetCartItemByIdAsync(int cartItemId);
        Task UpdateCartItemAsync(CartItem item);
        Task RemoveCartItemAsync(CartItem item);
        Task ClearCartAsync(int cartId);
    }
}
