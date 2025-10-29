using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService _voucherService;
        private readonly IVoucherTypeService _voucherTypeService;
        private readonly IAccountService _accountService;

        public VoucherController(IVoucherService voucherService, IVoucherTypeService voucherTypeService, IAccountService accountService)
        {
            _voucherService = voucherService;
            _voucherTypeService = voucherTypeService;
            _accountService = accountService;
        }

        [HttpPost("Voucher/ApplyVoucher")]
        public async Task<IActionResult> ApplyVoucher(string code, decimal orderAmount)
        {
            var accountId = GetAccountId();
            if (accountId == 0)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var (isValid, message, voucher) = await _voucherService.ApplyVoucherAsync(code, accountId, orderAmount);

                if (isValid && voucher != null)
                {
                    // Tính số tiền giảm giá
                    decimal discountAmount = 0;

                    // Kiểm tra loại voucher: Percentage (%) hoặc Fixed Amount
                    var voucherTypeName = voucher.VoucherTypeName?.ToLower() ?? "fixed";

                    if (voucherTypeName.Contains("percent") || voucherTypeName.Contains("%"))
                    {
                        // Giảm theo phần trăm
                        discountAmount = orderAmount * (voucher.DiscountValue / 100);
                        if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
                        {
                            discountAmount = voucher.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        // Giảm giá cố định
                        discountAmount = voucher.DiscountValue;
                    }

                    return Json(new
                    {
                        success = true,
                        message = message,
                        discount = discountAmount,
                        voucherCode = voucher.VoucherCode
                    });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi kiểm tra mã giảm giá: " + ex.Message });
            }
        }

        [HttpGet("Voucher/Manage")]
        public async Task<IActionResult> ManageVouchers(string? q, string? voucherType, string? status, int page = 1, int pageSize = 10, bool showDeleted = false)
        {
            try
            {
                var allVouchers = await _voucherService.GetAllVouchersAsync(includeDeleted: showDeleted);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    allVouchers = allVouchers
                        .Where(v => v.VoucherCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                    (v.VoucherTypeName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                    (v.CreatorName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(voucherType) && int.TryParse(voucherType, out int typeId))
                    allVouchers = allVouchers.Where(v => v.VoucherTypeId == typeId).ToList();

                if (!string.IsNullOrWhiteSpace(status))
                    allVouchers = allVouchers.Where(v => v.Status == status).ToList();

                if (!showDeleted)
                    allVouchers = allVouchers.Where(v => v.Status != "Inactive").ToList();

                allVouchers = allVouchers.OrderByDescending(v => v.CreatedAt).ToList();

                var totalCount = allVouchers.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedVouchers = allVouchers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.Query = q;
                ViewBag.VoucherTypeFilter = voucherType;
                ViewBag.StatusFilter = status;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.ShowDeleted = showDeleted;
                ViewBag.VoucherTypes = await _voucherTypeService.GetAllAsync();
                ViewBag.Accounts = await _accountService.GetAllAsync();

                return View("~/Views/Admin/ManageVouchers.cshtml", pagedVouchers);
            }
            catch
            {
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("Voucher/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoucher(VoucherDto dto)
        {
            try
            {
                var voucherId = await _voucherService.CreateAsync(dto);
                return Json(new { message = "Voucher created successfully", voucherId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Voucher/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVoucher(int id, VoucherDto dto)
        {
            try
            {
                var success = await _voucherService.UpdateAsync(id, dto);
                if (!success)
                    return BadRequest(new { error = "Failed to update voucher" });

                return Json(new { message = "Voucher updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Voucher/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteVoucher(int id)
        {
            try
            {
                var success = await _voucherService.SoftDeleteAsync(id);
                if (!success)
                    return BadRequest(new { error = "Không thể xóa voucher" });

                return Json(new { message = "Voucher đã được chuyển vào thùng rác" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Voucher/Restore/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreVoucher(int id)
        {
            try
            {
                var success = await _voucherService.RestoreAsync(id);
                if (!success)
                    return BadRequest(new { error = "Không thể khôi phục voucher" });

                return Json(new { message = "Voucher đã được khôi phục" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("Voucher/Details/{id}")]
        public async Task<IActionResult> GetVoucherDetail(int id)
        {
            try
            {
                var voucher = await _voucherService.GetByIdAsync(id);
                if (voucher == null)
                    return NotFound();

                var result = new
                {
                    voucherId = voucher.VoucherId,
                    voucherCode = voucher.VoucherCode,
                    voucherTypeId = voucher.VoucherTypeId,
                    voucherTypeName = voucher.VoucherTypeName ?? "Unknown",
                    createBy = voucher.CreateBy,
                    creatorName = voucher.CreatorName ?? "Unknown",
                    discountValue = voucher.DiscountValue,
                    maxDiscountAmount = voucher.MaxDiscountAmount,
                    minOrderAmount = voucher.MinOrderAmount,
                    usageLimitPerUser = voucher.UsageLimitPerUser,
                    isStackable = voucher.IsStackable,
                    startDate = voucher.StartDate.ToString("yyyy-MM-ddTHH:mm"),
                    endDate = voucher.EndDate.ToString("yyyy-MM-ddTHH:mm"),
                    status = voucher.Status,
                    isDeleted = voucher.Status == "Inactive"
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(accountIdClaim, out int id) ? id : 0;
        }
    }
}