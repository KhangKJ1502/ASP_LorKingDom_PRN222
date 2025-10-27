using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _svc;
        public WishlistController(IWishlistService svc) => _svc = svc;

        private bool TryGetAccountId(out int accountId)
        {
            accountId = 0;
            var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(s, out accountId);
        }

        // ==================== AJAX: Danh sách partial ====================
        [HttpGet("Wishlist/ListPartial")]
        [IgnoreAntiforgeryToken] // GET nên bỏ check token
        public async Task<IActionResult> ListPartial(string? wq)
        {
            if (!TryGetAccountId(out var accountId))
                return Challenge();

            var items = await _svc.ListAsync(accountId,
                string.IsNullOrWhiteSpace(wq) ? null : wq);
            return PartialView("_WishlistListPartial", items);
        }

        // ==================== AJAX: Xóa từng sản phẩm ====================
        [HttpPost("Wishlist/Remove")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            if (!TryGetAccountId(out var accountId))
                return Unauthorized();

            await _svc.RemoveAsync(accountId, productId);
            return Ok(); // 200
        }

        // ==================== AJAX: Xóa tất cả ====================
        [HttpPost("Wishlist/Clear")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Clear()
        {
            if (!TryGetAccountId(out var accountId))
                return Unauthorized();

            await _svc.ClearAsync(accountId);
            return Ok();
        }

        // ==================== Toggle từ icon trái tim (vẫn giữ logic cũ) ====================
        [HttpPost("Wishlist/Toggle")]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            var (added, _) = await _svc.ToggleAsync(accountId, productId);
            TempData["Success"] = added
                ? "Đã thêm vào yêu thích."
                : "Đã bỏ khỏi yêu thích.";

            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home");
            return Redirect(returnUrl);
        }
    }
}
