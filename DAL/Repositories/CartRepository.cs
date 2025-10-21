using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AspLorKingDomContext _db;

        public CartRepository(AspLorKingDomContext db)
        {
            _db = db;
        }

        public async Task<Cart?> GetByAccountIdAsync(int accountId)
        {
            return await _db.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.AccountId == accountId);
        }

        public async Task<CartItem?> GetCartItemByIdAsync(int cartItemId)
        {
            return await _db.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task UpdateCartItemAsync(CartItem item)
        {
            _db.CartItems.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveCartItemAsync(CartItem item)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int cartId)
        {
            var items = await _db.CartItems.Where(ci => ci.CartId == cartId).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
            // Có thể xóa cart nếu empty, nhưng giữ lại
        }
    }
}