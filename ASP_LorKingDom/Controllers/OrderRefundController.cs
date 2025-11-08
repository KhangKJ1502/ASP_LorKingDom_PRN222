using System.Security.Claims;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize] 
    public class OrderRefundController : Controller
    {
        private readonly IOrderRefundService _refundService;
        private readonly IOrderService _orderService;

        public OrderRefundController(
            IOrderRefundService refundService,
            IOrderService orderService)
        {
            _refundService = refundService;
            _orderService = orderService;
        }

        private int GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> CreateRequest(int orderId)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Unauthorized();

            var canRequest = await _refundService.CanRequestRefundAsync(orderId, accountId);
            if (!canRequest)
            {
                TempData["ErrorMessage"] = "Đơn hàng này không thể yêu cầu hoàn tiền.";
                return RedirectToAction("OrderHistory", "Order");
            }

            ViewBag.OrderId = orderId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest([FromBody] CreateRefundRequestDto dto)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0)
                return Json(new { success = false, message = "Unauthorized" });

            var result = await _refundService.CreateRefundRequestAsync(accountId, dto);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                refundId = result.RefundId
            });
        }

        [HttpGet]
        public async Task<IActionResult> MyRefunds()
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Unauthorized();

            var refunds = await _refundService.GetCustomerRefundHistoryAsync(accountId);
            return View(refunds);
        }

        [HttpGet]
        public async Task<IActionResult> RefundDetail(long id)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == 0) return Unauthorized();

            var refund = await _refundService.GetCustomerRefundDetailAsync(accountId, id);
            if (refund == null)
                return NotFound();

            return View(refund);
        }
    }
}