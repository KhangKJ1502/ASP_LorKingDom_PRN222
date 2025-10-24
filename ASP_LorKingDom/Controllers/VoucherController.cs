using BLL.DTOs;
using BLL.Interfaces;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

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

		[HttpGet("Voucher/Manage")]
		public async Task<IActionResult> ManageVouchers(string? q, string? voucherType, string? status, int page = 1, int pageSize = 10, bool showDeleted = false)
		{
			try
			{
				var allVouchers = await _voucherService.GetAllVouchersAsync(showDeleted);

				// Search filter
				if (!string.IsNullOrWhiteSpace(q))
				{
					allVouchers = allVouchers
						.Where(v =>
							v.VoucherCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
							(v.VoucherTypeName != null && v.VoucherTypeName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
							(v.CreatorName != null && v.CreatorName.Contains(q, StringComparison.OrdinalIgnoreCase)))
						.ToList();
				}

				// Voucher type filter
				if (!string.IsNullOrWhiteSpace(voucherType) && int.TryParse(voucherType, out int typeId))
				{
					allVouchers = allVouchers.Where(v => v.VoucherTypeId == typeId).ToList();
				}

				// Status filter
				if (!string.IsNullOrWhiteSpace(status))
				{
					allVouchers = allVouchers.Where(v => v.Status == status).ToList();
				}

				// Sort by latest
				allVouchers = allVouchers.OrderByDescending(v => v.CreatedAt).ToList();

				// Pagination
				var totalCount = allVouchers.Count;
				var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
				var pagedVouchers = allVouchers
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToList();

				ViewBag.Query = q;
				ViewBag.VoucherTypeFilter = voucherType;
				ViewBag.StatusFilter = status;
				ViewBag.CurrentPage = page;
				ViewBag.TotalPages = totalPages;
				ViewBag.PageSize = pageSize;
				ViewBag.TotalCount = totalCount;
				ViewBag.ShowDeleted = showDeleted;

				// Load voucher types and accounts for dropdowns
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
		public async Task<IActionResult> DeleteVoucher(int id)
		{
			try
			{
				var success = await _voucherService.DeleteAsync(id);
				if (!success)
					return BadRequest(new { error = "Failed to move voucher to recycle bin" });

				return Json(new { message = "Voucher moved to recycle bin" });
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
					return BadRequest(new { error = "Failed to restore voucher" });

				return Json(new { message = "Voucher restored successfully" });
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
					creatorName = voucher.CreatorName ?? "Unknown",
					discountValue = voucher.DiscountValue,
					maxDiscountAmount = voucher.MaxDiscountAmount,
					minOrderAmount = voucher.MinOrderAmount,
					usageLimitPerUser = voucher.UsageLimitPerUser,
					isStackable = voucher.IsStackable,
					startDate = voucher.StartDate.ToString("dd/MM/yyyy HH:mm"),
					endDate = voucher.EndDate.ToString("dd/MM/yyyy HH:mm"),
					status = voucher.Status,
					isDeleted = voucher.IsDeleted,
					createdAt = voucher.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
					updatedAt = voucher.UpdatedAt?.ToString("dd/MM/yyyy HH:mm")
				};

				return Json(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { error = ex.Message });
			}
		}
	}
}