using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;

        public CartService(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }

        public async Task<CartDto?> GetByAccountIdAsync(int accountId)
        {
            var cart = await _cartRepo.GetByAccountIdAsync(accountId);
            if (cart == null) return null;

            var dto = new CartDto
            {
                CartId = cart.CartId,
                AccountId = cart.AccountId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                CartItems = new List<CartItemDto>()
            };

            foreach (var item in cart.CartItems)
            {
                var product = item.Product;
                if (product != null)
                {
                    dto.CartItems.Add(new CartItemDto
                    {
                        CartItemId = item.CartItemId,
                        CartId = item.CartId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PriceAtThatTime = item.PriceAtThatTime,
                        Status = item.Status,
                        AddedAt = item.AddedAt,
                        ProductName = product.ProductName,
                        MainImageUrl = product.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? product.ProductImages.FirstOrDefault()?.ImageUrl ?? "/assets/placeholder.jpg",
                        CurrentPrice = product.Price
                    });
                }
            }

            return dto;
        }

		public async Task AddToCartAsync(int accountId, int productId, int quantity)
		{
			if (quantity < 1) throw new ArgumentException("Quantity must be at least 1");

			var cart = await _cartRepo.GetOrCreateByAccountIdAsync(accountId);

			var product = await _cartRepo.GetProductByIdAsync(productId);
			if (product == null)
				throw new InvalidOperationException("Product not found");

			if (product.Quantity < quantity)
				throw new InvalidOperationException($"Not enough stock for {product.ProductName}. Available: {product.Quantity}");

			var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId && ci.Status == "Active");
			if (existingItem != null)
			{
				existingItem.Quantity += quantity;
				existingItem.PriceAtThatTime = product.Price;
				await _cartRepo.UpdateCartItemAsync(existingItem);
			}
			else
			{
				var newItem = new CartItem
				{
					CartId = cart.CartId,
					ProductId = productId,
					Quantity = quantity,
					PriceAtThatTime = product.Price,
					Status = "Active",
					AddedAt = DateTime.Now
				};
				await _cartRepo.AddCartItemAsync(newItem);
			}

			cart.UpdatedAt = DateTime.Now;
		}

		public async Task UpdateCartItemQuantityAsync(int cartItemId, int newQuantity)
        {
            if (newQuantity < 1) throw new ArgumentException("Quantity must be at least 1");

            var item = await _cartRepo.GetCartItemByIdAsync(cartItemId);
            if (item == null) throw new InvalidOperationException("Cart item not found");

            if (item.Product.Quantity < newQuantity)
                throw new InvalidOperationException($"Not enough stock for {item.Product.ProductName}. Available: {item.Product.Quantity}");

            item.Quantity = newQuantity;
            // Keep PriceAtThatTime as the price per unit at the time of adding to cart
            await _cartRepo.UpdateCartItemAsync(item);
        }

        public async Task RemoveCartItemAsync(int cartItemId)
        {
            var item = await _cartRepo.GetCartItemByIdAsync(cartItemId);
            if (item == null) return; // Idempotent

            await _cartRepo.RemoveCartItemAsync(item);
        }

        public async Task ClearCartAsync(int accountId)
        {
            var cart = await _cartRepo.GetByAccountIdAsync(accountId);
            if (cart == null) return;

            await _cartRepo.ClearCartAsync(cart.CartId);
        }
    }
}