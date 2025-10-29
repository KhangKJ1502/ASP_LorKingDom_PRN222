using DAL.Models;

namespace DAL.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByAccountIdAsync(int accountId);
        Task<Cart> GetOrCreateByAccountIdAsync(int accountId);
        Task<Product?> GetProductByIdAsync(int productId);
        Task<CartItem?> GetCartItemByIdAsync(int cartItemId);
        Task AddCartItemAsync(CartItem item);
        Task UpdateCartItemAsync(CartItem item);
        Task RemoveCartItemAsync(CartItem item);
        Task ClearCartAsync(int cartId);
        Task ClearCartByAccountIdAsync(int accountId);
    }
}
