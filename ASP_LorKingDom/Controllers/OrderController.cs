using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebUI.Filters;

namespace WebUI.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IAccountService _accountService;
        private readonly IOrderRefundService _orderRefundService;

        public OrderController(
            IOrderService orderService,
            ICartService cartService,
            IAccountService accountService,
            IOrderRefundService orderRefundService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _accountService = accountService;
            _orderRefundService = orderRefundService;
        }

        [HttpGet("/Home/OrderDetails")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null || order.AccountId != accountId)
                    return RedirectToAction("OrderHistory");

                return View("~/Views/Home/OrderDetails.cshtml", order);
            }
            catch
            {
                return RedirectToAction("OrderHistory");
            }
        }

        [HttpGet("/Home/Order")]
        [Authorize]
        public async Task<IActionResult> OrderHistory(int page = 1)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                const int pageSize = 5;
                var allOrders = await _orderService.GetOrdersByAccountIdAsync(accountId);

                // Calculate pagination
                var totalItems = allOrders.Count;
                var orders = allOrders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Create paged result
                var pagedResult = new PagedResult<OrderDto>
                {
                    Items = orders,
                    Total = totalItems,
                    Page = page,
                    PageSize = pageSize
                };

                return PartialView("~/Views/Home/_OrderHistory.cshtml", pagedResult);
            }
            catch
            {
                var emptyResult = new PagedResult<OrderDto>
                {
                    Items = new List<OrderDto>(),
                    Total = 0,
                    Page = 1,
                    PageSize = 10
                };
                return PartialView("~/Views/Home/_OrderHistory.cshtml", emptyResult);
            }
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
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Order Management
        public async Task<IActionResult> Manage(string? q, int? status, DateTime? dateFrom, DateTime? dateTo, int page = 1)
        {
            try
            {
                const int pageSize = 10;
                var result = await _orderService.GetAllOrdersAsync(q, status, dateFrom, dateTo, page, pageSize);

                ViewBag.Query = q;
                ViewBag.StatusFilter = status;
                ViewBag.DateFromFilter = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateToFilter = dateTo?.ToString("yyyy-MM-dd");
                ViewBag.CurrentPage = page;

                return View("~/Views/Admin/ManageOrder.cshtml", result);
            }
            catch
            {
                return View("~/Views/Admin/ManageOrder.cshtml", new BLL.DTOs.PagedResult<OrderDto>());
            }
        }

        [HttpPost("/Order/UpdateStatus")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Order Management
        public async Task<IActionResult> UpdateStatus(int orderId, int statusId)
        {
            try
            {
                // Get current admin/staff ID
                var adminId = GetAccountId();
                
                var success = await _orderService.UpdateOrderStatusAsync(orderId, statusId, adminId, "Admin/Staff cập nhật trạng thái");
                if (success)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet("/Order/GetOrderDetail")]
        [Authorize(AuthenticationSchemes = "AdminScheme")]
        [AdminAndStaffOnly] // Staff: Order Management
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                return Json(new { success = true, order = order });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ===== Helper Methods =====
        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(accountIdClaim, out int id) ? id : 0;
        }
    }
}
