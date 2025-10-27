using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize]
    [Route("[controller]")]  // Add this to enable attribute routing for the actions
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
            if (accountId == 0) return Unauthorized("User not authenticated");

            var cart = await _cartService.GetByAccountIdAsync(accountId);
            if (cart == null)
            {
                return Ok(new { CartItems = new List<CartItemDto>() });
            }

            return Ok(cart);
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