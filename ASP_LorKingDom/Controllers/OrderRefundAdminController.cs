using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    //[Authorize(Roles = "Admin,Staff")] // Bỏ comment nếu cần giới hạn quyền truy cập
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
        // GET: /OrderRefundAdmin/Manage (Combined handler)
        // ------------------------------
        [HttpGet("Manage")]
        public async Task<IActionResult> Manage(
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
        // -> Dùng cho modal chi tiết hoàn tiền (AJAX)
        // ------------------------------
        [HttpGet("GetRefundDetail/{id}")]
        public async Task<IActionResult> GetRefundDetail(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "ID không hợp lệ." });

            try
            {
                var detail = await _refundService.GetDetailForModalAsync(id);
                if (detail == null)
                    return NotFound(new { message = "Không tìm thấy yêu cầu hoàn tiền." });

                return Json(detail); // trả JSON cho AJAX
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/ApproveRefund
        // ------------------------------
        [HttpPost("ApproveRefund")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRefund(long refundId)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToAction(nameof(Manage));
            }

            try
            {
                int staffId = GetCurrentStaffId();
                await _refundService.ApproveOrUpdateStatusAsync(refundId, "Approved", staffId);

                TempData["SuccessMessage"] = "✅ Đã duyệt yêu cầu hoàn tiền.";
            }
            catch (KeyNotFoundException)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hoàn tiền.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = "Lỗi xác thực: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi duyệt hoàn tiền: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/RejectRefund
        // ------------------------------
        [HttpPost("RejectRefund")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRefund(long refundId)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToAction(nameof(Manage));
            }

            try
            {
                int staffId = GetCurrentStaffId();
                await _refundService.ApproveOrUpdateStatusAsync(refundId, "Rejected", staffId);

                TempData["SuccessMessage"] = "❌ Đã từ chối yêu cầu hoàn tiền.";
            }
            catch (KeyNotFoundException)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hoàn tiền.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = "Lỗi xác thực: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi từ chối hoàn tiền: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        // ------------------------------
        // POST: /OrderRefundAdmin/UpdateStatus
        // (Giữ lại để tương thích ngược - cho các trạng thái khác: Processing, Completed, Cancelled)
        // ------------------------------
        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long refundId, string newStatus)
        {
            if (refundId <= 0)
            {
                TempData["ErrorMessage"] = "ID hoàn tiền không hợp lệ.";
                return RedirectToAction(nameof(Manage));
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không được để trống.";
                return RedirectToAction(nameof(Manage));
            }

            try
            {
                int staffId = GetCurrentStaffId();
                await _refundService.ApproveOrUpdateStatusAsync(refundId, newStatus, staffId);

                TempData["SuccessMessage"] = GetSuccessMessageByStatus(newStatus);
            }
            catch (KeyNotFoundException)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hoàn tiền.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = "Lỗi xác thực: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật: " + ex.Message;
            }

            return RedirectToAction(nameof(Manage));
        }

        // ===================================================================
        // PRIVATE HELPERS
        // ===================================================================

        // Tải danh sách refund + gắn ViewBag filter
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

            try
            {
                var result = await _refundService.SearchRefundsAsync(keyword, status, dateFrom, dateTo, page, pageSize);

                PrepareViewBagForList(keyword, status, dateFrom, dateTo, result);

                // Dùng đường dẫn tuyệt đối vì view nằm ở thư mục Admin
                return View("~/Views/Admin/ManageOrderRefundAdmin.cshtml", result);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi khi tải dữ liệu hoàn tiền: " + ex.Message;

                return View("~/Views/Admin/ManageOrderRefundAdmin.cshtml",
                    CreateEmptyPagedResult(page, pageSize));
            }
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

        // Tạo danh sách trống khi load lỗi
        private PagedResult<OrderRefundDto> CreateEmptyPagedResult(int page, int pageSize)
        {
            return new PagedResult<OrderRefundDto>
            {
                Items = new List<OrderRefundDto>(),
                Page = page,
                PageSize = pageSize,
                Total = 0
            };
        }

        // Mapping status -> message
        private string GetSuccessMessageByStatus(string status)
        {
            return status switch
            {
                "Approved" => "✅ Đã duyệt yêu cầu hoàn tiền.",
                "Rejected" => "❌ Đã từ chối yêu cầu hoàn tiền.",
                "Refunded" => "💰 Đã đánh dấu hoàn tiền thành công.",
                "Processing" => "⏳ Đang xử lý hoàn tiền.",
                "Cancelled" => "🚫 Đã hủy yêu cầu hoàn tiền.",
                _ => "✅ Cập nhật trạng thái thành công."
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
    }
}
