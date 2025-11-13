using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Filters;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    [AdminAndStaffOnly] // Staff: Refund Management
    [Route("OrderRefundAdmin")]
    public class OrderRefundAdminController : Controller
    {                                                                                                                                                                                                           
        private readonly IOrderRefundService _refundService;

        public OrderRefundAdminController(IOrderRefundService refundService)
        {
            _refundService = refundService;
        }

        // ------------------------------
        // GET: /OrderRefundAdmin (Index - List all)
        // ------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10)
        {
            return await LoadRefundListAsync(null, null, null, null, page, pageSize);
        }

        // ------------------------------
        // GET: /OrderRefundAdmin/Search (Search with filters)
        // ------------------------------
        [HttpGet("Search")]
        public async Task<IActionResult> Search(
            string? keyword,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page = 1,
            int pageSize = 10)
        {
            return await LoadRefundListAsync(keyword, status, dateFrom, dateTo, page, pageSize);
        }

        // ------------------------------
        // GET: /OrderRefundAdmin/GetRefundDetail/{id}
        // ------------------------------
        [HttpGet("GetRefundDetail/{id}")]
        public async Task<IActionResult> GetRefundDetail(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "ID không hợp lệ." });

            var detail = await _refundService.GetDetailForModalAsync(id);
            if (detail == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu hoàn tiền." });

            return Json(detail);
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/ApproveRefund
        // ------------------------------
        [HttpPost("ApproveRefund")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRefund(
            long refundId, 
            string? keyword, 
            string? status, 
            DateTime? dateFrom, 
            DateTime? dateTo, 
            int page = 1, 
            int pageSize = 10)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
            }

            int staffId = GetCurrentStaffId();
            await _refundService.ApproveOrUpdateStatusAsync(refundId, "Approved", staffId);

            TempData["SuccessMessage"] = "Đã duyệt yêu cầu hoàn tiền.";
            return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/RejectRefund
        // ------------------------------
        [HttpPost("RejectRefund")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRefund(
            long refundId, 
            string? keyword, 
            string? status, 
            DateTime? dateFrom, 
            DateTime? dateTo, 
            int page = 1, 
            int pageSize = 10)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
            }

            int staffId = GetCurrentStaffId();
            await _refundService.ApproveOrUpdateStatusAsync(refundId, "Rejected", staffId);

            TempData["SuccessMessage"] = "Đã từ chối yêu cầu hoàn tiền.";
            return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/UpdateStatus
        // ------------------------------
        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            long refundId, 
            string newStatus, 
            string? keyword, 
            string? status, 
            DateTime? dateFrom, 
            DateTime? dateTo, 
            int page = 1, 
            int pageSize = 10)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không được để trống.";
                return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
            }

            int staffId = GetCurrentStaffId();
            await _refundService.ApproveOrUpdateStatusAsync(refundId, newStatus, staffId);

            TempData["SuccessMessage"] = GetSuccessMessageByStatus(newStatus);
            return RedirectToListOrSearch(keyword, status, dateFrom, dateTo, page, pageSize);
        }

        // ===================================================================
        // PRIVATE HELPERS
        // ===================================================================
        private async Task<IActionResult> LoadRefundListAsync(
            string? keyword,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _refundService.SearchRefundsAsync(keyword, status, dateFrom, dateTo, page, pageSize);

            PrepareViewBagForList(keyword, status, dateFrom, dateTo, result);

            return View("~/Views/Admin/ManageOrderRefundAdmin.cshtml", result);
        }

        // Gắn các biến filter vào ViewBag (để giữ lại form tìm kiếm)
        private void PrepareViewBagForList(
            string? keyword,
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            PagedResult<OrderRefundDto> result)
        {
            ViewBag.Query = keyword ?? "";
            ViewBag.StatusFilter = status ?? "";
            ViewBag.DateFromFilter = dateFrom?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.DateToFilter = dateTo?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.CurrentPage = result.Page;
            ViewBag.PageSize = result.PageSize;
            ViewBag.TotalRecords = result.Total;
            ViewBag.TotalPages = result.Total == 0
                ? 1
                : (int)Math.Ceiling((double)result.Total / result.PageSize);
        }

        // Mapping status -> message
        private string GetSuccessMessageByStatus(string status)
        {
            return status switch
            {
                "Approved" => "Đã duyệt yêu cầu hoàn tiền.",
                "Rejected" => "Đã từ chối yêu cầu hoàn tiền.",
                "Refunded" => "Đã đánh dấu hoàn tiền thành công.",
                "Processing" => "Đang xử lý hoàn tiền.",
                "Cancelled" => "Đã hủy yêu cầu hoàn tiền.",
                _ => "Cập nhật trạng thái thành công."
            };
        }

        // Lấy ID staff hiện tại từ Claims
        private int GetCurrentStaffId()
        {
            var claim = User.FindFirst("AccountId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            if (claim != null &&
                int.TryParse(claim.Value, out var staffId) &&
                staffId > 0)
            {
                return staffId;
            }

            throw new UnauthorizedAccessException("Không thể xác định AccountId của staff.");
        }

        // Helper to redirect with or without filters
        private IActionResult RedirectToListOrSearch(
            string? keyword, 
            string? status, 
            DateTime? dateFrom, 
            DateTime? dateTo, 
            int page, 
            int pageSize)
        {
            bool hasFilters = !string.IsNullOrWhiteSpace(keyword) 
                || !string.IsNullOrWhiteSpace(status) 
                || dateFrom.HasValue 
                || dateTo.HasValue;

            return hasFilters
                ? RedirectToAction(nameof(Search), new { keyword, status, dateFrom, dateTo, page, pageSize })
                : RedirectToAction(nameof(Index), new { page, pageSize });
        }
    }
}
