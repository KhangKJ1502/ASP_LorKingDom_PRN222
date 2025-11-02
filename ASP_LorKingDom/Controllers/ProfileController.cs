using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IAddressService _addrSvc;
        public ProfileController(IAddressService addrSvc) => _addrSvc = addrSvc;

        private bool TryGetAccountId(out int accountId)
        {
            accountId = 0;
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(s, out accountId);
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Overview));

        [HttpGet]
        public IActionResult Overview()
        {
            ViewBag.ActiveTab = "overview";
            return View("~/Views/Profile/Overview.cshtml");
        }

        [HttpGet]
        public IActionResult Orders()
        {
            ViewBag.ActiveTab = "orders";
            return View("~/Views/Profile/Orders.cshtml");
        }

        [HttpGet]
        public IActionResult Wishlist()
        {
            ViewBag.ActiveTab = "wishlist";
            return View("~/Views/Profile/Wishlist.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Addresses(string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();
            var list = await _addrSvc.ListAsync(accountId);
            ViewBag.ActiveTab = "addresses";
            ViewBag.Query = q;
            return View("~/Views/Profile/Addresses.cshtml", list); // Model: List<AddressDto>
        }


        [HttpGet]
        public IActionResult Settings()
        {
            ViewBag.ActiveTab = "settings";
            return View("~/Views/Profile/Settings.cshtml");
        }
    }
}
