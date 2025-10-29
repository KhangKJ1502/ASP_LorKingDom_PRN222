using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IAccountService _accountService;

        public OrderController(
            IOrderService orderService,
            ICartService cartService,
            IAccountService accountService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _accountService = accountService;
        }

        [HttpGet("/Home/OrderDetails")]
        public IActionResult Index()
        {
            return View("~/Views/Home/OrderDetails.cshtml");
        }

        [HttpGet("/Home/Order")]
        public IActionResult OrderHistory()
        {
            // Trả về Partial View để tránh duplicate header/footer khi load qua AJAX
            return PartialView("~/Views/Home/_OrderHistory.cshtml");
        }

        [HttpGet("/Home/Checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                // Lấy thông tin account
                var account = await _accountService.GetByIdAsync(accountId);
                if (account == null)
                    return RedirectToAction("Login", "Auth");

                // Lấy dữ liệu giỏ hàng
                var cart = await _cartService.GetByAccountIdAsync(accountId);
                if (cart == null || !cart.CartItems.Any())
                    return RedirectToAction("Index", "Cart");

                // Chuẩn bị dữ liệu giỏ hàng cho View
                var cartItems = cart.CartItems.ToList();

                // Tính tổng tiền
                decimal subtotal = 0;
                foreach (var item in cartItems)
                {
                    decimal price = item.PriceAtThatTime > 0 ? item.PriceAtThatTime : item.CurrentPrice;
                    subtotal += item.Quantity * price;
                }

                //decimal total = subtotal;

                ViewBag.CartItems = cartItems;
                ViewBag.Subtotal = subtotal;
                //ViewBag.Total = total;

                // Thông tin người nhận mặc định từ account
                ViewBag.FullName = account.AccountName ?? "";
                ViewBag.Email = account.Email ?? "";
                ViewBag.Phone = account.PhoneNumber ?? "";
                ViewBag.AccountId = accountId;

                return View("~/Views/Home/Checkout.cshtml");
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost("/Order/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckoutDto checkoutDto)
        {
            var accountId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (accountId == 0)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var (success, message, orderId) = await _orderService.CreateOrderAsync(accountId, checkoutDto);

                if (success)
                {
                    return Json(new { success = true, message = message, orderId = orderId });
                }
                else
                {
                    return Json(new { success = false, message = message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("/Order/Manage")]
        public IActionResult Manage()
        {
            return View("~/Views/Admin/ManageOrder.cshtml");
        }

        // ===== Helper Methods =====
        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(accountIdClaim, out int id) ? id : 0;
        }
    }
}
