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
                return Json(new { cartItems = new List<object>() });

            var cart = await _cartService.GetByAccountIdAsync(accountId);

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new { cartItems = new List<object>() });
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
                i.AddedAt,
                i.ProductQuantity
            }).ToList();

            return Json(new { cartItems = items });
        }

        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return Json(new { success = false, redirectToLogin = true });

            if (request.Id <= 0 || request.Qty < 1)
                return Json(new { success = false, message = "Invalid product or quantity" });

            try
            {
                await _cartService.AddToCartAsync(accountId, request.Id, request.Qty);
                return Json(new { success = true });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("stock") || ex.Message.Contains("Không đủ hàng"))
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("UpdateQuantity/{cartItemId}")]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] int newQuantity)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return Json(new { success = false, redirectToLogin = true });

            var cart = await _cartService.GetByAccountIdAsync(accountId);
            if (cart == null || !cart.CartItems.Any(i => i.CartItemId == cartItemId))
                return Json(new { success = false, message = "Cart item does not belong to user" });

            try
            {
                await _cartService.UpdateCartItemQuantityAsync(cartItemId, newQuantity);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("RemoveItem/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return Json(new { success = false, redirectToLogin = true });

            var cart = await _cartService.GetByAccountIdAsync(accountId);
            if (cart == null || !cart.CartItems.Any(i => i.CartItemId == cartItemId))
                return Json(new { success = false, message = "Cart item does not belong to user" });

            try
            {
                await _cartService.RemoveCartItemAsync(cartItemId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Clear")]
        public async Task<IActionResult> Clear()
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return Json(new { success = false, redirectToLogin = true });

            try
            {
                await _cartService.ClearCartAsync(accountId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(accountIdClaim, out int id) ? id : 0;
        }
    }
}
