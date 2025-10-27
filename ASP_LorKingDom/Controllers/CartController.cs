using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            return View();
        }

		[HttpGet("GetCartData")]
		public async Task<IActionResult> GetCartData()
		{
			var accountId = GetAccountId();
			if (accountId == 0)
				return Unauthorized(new { message = "User not authenticated" });

			var cart = await _cartService.GetByAccountIdAsync(accountId);

			if (cart == null || !cart.CartItems.Any())
			{
				return Ok(new { cartItems = new List<object>() });
			}

			var items = cart.CartItems.Select(i => new
			{
				i.CartItemId,
				i.ProductId,
				i.ProductName,
				i.MainImageUrl,
				i.Quantity,
				i.PriceAtThatTime,
				i.CurrentPrice,
				i.AddedAt
			}).ToList();

			return Ok(new { cartItems = items });
		}

		[HttpPost("AddToCart")]
		public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
		{
			var accountId = GetAccountId();
			if (accountId == 0)
				return Unauthorized(new { message = "User not authenticated" });

			if (request.Id <= 0 || request.Qty < 1)
				return BadRequest(new { message = "Invalid product or quantity" });

			try
			{
				await _cartService.AddToCartAsync(accountId, request.Id, request.Qty);
				return Ok(new { success = true });
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
			{
				return BadRequest(new { message = "Sản phẩm không tồn tại." });
			}
			catch (InvalidOperationException ex) when (ex.Message.Contains("stock"))
			{
				return BadRequest(new { message = "Không đủ hàng trong kho." });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("UpdateQuantity/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] int newQuantity)
        {
            var accountId = GetAccountId();
            if (accountId == 0) return Unauthorized("User not authenticated");

            var cart = await _cartService.GetByAccountIdAsync(accountId);
            if (cart == null || !cart.CartItems.Any(i => i.CartItemId == cartItemId))
                return Forbid("Cart item does not belong to user");

            try
            {
                await _cartService.UpdateCartItemQuantityAsync(cartItemId, newQuantity);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("RemoveItem/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var accountId = GetAccountId();
            if (accountId == 0) return Unauthorized("User not authenticated");

            var cart = await _cartService.GetByAccountIdAsync(accountId);
            if (cart == null || !cart.CartItems.Any(i => i.CartItemId == cartItemId))
                return Forbid("Cart item does not belong to user");

            try
            {
                await _cartService.RemoveCartItemAsync(cartItemId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Clear")]
        public async Task<IActionResult> Clear()
        {
            var accountId = GetAccountId();
            if (accountId == 0) return Unauthorized("User not authenticated");

            try
            {
                await _cartService.ClearCartAsync(accountId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(accountIdClaim, out int id) ? id : 0;
        }
    }
}